using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using UnityEngine;

public partial class NetworkSystem
{
    public GameServer Server => _server;
    private GameServer _server;
    public async UniTask<NetworkObject> CreateWorldReferenceNetworkObject(string prefabID, Vector3 pos, Quaternion rot, ulong owner, bool dontcreateinInit = false)
    {
        NetworkObject nobj = await CreateNetworkObject(prefabID, pos, rot, owner, GameCore.Instance.GetWorldReferenceTransform(), dontcreateinInit);
        return nobj;
    }
    public async UniTask<NetworkObject> CreateNetworkObject(string prefabID, Vector3 pos, Quaternion rot, ulong owner, Transform parent = null, bool dontcreateinInit = false) //Server Only
    { //more check added

        if (IsOnline && !IsServer) return null;
        string uid = Guid.NewGuid().ToString();

        NetworkObject nobj = await GameCore.Instance.spawnNetworkPrefab(prefabID, owner, uid, pos, rot, parent);
        ServerSend.NewObject(prefabID, uid, pos, rot, owner);

        return nobj;

    }
    public async UniTask<Spaceship> SpawnSpaceShip(DecorationSaveData[] decs, ulong owner) //run by server
    {
        if (IsOnline && !Instance.IsServer) return null;
        NetworkPlayerObject player = PlayerList[owner];
        Transform spawn = GameCore.Instance.GetSpaceshipSpawn(player.index);

        Spaceship ss = (await CreateNetworkObject("Spaceship", spawn.position, spawn.rotation, owner)).GetComponent<Spaceship>();
        await GameCore.Instance.SpawnDecorations(decs, ss);
        //ss.OwnerPlayer = PlayerList[owner];
        //ss.OwnerPlayer.spaceship = ss;

        return ss;

    }
    private async void OnLobbyCreated(Result r, Lobby l)
    {
        l.SetFriendsOnly();
        l.SetJoinable(true);
        l.Owner = new Friend(SteamID);
        Debug.Log($"Lobby ID: {l.Id} Result: {r} Starting Game Server...");
        // Publish the relay port as part of the lobby so clients receive it
        l.SetGameServer(SteamID);
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
    private async UniTask StartOnlineHost()
    {
        UIManager.Instance.ShowLoadingScreen("Waiting for Wormhole Network to respond...");

        SteamNetworkingUtils.InitRelayNetworkAccess();
        Debug.Log("SteamClient Initialized, Waiting for relay network...");
        bool networkready = await WaitForRelayNetwork();
        if (networkready)
        {
            Debug.Log("SteamRelayNetwork Initialized,");
            UIManager.Instance.ChangeLoadingStatus("Wormhole Network Approved... Creating a brand new wormhole", 0.5f);
            bool lobbyReady = await CreateLobby();
            if (lobbyReady)
            {
                Debug.Log("Lobby System Ready");
                UIManager.Instance.LoadingComplete();
            }
            else { return; }

        }
        else
        {
            UIManager.Instance.ChangeLoadingStatus("Wormhole Network Rejected... Please check your Appliances and restart this system (Maybe you are in eduroam, some uni banned steam relay)", 0f);

        }
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
}
