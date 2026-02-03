using System;
using UnityEngine;
using Steamworks;
using System.Threading.Tasks;
using Steamworks.Data;
using UnityEditor;
using System.Collections.Generic;

public class NetworkSystem : MonoBehaviour
{
    [Header("Network Setting")]
    [SerializeField]
    private bool CreateLobbyOnStart = true;
    public int MaxPlayer = 2;
    [Header("NetworkData")]
    public bool Connected = false;
    public static NetworkSystem instance;
    public GameServer server;
    public GameClient client;
    //True if current instance is server
    public bool IsServer = true;
    //Network Player Prefab
    public GameObject PlayerInstance;
    public Dictionary<string,NetworkObject> FindNetworkObject = new Dictionary<string, NetworkObject>();
    //All player list
    public List<NetworkPlayerObject> PlayerList = new List<NetworkPlayerObject>();
    //Current Lobby player is in, no matter server or client
    public Lobby CurrentLobby;
    // Start is called before the first frame update
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

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.Log("Instance Already Exist");
            Destroy(this.gameObject);
        }
    }
    //Spawn the network Player
    public NetworkPlayerObject SpawnPlayer(bool isLocal, int networkid, ulong steamid)
    {
        Debug.Log("SpawnPlayer Called " + "steamid:" + steamid);
        if (PlayerInstance == null)
        {
            Debug.LogError("PlayerInstance prefab is not set. Cannot spawn player.");
            return null;
        }
        Debug.Log("Spawning Player");
        NetworkPlayerObject p = Instantiate(PlayerInstance).GetComponent<NetworkPlayerObject>();
        if (p == null)
        {
            Debug.LogError("PlayerInstance prefab does not have NetworkPlayerObject component.");
            return null;
        }
        p.NetworkID = networkid;
        p.steamID = steamid;
        p.IsLocal = isLocal;
        p.gameObject.name = "Player_" + networkid;
        if (p.IsLocal)
        {
            GameCore.instance.localNetworkPlayer = p;
        }
        PlayerList.Add(p);
        return p;
    }
    public void RemoveAllPlayerObject()
    {
        foreach (NetworkPlayerObject g in PlayerList)
        {
            Destroy(g.gameObject);
        }
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        try
        {
            if (!SteamClient.IsValid)
            {
                SteamClient.Init(480, true);
                SteamNetworkingUtils.InitRelayNetworkAccess();
                Debug.Log("Steam Initialized");
                if(CreateLobbyOnStart)
                {
                    CreateGameLobby();

                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Steam initialization failed: {e}");
        }
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnExit;
#endif
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnFriendJoinLobby;
    }
    //GO ONLINE!
    public async void CreateGameLobby()
    {
        bool success = await CreateLobby();
        if (!success)
        {
            Debug.Log("Create Lobby Failed");
        }
    }
    private void NewLobby()
    {
        RemoveAllPlayerObject();
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


    private bool destroyed = false;
    private void OnDestroy()
    {
        if (destroyed) return;
        destroyed = true;
        // Unsubscribe from events
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= OnFriendJoinLobby;


        try
        {
            if (server != null)
            {
                Debug.Log("Destroyed Server");
                server.DisconnectAll();
                server = null;
            }
            if (client != null)
            {
                client.Close();
                client = null;
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
    public async Task<bool> CreateLobby()
    {
        if (!SteamClient.IsValid)
        {
            Debug.LogError("SteamClient is not initialized. Cannot create lobby.");
            return false;
        }
        NewLobby();
        IsServer = true;
        client = null;
        try
        {
            var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(8);
            if (!createLobbyOutput.HasValue)
            {
                Debug.LogError("Lobby created but not correctly instantiated");
                throw new Exception();
            }
            Debug.Log("Successfully Created Lobby");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to create multiplayer lobby");
            Debug.LogError(exception.ToString());
            return false;
        }
    }
    private void Update()
    {
        if (server != null)
        {
            try
            {
                server.Receive();
            }
            catch (Exception e)
            {
                Debug.LogError($"Server receive error: {e}");
            }
        }
        if (client != null)
        {
            try
            {
                client.Receive();
            }
            catch (Exception e)
            {
                Debug.LogError($"Client receive error: {e}");
            }
        }
    }

    private async void OnLobbyCreated(Result r, Lobby l)
    {
        l.SetFriendsOnly();
        l.SetJoinable(true);
        l.Owner = new Friend(SteamClient.SteamId);
        Debug.Log($"Lobby ID: {l.Id} Result: {r} Starting Game Server...");
        // Publish the relay port as part of the lobby so clients receive it
        l.SetGameServer(SteamClient.SteamId);
        CurrentLobby = l;
        //MainScreenUI.instance.InviteCodeDisplay.text = NetworkSystem.instance.GetInviteCode().ToString();

        while (server == null)
        {
            try
            {
                server = SteamNetworkingSockets.CreateRelaySocket<GameServer>();
                Debug.Log($"Successfully created Game Server");

            }
            catch (Exception ex)
            {
                Debug.LogError($"Please Restart your game Client | Error: {ex}");
                await Task.Delay(5000);

            }
        }
        
        await Task.Delay(300);
        Debug.Log($"Server: {server}");
    }
    private void OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
    {
        if (id == SteamClient.SteamId) return;
        Debug.Log($"Connecting To Relay Server: {ip}:{port}, {id}");
        if (client == null)
        {
            IsServer = false;

            // Use the port provided by the matchmaking callback
            client = SteamNetworkingSockets.ConnectRelay<GameClient>(id, port);
            CurrentLobby = lobby;
        }
    }
    private async void OnLobbyEntered(Lobby l)
    {
        if (l.Owner.Id == SteamClient.SteamId) { return; }
        NewLobby();

        if (client == null)
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
                server = null;
                IsServer = false;
                // Use the port returned by GetGameServer
                client = SteamNetworkingSockets.ConnectRelay<GameClient>(serverid, port);
                print(client.NetworkID);

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
        if (server != null)
        {
            server.Close();
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
