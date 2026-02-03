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
    private bool createLobbyOnStart = true;
    public int maxPlayer = 8;
    [Header("NetworkData")]
    public bool connected = false;
    public static NetworkSystem instance;
    public GameServer server;
    public GameClient client;
    //True if current instance is server
    public bool isServer = true;
    //Network Player Prefab
    public GameObject playerInstance;
    //All player list
    public List<NetworkPlayerObject> playerList = new List<NetworkPlayerObject>();
    //Current Lobby player is in, no matter server or client
    public Lobby currentLobby;
    // Start is called before the first frame update

    
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
        if (playerInstance == null)
        {
            Debug.LogError("PlayerInstance prefab is not set. Cannot spawn player.");
            return null;
        }
        Debug.Log("Spawning Player");
        NetworkPlayerObject p = Instantiate(playerInstance, Vector3.zero, Quaternion.identity).GetComponent<NetworkPlayerObject>();
        if (p == null)
        {
            Debug.LogError("PlayerInstance prefab does not have NetworkPlayerObject component.");
            return null;
        }
        Debug.Log("Successfully Spawned Player! NetworkID:" + networkid);
        p.NetworkID = networkid;
        p.steamID = steamid;
        p.IsLocal = isLocal;
        playerList.Add(p);
        return p;
    }
    public void RemoveAllPlayerObject()
    {
        foreach (NetworkPlayerObject g in playerList)
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
                if(createLobbyOnStart)
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
        isServer = true;
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
        Debug.Log($"Lobby ID: {l} Result: {r} Starting Game Server...");
        l.SetGameServer(SteamClient.SteamId);
        currentLobby = l;
        while(server == null)
        {
            try
            {
                server = SteamNetworkingSockets.CreateRelaySocket<GameServer>(1111);
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


        //Create the local Server Player

    }
    private void OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
    {
        if (id == SteamClient.SteamId) return;
        Debug.Log($"Connecting To Relay Server: {ip}:{port}, {id}");
        if (client == null)
        {
            isServer = false;

            client = SteamNetworkingSockets.ConnectRelay<GameClient>(id);
            currentLobby = lobby;
        }
    }
    private void OnLobbyEntered(Lobby l)
    {
        if (l.Owner.Id == SteamClient.SteamId) { return; }
        NewLobby();

        if (client == null)
        {

            SteamId serverid = new SteamId();
            uint ip = 0;
            ushort port = 0;
            bool haveserver = l.GetGameServer(ref ip, ref port, ref serverid);
            if (haveserver)
            {
                Debug.Log($"Connecting To Relay Server: {ip}:{port}, {serverid}");
                currentLobby = l;
                server = null;
                isServer = false;
                client = SteamNetworkingSockets.ConnectRelay<GameClient>(serverid, 1111);

            }
            else
            {
                Debug.Log($"No Server: {ip}:{port}, {serverid}");

            }



        }
    }
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
