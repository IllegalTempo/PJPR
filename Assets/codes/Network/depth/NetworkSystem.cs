using Cysharp.Threading.Tasks;
using Assets.codes.Network;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public partial class NetworkSystem : MonoBehaviour
{
    public bool StartServerOnStart = false;
    [Header("Network Setting")]
    [SerializeField]
    private int _maxPlayer = 2;
    [Header("NetworkData")]
    public static NetworkSystem Instance;
    public NetworkListener NetworkListener;
    public bool IsOnline = false;
    public bool IsServer = true;
    public bool IsWorldManager => !IsOnline || IsServer; //If the machine manage own world, either offline or isServer
    public NetworkInstance CurrentNetworkInstance { get; private set; } = new NetworkInstance();
    public Dictionary<string, NetworkIdentity> FindNetworkIdentity => CurrentNetworkInstance.FindNetworkIdentity;
    [SerializeField] private List<string> FindNetworkObjectKey = new List<string>();
    private readonly Dictionary<string, PrefabDefinition> _networkPrefabsById = new Dictionary<string, PrefabDefinition>();
    private readonly Dictionary<PrefabDefinition, string> _networkPrefabIdsByDefinition = new Dictionary<PrefabDefinition, string>();
    private NetworkPrefabRegistry _networkPrefabRegistry;
    public List<Slot> Slots
    {
        get => CurrentNetworkInstance.Slots;
        set => CurrentNetworkInstance.Slots = value;
    }
    public ulong SteamID;
    public int initState = 0;
    public Lobby CurrentLobby;// Start is called before the first frame update
    private GameClient _client;
    private bool _destroyed = false;
    public const float TIMEOUTSECONDS = 10f;
    public GameClient Client => _client;
    public int MaxPlayer => _maxPlayer;
    private bool _startedAsHost = false;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            NetworkListener = new NetworkListener();
            RebuildNetworkPrefabLookup();

        }
        else
        {
            Debug.Log("Instance Already Exist");
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(gameObject);

    }
    public PrefabDefinition GetPrefabDefinition(string prefabID)
    {
        if (_networkPrefabsById.TryGetValue(prefabID, out PrefabDefinition prefab))
        {
            return prefab;
        }
        else
        {
            Debug.LogError($"PrefabDefinition with ID '{prefabID}' not found.");
            return null;
        }
    }

    public bool TryGetPrefabId(PrefabDefinition prefabDefinition, out string prefabID)
    {
        if (prefabDefinition == null)
        {
            prefabID = null;
            return false;
        }

        if (_networkPrefabIdsByDefinition.TryGetValue(prefabDefinition, out prefabID))
        {
            return true;
        }

        if (_networkPrefabsById.Count == 0)
        {
            RebuildNetworkPrefabLookup();
        }

        return _networkPrefabIdsByDefinition.TryGetValue(prefabDefinition, out prefabID);
    }

    public string GetPrefabId(PrefabDefinition prefabDefinition)
    {
        if (TryGetPrefabId(prefabDefinition, out string prefabID))
        {
            return prefabID;
        }

        Debug.LogError($"PrefabDefinition '{prefabDefinition?.name}' is not mapped in NetworkPrefabRegistry.");
        return null;
    }
    public List<PlayerData> GetPlayerData()
    {
        return CurrentNetworkInstance.Players.Select(p => new PlayerData(
            p.steamID.ToString(),
            p.transform.position,
            p.transform.rotation)).ToList();
    }

    public NetworkPlayerObject GetPlayer(ulong steamId)
    {
        return CurrentNetworkInstance.GetPlayer(steamId);
    }

    public bool RemovePlayer(ulong steamId)
    {
        return CurrentNetworkInstance.RemovePlayer(steamId);
    }
    public T GetComponentOfIdentity<T>(string NetworkID)
    {
        NetworkIdentity identity = FindNetworkIdentity[NetworkID];
        T component = identity.GetComponent<T>();
        if (component == null)
        {
            Debug.LogError($"Component of type {typeof(T)} not found on NetworkIdentity with ID: {NetworkID}");
            
        }
        return component;
    }

    public void RebuildNetworkPrefabLookup()
    {
        _networkPrefabsById.Clear();
        _networkPrefabIdsByDefinition.Clear();

        _networkPrefabRegistry = Resources.Load<NetworkPrefabRegistry>("Prefabs/NetworkPrefabRegistry");
        if (_networkPrefabRegistry == null)
        {
            Debug.LogError("NetworkPrefabRegistry not found at Resources/Prefabs/NetworkPrefabRegistry.");
            return;
        }

        foreach (NetworkPrefabRegistry.Entry entry in _networkPrefabRegistry.Entries)
        {
            if (entry == null || entry.PrefabDefinition == null || entry.PrefabDefinition.itemPrefab == null || string.IsNullOrWhiteSpace(entry.PrefabId))
            {
                continue;
            }

            if (_networkPrefabsById.ContainsKey(entry.PrefabId))
            {
                Debug.LogError($"Duplicate network prefab ID '{entry.PrefabId}' found while building network prefab lookup.");
                continue;
            }

            if (_networkPrefabIdsByDefinition.ContainsKey(entry.PrefabDefinition))
            {
                Debug.LogError($"PrefabDefinition '{entry.PrefabDefinition.name}' is mapped to multiple network prefab IDs.");
                continue;
            }

            _networkPrefabsById.Add(entry.PrefabId, entry.PrefabDefinition);
            _networkPrefabIdsByDefinition.Add(entry.PrefabDefinition, entry.PrefabId);

            NetworkPrefabIdentity identity = entry.PrefabDefinition.itemPrefab.GetComponent<NetworkPrefabIdentity>();
            if (identity != null)
            {
                identity.PrefabID = entry.PrefabId;
            }
        }

        Debug.Log($"Loaded {_networkPrefabsById.Count} network prefab(s) from NetworkPrefabRegistry.");
    }

    //public bool TryGetNetworkPrefab(string prefabID, out GameObject prefab)
    //{
    //    if (_networkPrefabsById.Count == 0)
    //    {
    //        RebuildNetworkPrefabLookup();
    //    }

    //    return _networkPrefabsById.TryGetValue(prefabID, out prefab);
    //}

    //public bool TryGetNetworkPrefabID(GameObject prefab, out string prefabID)
    //{
    //    if (prefab == null)
    //    {
    //        prefabID = null;
    //        return false;
    //    }

    //    if (_networkPrefabIdsByPrefab.Count == 0)
    //    {
    //        RebuildNetworkPrefabLookup();
    //    }

    //    return _networkPrefabIdsByPrefab.TryGetValue(prefab, out prefabID);
    //}
    public void BecomeOnline(bool isServer)
    {
        IsOnline = true;
        IsServer = isServer;
        UnityEngine.Debug.Log(new StackTrace(true));
    }
    private void Update()
    {
        ReceiveData();
    }
#if UNITY_EDITOR
    private void OnExit(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            OnDestroy();
        }
    }
#endif
    private void OnApplicationQuit()
    {
        OnDestroy();
    }


    private void OnDestroy()
    {
        if (_destroyed) return;
        _destroyed = true;
        // Unsubscribe from events
        UnRegisterCallbacks();
        try
        {
            if (_server != null)
            {
                Debug.Log("Destroyed Server");

                _server.DisconnectAll();
                _server = null;
            }
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
            if (SteamClient.IsValid)
            {
                SteamClient.Shutdown();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during NetworkSystem shutdown: {e}");
        }
    }
    private void ReceiveData()
    {
        if (_server != null)
        {
            try
            {
                _server.Receive();
            }
            catch (Exception e)
            {
                Debug.LogError($"Server receive error: {e}");
            }
        }
        else if (_client != null)
        {
            try
            {
                _client.Receive();
            }
            catch (Exception e)
            {
                Debug.LogError($"Client receive error: {e}");
            }
        }
#if UNITY_EDITOR
        // Sync dictionary keys to inspector for debugging
        FindNetworkObjectKey.Clear();
        FindNetworkObjectKey.AddRange(FindNetworkIdentity.Keys);
#endif
    }
    public ulong GetInviteCode()
    {
        return CurrentLobby.Id;
    }
    public async void JoinLobby(ulong id)
    {
        Lobby lobby = new Lobby(id);
        RoomEnter result = await lobby.Join();
        if (result != RoomEnter.Success)
        {
            Debug.Log($"Failed To Join Lobby created by {(new Friend(id)).Name}");
        }
        else
        {
            Debug.Log($"Joined Lobby created by {(new Friend(id)).Name}");

        }

    }
    public Connection GetServerConnection()
    {
        return _client.GetServer();
    }
    //Spawn the network Player
    public async UniTask<NetworkPlayerObject> SpawnPlayer(ulong steamid)
    {
        if (CurrentNetworkInstance.TryGetPlayer(steamid, out NetworkPlayerObject existingPlayer))
        {
            Debug.LogWarning($"Player {steamid} is already spawned. Reusing existing player object.");
            return existingPlayer;
        }

        ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/Player");
        await request;
        GameObject PlayerInstance = request.asset as GameObject;
        int index = CurrentNetworkInstance.PlayerCount;
        NetworkPlayerObject p = Instantiate(PlayerInstance, GameCore.Instance.getPlayerSpawn(), Quaternion.identity).GetComponent<NetworkPlayerObject>();
        await p.Init(steamid, index);
        CurrentNetworkInstance.SetPlayer(steamid, p);

        Debug.Log($"Spawned Player {steamid}");

        return p;
    }
    //private Vector3 getPlayerSpawnPos(int index)
    //{
    //    return GameCore.Instance.GetSpaceshipSpawn(index).position - new Vector3(0,1,0);
    //}
    public void RemoveAllPlayerObject()
    {
        foreach (NetworkPlayerObject g in CurrentNetworkInstance.Players)
        {
            Destroy(g.gameObject);
        }
        CurrentNetworkInstance.ClearPlayers();
    }

    public async UniTask InitializeNetwork()
    {
        if (SteamClient.IsValid)
        {
            Debug.Log("SteamClient already initialized. ");
            return;
        }
        SteamClient.Init(480, true);
        SteamID = SteamClient.SteamId;
        RegisterCallbacks();

        if (StartServerOnStart)
        {
            await StartOnlineHost();
        }
        else
        {

            await StartAsHost();
        }

        Debug.Log("NetworkSystem Initialization Complete");
    }
    
    private async UniTask<bool> WaitForRelayNetwork()
    {
        while (SteamNetworkingUtils.Status != SteamNetworkingAvailability.Current)
        {
            await UniTask.Delay(100);
            if(SteamNetworkingUtils.Status == SteamNetworkingAvailability.Failed)
            {
                Debug.LogError("Failed to initialize Steam Relay Network. Please check your network connection and try again.");
                return false;

            }
        }

        Debug.Log($"Relay network status: {SteamNetworkingUtils.Status}");
        return true;
    }
    
    public async UniTask StartAsHost()
    {
        if (_startedAsHost) return;
        _startedAsHost = true;
        Debug.Log("Starting as Host...");
        ulong steamid = SteamClient.SteamId;
        await SpawnPlayer(steamid); //Add the server player to the player list
        //await SpawnSpaceShip(SaveObject.instance.saved_decorations, steamid);
    }
    //public async UniTask<Spaceship> SpawnSpaceShip(ulong owner)
    //{
    //    return await SpawnSpaceShip(null, owner);
    //}
    private void RegisterCallbacks()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnExit;
#endif
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnFriendJoinLobby;
        Debug.Log("Network Callback registered.");
    }
    private void UnRegisterCallbacks()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnExit;
#endif
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= OnFriendJoinLobby;
        Debug.Log("Network Callback unregistered.");
    }
    public void CreateNewNetworkInstance()
    {
        _startedAsHost = false;
        if (MainSpaceship.Instance != null)
        {
            MainSpaceship.Instance.ResetScene();
        }
        initState = (int)ReadyState.NotReady;
        CurrentNetworkInstance.CleanupScene();
        CurrentNetworkInstance = new NetworkInstance();
        Slots = FindObjectsByType<Slot>(FindObjectsSortMode.None).ToList();
        RegisterSceneNetworkIdentities();
        Debug.Log("Created new network instance");
    }

    private void RegisterSceneNetworkIdentities()
    {
        NetworkIdentity[] sceneIdentities = FindObjectsByType<NetworkIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NetworkIdentity identity in sceneIdentities)
        {
            if (identity == null || identity is NetworkPrefabIdentity)
            {
                continue;
            }

            identity.Register();
        }
    }


    


    
    private async UniTask WaitForSocketReady(int maxAttempts = 10)
    {
        int attempts = 0;
        while (_server == null && attempts < maxAttempts)
        {
            await UniTask.Delay(100);
            attempts++;
        }

        if (_server == null)
        {
            throw new ServerSocketInitializationFailed();
        }

        // Optional: Additional delay for Steam networking to fully bind
        await UniTask.Delay(200);
    }
    private void OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
    {
        if (id == SteamID) return;
        Debug.Log($"Connecting To Relay Server: {ip}:{port}, {id}");
        if (_client == null)
        {

            // Use the port provided by the matchmaking callback
            _client = SteamNetworkingSockets.ConnectRelay<GameClient>(id, port);
            CurrentLobby = lobby;
        }
    }
    private async void OnLobbyEntered(Lobby l)
    {
        if (l.Owner.Id == SteamID) { return; }
        CreateNewNetworkInstance();

        if (_client == null)
        {

            SteamId serverid = new SteamId();
            uint ip = 0;
            ushort port = 0;
            bool haveserver = l.GetGameServer(ref ip, ref port, ref serverid);
            await Task.Delay(1000);

            if (haveserver)
            {
                Debug.Log($"Connecting To Relay Server: {ip}:{port}, {serverid}");
                CurrentLobby = l;
                _server = null;
                // Use the port returned by GetGameServer
                _client = SteamNetworkingSockets.ConnectRelay<GameClient>(serverid, port);
                //print(client.NetworkID);

            }
            else
            {
                Debug.Log($"No Server: {ip}:{port}, {serverid}");

            }
        }
    }
    //when you join someone's lobby from friend invite
    private async void OnFriendJoinLobby(Lobby lobby, SteamId id)
    {
        if (_server != null)
        {
            _server.Close();
        }
        RoomEnter result = await lobby.Join();

        if (result != RoomEnter.Success)
        {
            Debug.Log($"Failed To Join Lobby created by {(new Friend(id)).Name}");
        }
        else
        {
            Debug.Log($"Joined Lobby created by {(new Friend(id)).Name}");

        }

    }

}
