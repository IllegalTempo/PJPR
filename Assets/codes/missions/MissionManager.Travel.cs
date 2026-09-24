using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class MissionManager
{
    private const string MissionPortalPrefabId = "Mission_Portal";

    [Header("Mission Travel")]
    [SerializeField, Min(0f)] private float outboundPortalMinimumDistance = 250f;
    [SerializeField, Min(0f)] private float outboundPortalMaximumDistance = 500f;
    [SerializeField] private GameObject[] mainScenePauseRoots;

    private readonly MissionTravelState travelState = new();
    private readonly HashSet<ulong> loadingPeerIds = new();
    private MissionPauseState missionPauseState;
    private Scene loadedMissionScene;
    private int loadedMissionSessionId;
    private MissionSpawnPoint missionSpawnPoint;
    private int nextTravelSessionId;
    private bool completingMissionEntry;
    private MissionTravelSnapshot? lastCompletedTravelSnapshot;
    private string outboundPortalIdForSession = string.Empty;

    public bool CanStartVote => travelState.Phase == MissionTravelPhase.Idle;
    public MissionTravelSnapshot TravelSnapshot => travelState.Snapshot;

#if UNITY_EDITOR
    public MissionTravelState TestTravelState => travelState;
#endif

    public async UniTask PrepareWinningMissionAsync(MissionData missionData)
    {
        if (!IsWorldManager())
            return;

        if (missionData == null || string.IsNullOrWhiteSpace(missionData.missionSceneName))
        {
            Debug.LogError("[MissionManager] Winning mission has no additive scene configured.");
            travelState.Reset();
            return;
        }

        if (MainSpaceship.Instance == null || NetworkSystem.Instance == null)
        {
            Debug.LogError("[MissionManager] Cannot create mission portal without the spaceship and network system.");
            travelState.Reset();
            return;
        }

        if (outboundPortalMaximumDistance < outboundPortalMinimumDistance)
        {
            Debug.LogError("[MissionManager] Outbound portal maximum distance must be greater than or equal to its minimum distance.");
            travelState.Reset();
            return;
        }

        Vector3 direction = UnityEngine.Random.onUnitSphere;
        Vector3 portalPosition = MissionPortalPlacement.Calculate(
            MainSpaceship.Instance.transform.position,
            outboundPortalMinimumDistance,
            outboundPortalMaximumDistance,
            direction,
            UnityEngine.Random.value);

        NetworkGameObject portal = await NetworkSystem.Instance.CreateNetworkObject(
            MissionPortalPrefabId, portalPosition, Quaternion.LookRotation(-direction), 0);
        if (portal == null || portal.Identity == null)
        {
            Debug.LogError("[MissionManager] Failed to spawn Mission_Portal.");
            travelState.Reset();
            return;
        }

        travelState.SetOutboundPortal(
            missionData.missionName,
            missionData.missionSceneName,
            portal.Identity.Identifier,
            portalPosition);
        outboundPortalIdForSession = portal.Identity.Identifier;
        portal.GetComponent<MissionPortal>()?.ShowWaypoint($"{missionData.missionName} Portal");
    }

    public bool TryUsePortal(string portalId, Collider shipCollider)
    {
        if (!IsWorldManager() || !MissionPortal.IsSpaceshipCollider(shipCollider) ||
            string.IsNullOrEmpty(portalId) || portalId != travelState.ActivePortalId)
        {
            return false;
        }

        if (travelState.Phase == MissionTravelPhase.OutboundPortal)
        {
            BeginMissionLoadAsync().Forget();
            return true;
        }

        if (travelState.Phase == MissionTravelPhase.MissionActive)
            return TryBeginReturnFromPortal();

        return false;
    }

    private async UniTask BeginMissionLoadAsync()
    {
        if (MainSpaceship.Instance == null)
            return;

        int sessionId = ++nextTravelSessionId;
        loadingPeerIds.Clear();
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && NetworkSystem.Instance.Server != null)
        {
            foreach (KeyValuePair<ulong, NetworkPlayer> entry in NetworkSystem.Instance.Server.NetworkUsers)
            {
                if (entry.Value != null && entry.Value.ReadyState == (int)ReadyState.SyncNetworkObjects)
                    loadingPeerIds.Add(entry.Key);
            }
        }

        if (!travelState.TryBeginLoading(sessionId, loadingPeerIds, MainSpaceship.Instance.transform.rotation))
            return;

        SetPortalUsable(travelState.ActivePortalId, false);
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
        {
            NetworkRouter.Instance.DistributeMessageToReady(new NMS_Server_LoadMissionScene(
                sessionId, travelState.MissionName, travelState.SceneName));
        }

        bool loaded = await LoadMissionSceneAsync(sessionId, travelState.SceneName);
        if (!loaded)
        {
            await AbortMissionLoadAsync(sessionId, "Mission scene failed to load or has no MissionSpawnPoint.");
            return;
        }

        travelState.MarkHostLoaded(sessionId);
        float deadline = Time.realtimeSinceStartup + NetworkSystem.TIMEOUTSECONDS;
        while (travelState.Phase == MissionTravelPhase.LoadingMission && !travelState.AllPeersLoaded)
        {
            PruneDisconnectedLoadingPeers();
            if (Time.realtimeSinceStartup >= deadline)
            {
                await AbortMissionLoadAsync(sessionId, "Timed out waiting for mission scene readiness.");
                return;
            }
            await UniTask.Yield();
        }

        await TryCompleteLoadingAsync();
    }

    public async UniTask HandleLoadMissionSceneAsync(int sessionId, string missionName, string sceneName)
    {
        if (IsWorldManager() || sessionId < travelState.SessionId)
            return;

        string outboundPortalId = travelState.ActivePortalId;
        Vector3 outboundPosition = travelState.ReturnPosition;
        if (string.IsNullOrEmpty(outboundPortalId))
        {
            foreach (MissionPortal portal in UnityEngine.Object.FindObjectsByType<MissionPortal>(FindObjectsSortMode.None))
            {
                if (string.IsNullOrEmpty(portal.NetworkId))
                    continue;
                outboundPortalId = portal.NetworkId;
                outboundPosition = portal.transform.position;
                break;
            }
        }

        Quaternion outboundRotation = MainSpaceship.Instance != null
            ? MainSpaceship.Instance.transform.rotation
            : travelState.ReturnRotation;
        MissionTravelSnapshot incoming = new(
            MissionTravelPhase.LoadingMission, sessionId, missionName, sceneName,
            outboundPortalId, outboundPosition, outboundRotation, MissionOutcome.None);
        travelState.ApplySnapshot(incoming);

        bool loaded = await LoadMissionSceneAsync(sessionId, sceneName);
        if (loaded && NetworkRouter.Instance != null)
            NetworkRouter.Instance.SendMessageToServer(new NMS_Client_MissionSceneReady(sessionId, sceneName));
    }

    public void HandleMissionSceneReady(
        ulong steamId, int sessionId, string sceneName, bool requiresEntryCatchup)
    {
        if (!IsWorldManager())
            return;

        if (travelState.Phase == MissionTravelPhase.Returning && sessionId == travelState.SessionId)
        {
            SendCurrentMissionReturnToPeer(steamId, travelState.Snapshot);
            return;
        }

        if (travelState.Phase == MissionTravelPhase.Idle &&
            lastCompletedTravelSnapshot.HasValue &&
            lastCompletedTravelSnapshot.Value.SessionId == sessionId)
        {
            SendCurrentMissionReturnToPeer(steamId, lastCompletedTravelSnapshot.Value);
            return;
        }

        if (sessionId != travelState.SessionId || sceneName != travelState.SceneName)
            return;

        if (travelState.Phase == MissionTravelPhase.MissionActive)
        {
            if (requiresEntryCatchup)
                SendCurrentMissionEntryToLatePeer(steamId);
            return;
        }

        travelState.AddExpectedPeer(steamId, sessionId);
        loadingPeerIds.Add(steamId);
        if (travelState.Acknowledge(steamId, sessionId))
            TryCompleteLoadingAsync().Forget();
    }

    public void RegisterLoadingPeersForSnapshot()
    {
        if (!IsWorldManager() || travelState.Phase != MissionTravelPhase.LoadingMission ||
            NetworkSystem.Instance?.Server == null)
            return;

        foreach (KeyValuePair<ulong, NetworkPlayer> entry in NetworkSystem.Instance.Server.NetworkUsers)
        {
            if (entry.Value == null)
                continue;
            travelState.AddExpectedPeer(entry.Key, travelState.SessionId);
            loadingPeerIds.Add(entry.Key);
        }
    }

    public void NotifyLateJoinTravelReady(MissionTravelSnapshot snapshot)
    {
        if (IsWorldManager() || NetworkRouter.Instance == null)
            return;

        if (snapshot.Phase == MissionTravelPhase.LoadingMission)
        {
            NetworkRouter.Instance.SendMessageToServer(
                new NMS_Client_MissionSceneReady(snapshot.SessionId, snapshot.SceneName, true));
        }
        else if (snapshot.Phase == MissionTravelPhase.MissionActive)
        {
            NetworkRouter.Instance.SendMessageToServer(
                new NMS_Client_MissionSceneReady(snapshot.SessionId, snapshot.SceneName, false));
        }
    }

    private void SendCurrentMissionReturnToPeer(ulong steamId, MissionTravelSnapshot snapshot)
    {
        if (NetworkSystem.Instance?.Server == null ||
            !NetworkSystem.Instance.Server.NetworkUsers.TryGetValue(steamId, out NetworkPlayer player))
            return;

        NetworkRouter.Instance.SendMessageToClient(player, new NMS_Server_ReturnFromMission(
            snapshot.SessionId,
            snapshot.ReturnPosition,
            snapshot.ReturnRotation,
            snapshot.Outcome == MissionOutcome.Succeeded));
    }

    private void SendCurrentMissionEntryToLatePeer(ulong steamId)
    {
        if (missionSpawnPoint == null || NetworkSystem.Instance?.Server == null ||
            !NetworkSystem.Instance.Server.NetworkUsers.TryGetValue(steamId, out NetworkPlayer player))
            return;

        if (!string.IsNullOrEmpty(outboundPortalIdForSession))
        {
            NetworkRouter.Instance.SendMessageToClient(
                player, new NMS_Server_NO_Destroy(outboundPortalIdForSession));
        }
        NetworkRouter.Instance.SendMessageToClient(player, new NMS_Server_EnterMission(
            travelState.SessionId,
            missionSpawnPoint.ShipPosition,
            missionSpawnPoint.ShipRotation,
            travelState.ActivePortalId));
        NetworkRouter.Instance.SendMessageToClient(player, new NMS_Server_NewObject(
            MissionPortalPrefabId,
            travelState.ActivePortalId,
            missionSpawnPoint.GetReturnPortalPosition(travelState.SessionId),
            missionSpawnPoint.ShipRotation,
            0,
            false));
    }

    private async UniTask TryCompleteLoadingAsync()
    {
        if (completingMissionEntry || !travelState.AllPeersLoaded || missionSpawnPoint == null)
            return;

        completingMissionEntry = true;
        try
        {
            string outboundPortalId = travelState.ActivePortalId;
            string returnPortalId = Guid.NewGuid().ToString();
            if (!travelState.EnterMission(returnPortalId))
                return;

            var enter = new NMS_Server_EnterMission(
                travelState.SessionId,
                missionSpawnPoint.ShipPosition,
                missionSpawnPoint.ShipRotation,
                returnPortalId);
            if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
                NetworkRouter.Instance.DistributeMessageToReady(enter);

            HandleEnterMission(enter.SessionId, enter.ShipPosition, enter.ShipRotation, enter.ReturnPortalId);
            DestroyPortal(outboundPortalId);

            NetworkGameObject returnPortal = await NetworkSystem.Instance.CreateNetworkObject(
                MissionPortalPrefabId,
                missionSpawnPoint.GetReturnPortalPosition(enter.SessionId),
                missionSpawnPoint.ShipRotation,
                0,
                networkID: returnPortalId);
            MissionPortal missionPortal = returnPortal?.GetComponent<MissionPortal>();
            if (missionPortal != null)
            {
                // The ship has just been teleported to the mission spawn. If the return
                // portal overlaps its collider, enabling it immediately returns the ship
                // before the mission start hook can run.
                Collider portalCollider = missionPortal.GetComponent<Collider>();
                Bounds portalBounds = portalCollider != null ? portalCollider.bounds : default;
                missionPortal.SetUsable(false);
                ArmReturnPortalWhenShipClearsAsync(missionPortal, portalBounds, enter.SessionId).Forget();
            }
            InvokeMissionStartHook(travelState.MissionName);
        }
        finally
        {
            completingMissionEntry = false;
        }
    }

    private async UniTask ArmReturnPortalWhenShipClearsAsync(MissionPortal portal, Bounds portalBounds, int sessionId)
    {
        if (portal == null || MainSpaceship.Instance == null)
            return;

        while (portal != null && MainSpaceship.Instance != null)
        {
            if (travelState.Phase != MissionTravelPhase.MissionActive || travelState.SessionId != sessionId)
                return;

            bool overlapsShip = false;
            foreach (Collider shipCollider in MainSpaceship.Instance.GetComponentsInChildren<Collider>())
            {
                if (shipCollider != null && shipCollider.enabled && portalBounds.Intersects(shipCollider.bounds))
                {
                    overlapsShip = true;
                    break;
                }
            }

            if (!overlapsShip)
                return;

            await UniTask.Yield();
        }

        if (portal != null && travelState.Phase == MissionTravelPhase.MissionActive &&
            travelState.SessionId == sessionId)
        {
            portal.SetUsable(true);
            portal.ShowWaypoint("Return to Main");
        }
    }

    public void HandleEnterMission(int sessionId, Vector3 shipPosition, Quaternion shipRotation, string returnPortalId)
    {
        if (sessionId != travelState.SessionId || sessionId <= 0)
            return;

        if (travelState.Phase == MissionTravelPhase.LoadingMission)
        {
            MissionTravelSnapshot current = travelState.Snapshot;
            travelState.ApplySnapshot(new MissionTravelSnapshot(
                MissionTravelPhase.MissionActive, sessionId, current.MissionName, current.SceneName,
                returnPortalId, current.ReturnPosition, current.ReturnRotation, current.Outcome));
        }
        else if (travelState.Phase != MissionTravelPhase.MissionActive)
        {
            return;
        }

        missionPauseState ??= new MissionPauseState(mainScenePauseRoots);
        missionPauseState.Pause();
        TeleportShipAndPlayers(shipPosition, shipRotation);
    }

    private static void TeleportShipAndPlayers(Vector3 shipPosition, Quaternion shipRotation)
    {
        MainSpaceship ship = MainSpaceship.Instance;
        if (ship == null)
            return;

        Transform shipTransform = ship.transform;
        PlayerMain[] players = UnityEngine.Object.FindObjectsByType<PlayerMain>(FindObjectsSortMode.None);
        var localPositions = new Vector3[players.Length];
        var localRotations = new Quaternion[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            localPositions[i] = shipTransform.InverseTransformPoint(players[i].transform.position);
            localRotations[i] = Quaternion.Inverse(shipTransform.rotation) * players[i].transform.rotation;
        }

        ship.Teleport(shipPosition, shipRotation);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            Transform playerTransform = players[i].transform;
            playerTransform.SetPositionAndRotation(
                shipTransform.TransformPoint(localPositions[i]),
                shipTransform.rotation * localRotations[i]);
            Rigidbody playerRigidbody = players[i].GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                playerRigidbody.position = playerTransform.position;
                playerRigidbody.rotation = playerTransform.rotation;
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    public async UniTask HandleAbortMissionLoadAsync(int sessionId, string sceneName, string reason)
    {
        if (sessionId != travelState.SessionId || travelState.Phase != MissionTravelPhase.LoadingMission)
            return;

        await UnloadMissionSceneAsync(sceneName);
        travelState.RestoreOutboundPortal();
        SetPortalUsable(travelState.ActivePortalId, true);
        UIManager.Instance?.SetWaypoint(
            travelState.ReturnPosition,
            $"{travelState.MissionName} Portal");
        Debug.LogWarning($"[MissionManager] Mission load aborted: {reason}");
    }

    private async UniTask AbortMissionLoadAsync(int sessionId, string reason)
    {
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
        {
            NetworkRouter.Instance.DistributeMessageToReady(
                new NMS_Server_AbortMissionLoad(sessionId, travelState.SceneName, reason));
        }
        await HandleAbortMissionLoadAsync(sessionId, travelState.SceneName, reason);
    }

    private async UniTask<bool> LoadMissionSceneAsync(int sessionId, string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sessionId != travelState.SessionId)
            return false;

        if (loadedMissionScene.IsValid() && loadedMissionScene.isLoaded &&
            (loadedMissionScene.name != sceneName || loadedMissionSessionId != sessionId))
        {
            await SceneManager.UnloadSceneAsync(loadedMissionScene);
            loadedMissionScene = default;
            loadedMissionSessionId = 0;
            missionSpawnPoint = null;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);
        bool loadedByThisCall = false;
        bool ownedBySession = loadedMissionScene.IsValid() && scene.IsValid() &&
            loadedMissionScene.handle == scene.handle && loadedMissionSessionId == sessionId;
        if (scene.IsValid() && scene.isLoaded && !ownedBySession)
        {
            await SceneManager.UnloadSceneAsync(scene);
            scene = default;
        }
        if (!scene.IsValid() || !scene.isLoaded)
        {
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (operation == null)
                    return false;
                await operation;
                scene = SceneManager.GetSceneByName(sceneName);
                loadedByThisCall = true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MissionManager] Failed loading '{sceneName}': {exception.Message}");
                return false;
            }
        }

        if (sessionId != travelState.SessionId || sceneName != travelState.SceneName ||
            (travelState.Phase != MissionTravelPhase.LoadingMission &&
             travelState.Phase != MissionTravelPhase.MissionActive))
        {
            if (loadedByThisCall && scene.IsValid() && scene.isLoaded)
                await SceneManager.UnloadSceneAsync(scene);
            return false;
        }

        loadedMissionScene = scene;
        loadedMissionSessionId = sessionId;
        missionSpawnPoint = FindSpawnPoint(scene);
        return missionSpawnPoint != null;
    }

    private static MissionSpawnPoint FindSpawnPoint(Scene scene)
    {
        MissionSpawnPoint found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MissionSpawnPoint candidate in root.GetComponentsInChildren<MissionSpawnPoint>(true))
            {
                if (found != null)
                {
                    Debug.LogError($"[MissionManager] Scene '{scene.name}' has multiple MissionSpawnPoints.");
                    return null;
                }
                found = candidate;
            }
        }
        return found;
    }

    private async UniTask UnloadMissionSceneAsync(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
            await SceneManager.UnloadSceneAsync(scene);

        loadedMissionScene = default;
        loadedMissionSessionId = 0;
        missionSpawnPoint = null;
    }

    private void SetPortalUsable(string portalId, bool usable)
    {
        if (NetworkSystem.Instance == null || string.IsNullOrEmpty(portalId) ||
            !NetworkSystem.Instance.FindNetworkIdentity.TryGetValue(portalId, out NetworkIdentity identity))
            return;
        identity.GetComponent<MissionPortal>()?.SetUsable(usable);
    }

    private void DestroyPortal(string portalId)
    {
        if (string.IsNullOrEmpty(portalId) || NetworkSystem.Instance == null ||
            !NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(portalId))
            return;

        if (NetworkSystem.Instance.IsOnline)
            NetworkRouter.Instance.DistributeMessageToReady(new NMS_Server_NO_Destroy(portalId));
        GameCore.Instance.DestroyNetworkObject(portalId);
    }

    private void PruneDisconnectedLoadingPeers()
    {
        if (travelState.Phase != MissionTravelPhase.LoadingMission || NetworkSystem.Instance == null ||
            !NetworkSystem.Instance.IsOnline || NetworkSystem.Instance.Server == null)
            return;

        var disconnected = new List<ulong>();
        foreach (ulong peerId in loadingPeerIds)
        {
            if (!NetworkSystem.Instance.Server.NetworkUsers.ContainsKey(peerId))
                disconnected.Add(peerId);
        }

        foreach (ulong peerId in disconnected)
        {
            loadingPeerIds.Remove(peerId);
            travelState.RemoveExpectedPeer(peerId);
        }
    }

    private static bool IsWorldManager()
    {
        return NetworkSystem.Instance == null || NetworkSystem.Instance.IsWorldManager;
    }

    private static void InvokeMissionStartHook(string missionName)
    {
        EscapeBlackholeMission.OnMissionVoteWon(missionName);
        PeakOfEnergyMission.OnMissionVoteWon(missionName);
    }

    private bool TryBeginReturnFromPortal()
    {
        if (travelState.Phase != MissionTravelPhase.MissionActive)
            return false;

        ReturnToMainAsync().Forget();
        return true;
    }

    public bool ReportMissionEnded(string missionName, bool succeeded)
    {
        if (string.IsNullOrEmpty(missionName) || missionName != travelState.MissionName)
            return false;

        return travelState.RecordOutcome(succeeded ? MissionOutcome.Succeeded : MissionOutcome.Failed);
    }

    public bool RequestActiveMissionFailure()
    {
        if (travelState.Phase != MissionTravelPhase.MissionActive ||
            travelState.Outcome != MissionOutcome.None)
            return false;

        if (EscapeBlackholeMission.Instance != null && EscapeBlackholeMission.Instance.IsMissionActive)
            EscapeBlackholeMission.Instance.FailMissionForReturn();
        else if (PeakOfEnergyMission.Instance != null && PeakOfEnergyMission.Instance.IsMissionActive)
            PeakOfEnergyMission.Instance.FailMissionForReturn();

        if (travelState.Outcome == MissionOutcome.None)
            travelState.RecordOutcome(MissionOutcome.Failed);
        return travelState.Outcome == MissionOutcome.Failed;
    }

    public async UniTask ReturnToMainAsync()
    {
        if (!IsWorldManager() || travelState.Phase != MissionTravelPhase.MissionActive)
            return;

        if (travelState.Outcome == MissionOutcome.None)
            RequestActiveMissionFailure();
        if (!travelState.BeginReturning())
            return;

        int sessionId = travelState.SessionId;
        Vector3 returnPosition = travelState.ReturnPosition;
        Quaternion returnRotation = travelState.ReturnRotation;
        bool succeeded = travelState.Outcome == MissionOutcome.Succeeded;
        string returnPortalId = travelState.ActivePortalId;

        DestroyPortal(returnPortalId);
        var message = new NMS_Server_ReturnFromMission(
            sessionId, returnPosition, returnRotation, succeeded);
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
            NetworkRouter.Instance.DistributeMessageToReady(message);

        await HandleReturnFromMissionAsync(
            sessionId, returnPosition, returnRotation, succeeded);
    }

    public async UniTask HandleReturnFromMissionAsync(
        int sessionId, Vector3 returnPosition, Quaternion returnRotation, bool succeeded)
    {
        if (sessionId != travelState.SessionId ||
            (travelState.Phase != MissionTravelPhase.MissionActive &&
             travelState.Phase != MissionTravelPhase.LoadingMission &&
             travelState.Phase != MissionTravelPhase.Returning))
            return;

        if (travelState.Phase == MissionTravelPhase.LoadingMission)
        {
            MissionTravelSnapshot current = travelState.Snapshot;
            travelState.ApplySnapshot(new MissionTravelSnapshot(
                MissionTravelPhase.Returning,
                sessionId,
                current.MissionName,
                current.SceneName,
                current.ActivePortalId,
                returnPosition,
                returnRotation,
                succeeded ? MissionOutcome.Succeeded : MissionOutcome.Failed));
        }
        else if (travelState.Phase == MissionTravelPhase.MissionActive)
        {
            if (travelState.Outcome == MissionOutcome.None)
                travelState.RecordOutcome(succeeded ? MissionOutcome.Succeeded : MissionOutcome.Failed);
            travelState.BeginReturning();
        }

        TeleportShipAndPlayers(returnPosition, returnRotation);
        missionPauseState?.Restore();
        missionPauseState = null;
        UIManager.Instance?.HideWaypoint();
        lastCompletedTravelSnapshot = travelState.Snapshot;
        await UnloadMissionSceneAsync(travelState.SceneName);
        loadingPeerIds.Clear();
        outboundPortalIdForSession = string.Empty;
        travelState.Reset();
    }

    public async UniTask<bool> ApplyLateJoinTravelSnapshotAsync(MissionTravelSnapshot snapshot)
    {
        if (snapshot.SessionId < travelState.SessionId || !travelState.ApplySnapshot(snapshot))
            return false;

        switch (snapshot.Phase)
        {
            case MissionTravelPhase.Idle:
                return true;

            case MissionTravelPhase.Voting:
                return true;

            case MissionTravelPhase.OutboundPortal:
                if (NetworkSystem.Instance != null &&
                    NetworkSystem.Instance.FindNetworkIdentity.TryGetValue(snapshot.ActivePortalId, out NetworkIdentity outbound))
                    UIManager.Instance?.SetWaypoint(outbound.transform, $"{snapshot.MissionName} Portal");
                else
                    UIManager.Instance?.SetWaypoint(snapshot.ReturnPosition, $"{snapshot.MissionName} Portal");
                return true;

            case MissionTravelPhase.LoadingMission:
                return await LoadMissionSceneAsync(snapshot.SessionId, snapshot.SceneName);

            case MissionTravelPhase.MissionActive:
                if (!await LoadMissionSceneAsync(snapshot.SessionId, snapshot.SceneName) || missionSpawnPoint == null)
                    return false;
                HandleEnterMission(
                    snapshot.SessionId,
                    missionSpawnPoint.ShipPosition,
                    missionSpawnPoint.ShipRotation,
                    snapshot.ActivePortalId);
                return true;

            case MissionTravelPhase.Returning:
                await HandleReturnFromMissionAsync(
                    snapshot.SessionId,
                    snapshot.ReturnPosition,
                    snapshot.ReturnRotation,
                    snapshot.Outcome == MissionOutcome.Succeeded);
                return true;
        }

        return false;
    }
}
