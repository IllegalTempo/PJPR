using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Identifiers;
using UnityEngine.Rendering.Universal;

public partial class NetworkSystem : MonoBehaviour
{
    public bool StartServerOnStart = false;
    [Header("Network Setting")]
    [SerializeField]
    private int _maxPlayer = 2;
    [Header("NetworkData")]
    public bool Connected = false;
    public static NetworkSystem Instance;
    public bool IsOnline = false;
    public bool IsServer = true;
    public Dictionary<string, NetworkObject> FindNetworkObject = new Dictionary<string, NetworkObject>();
    [SerializeField] private List<string> FindNetworkObjectKey = new List<string>();
    //All player list
    public Dictionary<ulong, NetworkPlayerObject> PlayerList = new Dictionary<ulong, NetworkPlayerObject>();
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
        }
        else
        {
            Debug.Log("Instance Already Exist");
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        InitializeNetwork().Forget();



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
        FindNetworkObjectKey.AddRange(FindNetworkObject.Keys);
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
        ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/Player");
        await request;
        GameObject PlayerInstance = request.asset as GameObject;
        int index = PlayerList.Count;
        NetworkPlayerObject p = Instantiate(PlayerInstance, getPlayerSpawnPos(index), Quaternion.identity).GetComponent<NetworkPlayerObject>();
        p.Init(steamid, index);
        PlayerList.Add(steamid, p);

        Debug.Log($"Spawned Player {steamid}");

        return p;
    }
    private Vector3 getPlayerSpawnPos(int index)
    {
        return GameCore.Instance.GetSpaceshipSpawn(index).position - new Vector3(0,1,0);
    }
    public void RemoveAllPlayerObject()
    {
        foreach (NetworkPlayerObject g in PlayerList.Values)
        {
            Destroy(g.gameObject);
        }
        PlayerList.Clear();
    }

    private async UniTask InitializeNetwork()
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
            GameCore.Instance.Connector.gameObject.SetActive(false);

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
        await SpawnSpaceShip(SaveObject.instance.saved_decorations, steamid);
    }
    public async UniTask<Spaceship> SpawnSpaceShip(ulong owner)
    {
        return await SpawnSpaceShip(null, owner);
    }
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
    private void ResetScene()
    {

        _startedAsHost = false;
        GameCore.Instance.Connector.ResetScene();
        initState = (int)ReadyState.NotReady;
        RemoveAllPlayerObject();
        FindNetworkObject.Where(kvp => kvp.Value && !kvp.Value.Preset).ToList().ForEach(kvp => { Destroy(kvp.Value.gameObject); FindNetworkObject.Remove(kvp.Key); });
        Debug.Log("Cleaned up scene");
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
        ResetScene();

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
                IsOnline = true;
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
