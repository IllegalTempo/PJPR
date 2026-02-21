using Cysharp.Threading.Tasks;
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

public class NetworkSystem : MonoBehaviour
{
    [Header("Network Setting")]
    [SerializeField]
    private int _maxPlayer = 2;
    [Header("NetworkData")]
    public bool Connected = false;
    public static NetworkSystem INSTANCE;
    public bool IsOnline = false;//True if current instance is server
    public bool IsServer = true;
    public Dictionary<string, NetworkObject> FindNetworkObject = new Dictionary<string, NetworkObject>();
    [SerializeField] private List<string> FindNetworkObjectKey = new List<string>();
    //All player list
    public Dictionary<ulong, NetworkPlayerObject> PlayerList = new Dictionary<ulong, NetworkPlayerObject>();
    public ulong PlayerId;

    public bool initRoom = false;//Current Lobby player is in, no matter server or client
    public Lobby CurrentLobby;// Start is called before the first frame update
    private GameServer _server;
    private GameClient _client;
    private bool _destroyed = false;

    public GameServer Server => _server;
    public GameClient Client => _client;
    public int MaxPlayer => _maxPlayer;
    void Awake()
    {
        if (INSTANCE == null)
        {
            INSTANCE = this;
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
        else if(_client != null)
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
        bool isLocal = steamid == PlayerId;
        ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/Player");
        await request;
        GameObject PlayerInstance = request.asset as GameObject;
        NetworkPlayerObject p = Instantiate(PlayerInstance).GetComponent<NetworkPlayerObject>();
        p.steamID = steamid;
        p.IsLocal = isLocal;
        p.gameObject.name = "Player_" + steamid;
        PlayerList.Add(steamid, p);
        Debug.Log($"Spawned Player {steamid}");

        return p;
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
        RegisterCallbacks();
        if (SteamClient.IsValid)
        {
            Debug.Log("SteamClient already initialized. ");
            return;
        }
        SteamClient.Init(480, true);
        PlayerId = SteamClient.SteamId;
        SteamNetworkingUtils.InitRelayNetworkAccess(); 
        Debug.Log("SteamClient Initialized, Waiting for relay network...");
        bool relayReady = await WaitForRelayNetwork(); 
        if (relayReady) 
        { 
            Debug.Log("SteamRelayNetwork Initialized,"); 
        } else 
        {
            Debug.LogError("Failed to initialize relay network. NetworkSystem initialization failed.");
            return; 
        }
        bool lobbyReady = await CreateLobby(); if (lobbyReady) { Debug.Log("Lobby System Ready"); } else { return; }
        Debug.Log("NetworkSystem Initialization Complete");
    }
    private async UniTask<bool> WaitForRelayNetwork(float timeoutSeconds = 10f)
    {
        float elapsed = 0f;
        while (SteamNetworkingUtils.Status != SteamNetworkingAvailability.Current)
        {
            if (elapsed >= timeoutSeconds)
            {
                Debug.LogWarning($"Failed to initialize relay network. Relay network timeout. Status: {SteamNetworkingUtils.Status}");
                return false;
            }

            await UniTask.Delay(100);
            elapsed += 0.1f;
        }

        Debug.Log($"Relay network status: {SteamNetworkingUtils.Status}");
        return true;
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
        initRoom = false;
        RemoveAllPlayerObject();
        FindNetworkObject
    .Where(kvp => kvp.Value && !kvp.Value.InScene)
    .ToList()
    .ForEach(kvp =>
    {
        Destroy(kvp.Value.gameObject);
        FindNetworkObject.Remove(kvp.Key);
    });
        Debug.Log("Cleaned up scene");
    }


    public async UniTask<bool> CreateLobby()
    {
        if (!SteamClient.IsValid)
        {
            Debug.LogError("SteamClient is not initialized. Cannot create lobby.");
            return false;
        }
        ResetScene();
        _client = null;
        try
        {
            var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(8);

            if (!createLobbyOutput.HasValue)
            {
                Debug.LogError("Lobby created but not correctly instantiated");
                throw new InvalidLobbyCreated();
            }
            Debug.Log("Successfully Created Lobby");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.ToString());
            return false;
        }
    }
    

    private async void OnLobbyCreated(Result r, Lobby l)
    {
        l.SetFriendsOnly();
        l.SetJoinable(true);
        l.Owner = new Friend(PlayerId);
        Debug.Log($"Lobby ID: {l.Id} Result: {r} Starting Game Server...");
        // Publish the relay port as part of the lobby so clients receive it
        l.SetGameServer(PlayerId);
        CurrentLobby = l;
        //MainScreenUI.instance.InviteCodeDisplay.text = NetworkSystem.instance.GetInviteCode().ToString();

        try
        {
            _server = SteamNetworkingSockets.CreateRelaySocket<GameServer>();
            await WaitForSocketReady();
            Debug.Log($"Successfully created Game Server");
            await _server.onOnline();
            Debug.Log("Game Server is Online");

        }
        catch (Exception ex)
        {
            Debug.LogError($"Please Restart your game Client | Error: {ex}");

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
        if (id == PlayerId) return;
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
        if (l.Owner.Id == PlayerId) { return; }
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
