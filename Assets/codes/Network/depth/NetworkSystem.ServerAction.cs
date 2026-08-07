using Assets.codes.items;
using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using System;
using UnityEngine;

public partial class NetworkSystem
{
    public GameServer Server => _server;
    private GameServer _server;

    /// <summary>
    /// Creates a networked object on the server and distributes it to all clients. This method should only be called on the server.
    /// </summary>
    /// <param name="prefabID"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    /// <param name="owner"></param>
    /// <param name="parent"></param>
    /// <param name="isCombining"></param>
    /// <param name="networkID"></param>
    /// <returns></returns>
    public async UniTask<NetworkGameObject> CreateNetworkObject(string prefabID, Vector3 pos, Quaternion rot, ulong owner, Transform parent = null, bool isCombining = false, string networkID = null)
    { 
        if (IsOnline && !IsServer) return null;
        if(networkID == null)
        {
            networkID = Guid.NewGuid().ToString();

        }

        NetworkGameObject nobj = await GameCore.Instance.spawnNetworkPrefab(prefabID, owner, networkID, pos, rot, parent);
        if (IsOnline)
        {
            NetworkRouter.Instance.DistributeMessageToReady(new NMS_Server_NewObject(prefabID, networkID, pos, rot, owner,isCombining));

        }

        return nobj;


    }
    public void ServerDestroyNetworkItem(Item item)
    {
        string identifier = item.GetNetworkObject().Identity.Identifier;
        var msg = new NMS_Server_NO_Destroy(identifier);
        NetworkRouter.Instance.DistributeMessageToReady(msg, sendType: NetworkSendProfiles.Critical);
        GameCore.Instance.DestroyNetworkObject(identifier);

    }
    public async UniTask<NetworkGameObject> CreateNetworkObject(PrefabDefinition prefab, Vector3 pos, Quaternion rot, ulong owner, Transform parent = null, bool isCombining = false, string networkID = null)
    {
        return await CreateNetworkObject(GetPrefabId(prefab), pos, rot, owner, parent, isCombining, networkID);
    }
    public async UniTask<CombinedProcessableItem> CreateNewCombinedItem(Item it1, Item it2)
    {
        if (IsOnline && !IsServer) return null;
        string networkID = Guid.NewGuid().ToString();
        string it1id = it1.GetNetworkObject().Identity.Identifier;
        string it2id = it2.GetNetworkObject().Identity.Identifier;

        string spawningprefabID = "CPI";
        NetworkGameObject nobj = await GameCore.Instance.spawnNetworkPrefab(spawningprefabID, 0, networkID, it1.transform.position, it1.transform.rotation, null);
        if (IsOnline)
        {
            NetworkRouter.Instance.DistributeMessageToReady(new NMS_Server_NewObject(spawningprefabID, networkID, it1.transform.position, it1.transform.rotation, 0, true, it1id, it2id));

        }
        CombinedProcessableItem cpi = nobj.GetComponent<CombinedProcessableItem>();
        cpi.CombineIntoThis(it1);
        cpi.CombineIntoThis(it2);
        return cpi;
    }
    //public async UniTask<Spaceship> SpawnSpaceShip(DecorationSaveData[] decs, ulong owner) //run by server
    //{
    //    if (IsOnline && !Instance.IsServer) return null;
    //    NetworkPlayerObject player = GetPlayer(owner);
    //    Transform spawn = GameCore.Instance.GetSpaceshipSpawn(player.index);

    //    Spaceship ss = (await CreateNetworkObject("Spaceship", spawn.position, spawn.rotation, owner)).GetComponent<Spaceship>();
    //    await GameCore.Instance.SpawnDecorations(decs, ss);
    //    //ss.OwnerPlayer = GetPlayer(owner);
    //    //ss.OwnerPlayer.spaceship = ss;

    //    return ss;

    //}
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
        CreateNewNetworkInstance();
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
