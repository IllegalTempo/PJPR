using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Assets.codes.Network.Messages;
using UnityEngine;

public class GameInitManager : MonoBehaviour
{
    public static GameInitManager Instance;

    private UniTaskCompletionSource worldSnapshotApplied;
    private bool worldInitCompleteReceived;
    private bool worldSnapshotApplyReceived;
    private bool initializationStarted;

    public GameInitializationState State { get; private set; } = GameInitializationState.None;
    public bool IsReady => State == GameInitializationState.Ready;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public struct GameInitOptions
    {
        public bool LoadSave;
        public string SaveSlot;
        public float NetworkSyncTimeoutSeconds;
    }

    public async UniTask InitializeGameAsync(GameInitOptions options)
    {
        if (initializationStarted)
        {
            await UniTask.WaitUntil(() => State == GameInitializationState.Ready || State == GameInitializationState.Failed);
            return;
        }

        initializationStarted = true;

        try
        {
            await BootstrapAsync();
            await InitializeNetworkAsync();

            if (NetworkSystem.Instance.IsWorldManager)
            {
                await InitializeAuthoritativeWorldAsync(options);
            }
            else
            {
                await InitializeClientWorldAsync(options);
            }

            SetState(GameInitializationState.InitializingPlayers);
            await UniTask.Yield();

            SetState(GameInitializationState.Ready);
            UIManager.Instance?.LoadingComplete();
        }
        catch (Exception ex)
        {
            SetState(GameInitializationState.Failed);
            Debug.LogError($"Game initialization failed: {ex}");
            throw;
        }
    }

    private async UniTask BootstrapAsync()
    {
        SetState(GameInitializationState.Bootstrapping);
        UIManager.Instance?.ShowLoadingScreen("Starting game...");
        await UniTask.WaitUntil(() => GameCore.Instance != null && NetworkSystem.Instance != null);
    }

    private async UniTask InitializeNetworkAsync()
    {
        SetState(GameInitializationState.NetworkStarting);
        UIManager.Instance?.ChangeLoadingStatus("Initializing network...", 0.2f);
        await NetworkSystem.Instance.InitializeNetwork();
    }

    private async UniTask InitializeAuthoritativeWorldAsync(GameInitOptions options)
    {
        GameSaveData saveData = GameSaveSystem.CreateDefaultSaveData();

        if (options.LoadSave)
        {
            SetState(GameInitializationState.LoadingSave);
            UIManager.Instance?.ChangeLoadingStatus("Loading save...", 0.45f);
            bool saveLoaded = GameSaveSystem.Instance != null
                ? await GameSaveSystem.Instance.LoadGame()
                : await GameSaveSystem.LoadCurrentGame();

            if (!saveLoaded)
            {
                throw new InvalidOperationException("Could not load save data.");
            }

            saveData = GameSaveSystem.SaveData ?? GameSaveSystem.CreateDefaultSaveData();
        }

        SetState(GameInitializationState.ApplyingWorld);
        UIManager.Instance?.ChangeLoadingStatus("Building world...", 0.7f);
        await ApplySaveDataAsync(saveData);

        SetState(GameInitializationState.SyncingNetwork);
        UIManager.Instance?.ChangeLoadingStatus("Preparing world sync...", 0.9f);
        await UniTask.Yield();
    }

    private async UniTask InitializeClientWorldAsync(GameInitOptions options)
    {
        SetState(GameInitializationState.SyncingNetwork);
        UIManager.Instance?.ChangeLoadingStatus("Waiting for host world state...", 0.45f);

        worldSnapshotApplied = new UniTaskCompletionSource();
        worldInitCompleteReceived = false;
        worldSnapshotApplyReceived = false;

        NetworkRouter.Instance.SendMessageToServer(new NMS_Client_RequestWorldState(), NetworkSendProfiles.Critical);

        float timeout = options.NetworkSyncTimeoutSeconds > 0
            ? options.NetworkSyncTimeoutSeconds
            : NetworkSystem.TIMEOUTSECONDS;

        await worldSnapshotApplied.Task.Timeout(TimeSpan.FromSeconds(timeout));
    }

    public async UniTask ApplySaveDataAsync(GameSaveData saveData)
    {
        if (saveData == null)
        {
            saveData = GameSaveSystem.CreateDefaultSaveData();
        }

        await Server_SpawnNetworkObjectFromSave(saveData);
        await UniTask.Yield();
        InitSlotRelationFromSave(saveData.SlotRelationships);
    }

    public async UniTask Server_SpawnNetworkObjectFromSave(GameSaveData saveData)
    {
        foreach (NetworkObjectSnapshot snapshot in saveData.NetworkObjects)
        {
            await NetworkSystem.Instance.CreateNetworkObject(snapshot.PrefabId, snapshot.Position, snapshot.Rotation, snapshot.Owner, uid: snapshot.Uid);
        }
    }

    public void InitSlotRelationFromSave(IEnumerable<SlotSnapshot> saveData)
    {
        foreach (SlotSnapshot snapshot in saveData)
        {
            if (snapshot.AttachedItemId == string.Empty)
            {
                Debug.Log("No item attached to slot: " + snapshot.SlotId);
                continue;
            }
            Slot slot = NetworkSystem.Instance.GetComponentOfIdentity<Slot>(snapshot.SlotId);
            Item attachedItem = NetworkSystem.Instance.GetComponentOfIdentity<Item>(snapshot.AttachedItemId);
            if (attachedItem != null && slot != null && attachedItem != null)
            {
                slot.Attach(attachedItem, snapshot.rotation);
            }
            else
            {
                Debug.LogError($"Failed to attach item {snapshot.AttachedItemId} to slot {snapshot.SlotId}. Slot or Item not found.");
            }
        }
    }

    public void SendWorldStateToClient(NetworkPlayer player)
    {
        if (player == null)
        {
            return;
        }

        NetworkRouter.Instance.SendMessageToClient(player, new NMS_Server_WorldInitBegin(), NetworkSendProfiles.Critical);
        NetworkRouter.Instance.SendMessageToClient(player, new NMS_Server_SyncScene(NetworkSystem.Instance.Slots), NetworkSendProfiles.Critical);
        NetworkRouter.Instance.SendMessageToClient(player, new NMS_Server_WorldInitComplete(), NetworkSendProfiles.Critical);
    }

    public void NotifyWorldInitBegin()
    {
        SetState(GameInitializationState.SyncingNetwork);
        worldInitCompleteReceived = false;
        worldSnapshotApplyReceived = false;
    }

    public void NotifyWorldSnapshotApplied()
    {
        worldSnapshotApplyReceived = true;
        if (worldInitCompleteReceived)
        {
            worldSnapshotApplied?.TrySetResult();
        }
    }

    public void NotifyWorldInitComplete()
    {
        worldInitCompleteReceived = true;
        if (worldSnapshotApplyReceived)
        {
            worldSnapshotApplied?.TrySetResult();
        }
    }

    private void SetState(GameInitializationState state)
    {
        State = state;
        Debug.Log($"Game initialization state: {state}");
    }
}

public enum GameInitializationState
{
    None,
    Bootstrapping,
    NetworkStarting,
    LoadingSave,
    ApplyingWorld,
    SyncingNetwork,
    InitializingPlayers,
    Ready,
    Failed
}
