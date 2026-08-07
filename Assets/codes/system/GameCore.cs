using Assets.codes.Network;
using Assets.codes.Network.SyncedIdentity;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This is the brain of whole game, constants, global references and functions are stored here. It is also responsible for spawning networked objects and keeping track of local player info.
/// </summary>
[RequireComponent(typeof(LayerMasks))]
public partial class GameCore : MonoBehaviour
{
    private const string _decorationPath = "Prefabs/Decorations/";

    public static GameCore Instance;
    public LayerMasks Masks;
    public recording vc;
    public options Option;
    public PlayerInputAction PlayerControl;

    public Dictionary<string, string> GetDecorationWithID = new Dictionary<string, string>
    {
        { "TestDecoration","testDecoration" },
    };


    //Local Player Info
    public PlayerMain Local_Player;
    //public Spaceship Local_PlayerSpaceship;
    public NetworkPlayerObject Local_NetworkPlayer;
    [SerializeField]
    private GameObject PlayerSpawn;
    //[SerializeField]
    //private Transform[] SpaceshipSpawns;
    //public int CurrentMissionLevel = 0;

    public long RandomSeed;
    private bool startedGame = false;

    public Vector3 getPlayerSpawn()
    {
        return PlayerSpawn.transform.position;
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            StartGame().Forget();
        }
        else
        {
            Destroy(this.gameObject);
        }

    }
    private async UniTask StartGame()
    {
        if (startedGame)
        {
            return;
        }

        startedGame = true;
        await InitPlayerControl();
        await UniTask.WaitUntil(() => NetworkSystem.Instance != null);

        GameInitManager initManager = GameInitManager.Instance;
        if (initManager == null)
        {
            initManager = gameObject.AddComponent<GameInitManager>();
        }

        await initManager.InitializeGameAsync(new GameInitManager.GameInitOptions
        {
            LoadSave = true,
            NetworkSyncTimeoutSeconds = NetworkSystem.TIMEOUTSECONDS
        });
    }
    private async UniTask InitPlayerControl()
    {
#if UNITY_EDITOR
        PlayerPrefs.DeleteAll();
#endif
        Option = JsonUtility.FromJson<options>(PlayerPrefs.GetString("options", JsonUtility.ToJson(new options())));
        PlayerControl = new PlayerInputAction();
        string rebinds = PlayerPrefs.GetString("inputRebinds", string.Empty);
        PlayerControl.LoadBindingOverridesFromJson(rebinds);
        PlayerControl.Enable();
        await UniTask.CompletedTask;
    }
    private void OnApplicationQuit()
    {
        SavePlayerPrefs();

    }

    //public void StartMission(int level, int missionindex)
    //{
    //    GameObject missionPrefab = getMissionWithLevel(level)[missionindex];
    //    GameObject.Instantiate(missionPrefab);

    //}
    private void SavePlayerPrefs()
    {
        string rebinds = PlayerControl.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("inputRebinds", rebinds);
        PlayerPrefs.SetString("options", Option.saveAsJSON());
    }
    //public Transform GetSpaceshipSpawn(int index)
    //{
    //    return SpaceshipSpawns[index];
    //}
    //public bool TryGetNetworkPrefab(string prefabID, out GameObject prefab)
    //{
    //    if (NetworkSystem.Instance != null && NetworkSystem.Instance.TryGetNetworkPrefab(prefabID, out prefab))
    //    {
    //        return true;
    //    }

    //    prefab = null;
    //    return false;
    //}

    //public bool TryGetNetworkPrefabID(GameObject prefab, out string prefabID)
    //{
    //    if (NetworkSystem.Instance != null && NetworkSystem.Instance.TryGetNetworkPrefabID(prefab, out prefabID))
    //    {
    //        return true;
    //    }

    //    prefabID = null;
    //    return false;
    //}

    //public async UniTask<GameObject> GetPrefabObject(string PrefabID) //Get the gameobject reference using the PrefabID
    //{
    //    if (TryGetNetworkPrefab(PrefabID, out GameObject prefab))
    //    {
    //        await UniTask.Yield();
    //        return prefab;
    //    } 

    //    throw new PrefabNotFound(PrefabID);
    //}
    public async UniTask<GameObject> GetDecoration(string DecorationID)
    {
        string decPath = GetDecorationWithID.ContainsKey(DecorationID) ? _decorationPath + GetDecorationWithID[DecorationID] : throw new PrefabNotFound(DecorationID);
        ResourceRequest request = Resources.LoadAsync<GameObject>(decPath);
        await request;
        return request.asset as GameObject;
    }
    //public async UniTask SpawnDecorations(DecorationSaveData[] decs, Spaceship spaceship)
    //{
    //    if (decs != null)
    //    {
    //        foreach (DecorationSaveData dsd in decs)
    //        {
    //            GameObject prefab = await GetDecoration(dsd.DecorationID);
    //            Decoration obj = Instantiate(prefab, spaceship.transform).GetComponent<Decoration>();
    //            obj.OnCreate(spaceship, dsd.DecorationPosition, dsd.DecorationRotation);

    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("Cannot load decorations");
    //    }
    //}
    public async UniTask<NetworkGameObject> spawnNetworkPrefab(string prefabID,ulong owner, string networkID, Vector3 pos, Quaternion rot, Transform parent = null) //run by both server and client 
    {
        Debug.Log($"Created NetworkObject: {prefabID}, networkID: {networkID}");
        PrefabDefinition prefabDef = NetworkSystem.Instance.GetPrefabDefinition(prefabID);
        GameObject prefab = prefabDef.itemPrefab;
        bool isPoolPrefab = prefabDef.IsPoolPrefab;
        GameObject obj = null;
        NetworkGameObject nobj = null;
        if (isPoolPrefab)
        {
            NetworkPrefabPool pool = NetworkSystem.Instance.CurrentNetworkInstance.GetPool(prefabDef);
            if (pool != null)
            {
                nobj = pool.InstantiatePoolNetworkPrefab(networkID, pos, rot, parent);
            }
        }
        if (nobj == null)
        {
            obj = GameObject.Instantiate(prefab, pos, rot, parent);
            nobj = obj.gameObject.GetComponent<NetworkGameObject>();
        }
        if (nobj == null)
        {
            Debug.LogError($"The prefab {prefabID} does not have a NetworkPrefab component attached.");
            return null;
        }

        nobj.OnInstantiate(networkID, prefabID,owner);
        nobj.SetMovement(pos, rot);
        await nobj.Identity.StartTask;
        return nobj;
    }
    
    public void DestroyNetworkObject(string id) //Dont RUN THIS
    {
        NetworkGameObject obj = NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(id) ? NetworkSystem.Instance.FindNetworkIdentity[id].GetComponent<NetworkGameObject>() : null;
        if (obj == null)
        {
            Debug.LogError("Tried to destroy a null NetworkObject.");
            return;
        }
        if(obj.AbstractObject.IsPoolPrefab)
        {
            NetworkSystem.Instance.CurrentNetworkInstance.GetPool(obj.AbstractObject).Return(obj);
            return;
        }
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(obj.Identity.Identifier))
        {
            NetworkSystem.Instance.FindNetworkIdentity.Remove(obj.Identity.Identifier);
        }
        Destroy(obj.gameObject);
    }
    public bool IsLocal(ulong id)
    {
        //if (!NetworkSystem.instance.IsOnline)
        //{
        //    return true;
        //}
        return id == Local_NetworkPlayer.steamID;
    }
    public async UniTask<Texture2D> GetIcon(ulong steamid)
    {
        var icon = await SteamFriends.GetMediumAvatarAsync(steamid);
        if (icon.HasValue)
        {
            return Convert(icon.Value);
        }
        else
        {
            Debug.LogWarning($"Failed to get avatar for SteamID: {steamid}");
            return null;
        }
    }
    public static Texture2D Convert(Image image)
    {
        // Create a new Texture2D
        var avatar = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.ARGB32, false);

        // Set filter type, or else its really blury
        avatar.filterMode = FilterMode.Trilinear;

        // Flip image
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var p = image.GetPixel(x, y);
                avatar.SetPixel(x, (int)image.Height - y, new UnityEngine.Color(p.r / 255.0f, p.g / 255.0f, p.b / 255.0f, p.a / 255.0f));
            }
        }

        avatar.Apply();
        return avatar;
    }
}
