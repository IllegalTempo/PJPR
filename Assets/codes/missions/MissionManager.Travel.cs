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
    private MissionSpawnPoint missionSpawnPoint;
    private int nextTravelSessionId;
    private bool completingMissionEntry;

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

        Vector3 direction = UnityEngine.Random.onUnitSphere;
        Vector3 portalPosition = MissionPortalPlacement.Calculate(
            MainSpaceship.Instance.transform.position,
            outboundPortalMinimumDistance,
            Mathf.Max(outboundPortalMinimumDistance, outboundPortalMaximumDistance),
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
        portal.GetComponent<MissionPortal>()?.ShowWaypoint("Mission Portal");
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

        MissionTravelSnapshot incoming = new(
            MissionTravelPhase.LoadingMission, sessionId, missionName, sceneName,
            travelState.ActivePortalId, travelState.ReturnPosition, travelState.ReturnRotation, MissionOutcome.None);
        travelState.ApplySnapshot(incoming);

        bool loaded = await LoadMissionSceneAsync(sessionId, sceneName);
        if (loaded && NetworkRouter.Instance != null)
            NetworkRouter.Instance.SendMessageToServer(new NMS_Client_MissionSceneReady(sessionId, sceneName));
    }

    public void HandleMissionSceneReady(ulong steamId, int sessionId, string sceneName)
    {
        if (!IsWorldManager() || sceneName != travelState.SceneName)
            return;

        if (travelState.Acknowledge(steamId, sessionId))
            TryCompleteLoadingAsync().Forget();
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
                missionSpawnPoint.ReturnPortalPosition,
                missionSpawnPoint.ShipRotation,
                0,
                networkID: returnPortalId);
            returnPortal?.GetComponent<MissionPortal>()?.ShowWaypoint("Return to Main");
            InvokeMissionStartHook(travelState.MissionName);
        }
        finally
        {
            completingMissionEntry = false;
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
        MainSpaceship.Instance?.Teleport(shipPosition, shipRotation);
    }

    public async UniTask HandleAbortMissionLoadAsync(int sessionId, string sceneName, string reason)
    {
        if (sessionId != travelState.SessionId || travelState.Phase != MissionTravelPhase.LoadingMission)
            return;

        await UnloadMissionSceneAsync(sceneName);
        travelState.RestoreOutboundPortal();
        SetPortalUsable(travelState.ActivePortalId, true);
        UIManager.Instance?.SetWaypoint(travelState.ReturnPosition, "Mission Portal");
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

        if (loadedMissionScene.IsValid() && loadedMissionScene.isLoaded && loadedMissionScene.name != sceneName)
            await SceneManager.UnloadSceneAsync(loadedMissionScene);

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (operation == null)
                    return false;
                await operation;
                scene = SceneManager.GetSceneByName(sceneName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MissionManager] Failed loading '{sceneName}': {exception.Message}");
                return false;
            }
        }

        loadedMissionScene = scene;
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

    // Implemented with mission lifecycle reporting in Task 4.
    private bool TryBeginReturnFromPortal() => false;

    public async UniTask HandleReturnFromMissionAsync(
        int sessionId, Vector3 returnPosition, Quaternion returnRotation, bool succeeded)
    {
        await UniTask.CompletedTask;
    }
}
