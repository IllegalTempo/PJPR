using Assets.codes.system;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// This is the brain of whole game, constants, global references and functions are stored here. It is also responsible for spawning networked objects and keeping track of local player info.
/// </summary>
[RequireComponent(typeof(LayerMasks))]
public partial class GameCore : MonoBehaviour
{
    private const string _prefabPath = "Prefabs/";
    private const string _decorationPath = "Prefabs/Decorations/";

    public static GameCore Instance;
    public LayerMasks Masks;
    public recording vc;
    public options Option;
    public Connector Connector;
    public PlayerInputAction PlayerControl;
    public Dictionary<string, string> GetPrefabWithID = new Dictionary<string, string> //PrefabID, Path
    {
        { "TestPrefab","testPrefab" },
        { "Meteorite_Test","Meteorite_Test" },
        { "Meteorite_Fragment","Meteorite_Fragment" },
        { "Spaceship","Spaceships/default"},
        { "Spaceship_connector","Spaceships/connector"}
    };
    public Dictionary<string, string> GetDecorationWithID = new Dictionary<string, string>
    {
        { "TestDecoration","testDecoration" },
    };


    //Local Player Info
    public PlayerMain Local_Player;
    public Spaceship Local_PlayerSpaceship;
    public NetworkPlayerObject Local_NetworkPlayer;
    [SerializeField]
    private Transform[] SpaceshipSpawns;
    [SerializeField]
    public WorldReference WorldReference;
    [SerializeField]
    private GameObject[][] MissionManagerPrefabs;
    public int CurrentMissionLevel = 0;

    public long RandomSeed;
    private void Awake()
    {

        InitPlayerControl();
        // Convert the serialized list to dictionary

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

    }
    private void InitPlayerControl()
    {
#if UNITY_EDITOR
        PlayerPrefs.DeleteAll();
#endif
        Option = JsonUtility.FromJson<options>(PlayerPrefs.GetString("options", JsonUtility.ToJson(new options())));
        PlayerControl = new PlayerInputAction();
        string rebinds = PlayerPrefs.GetString("inputRebinds", string.Empty);
        PlayerControl.LoadBindingOverridesFromJson(rebinds);
        PlayerControl.Enable();
    }
    private void OnApplicationQuit()
    {
        SavePlayerPrefs();

    }
    private GameObject[] getMissionWithLevel(int level)
    {
        return MissionManagerPrefabs[level];
    }

    public void StartMission(int level, int missionindex)
    {
        GameObject missionPrefab = getMissionWithLevel(level)[missionindex];
        GameObject.Instantiate(missionPrefab);

    }
    private void SavePlayerPrefs()
    {
        string rebinds = PlayerControl.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("inputRebinds", rebinds);
        PlayerPrefs.SetString("options", Option.saveAsJSON());
    }
    public Transform GetSpaceshipSpawn(int index)
    {
        return SpaceshipSpawns[index];
    }
    public async UniTask<GameObject> GetPrefabObject(string PrefabID) //Get the gameobject reference using the PrefabID
    {
        string prefabPath = GetPrefabWithID.ContainsKey(PrefabID) ? _prefabPath + GetPrefabWithID[PrefabID] : throw new PrefabNotFound(PrefabID);
        ResourceRequest request = Resources.LoadAsync<GameObject>(prefabPath);
        await request;
        return request.asset as GameObject;
    }
    public async UniTask<GameObject> GetDecoration(string DecorationID)
    {
        string decPath = GetDecorationWithID.ContainsKey(DecorationID) ? _decorationPath + GetDecorationWithID[DecorationID] : throw new PrefabNotFound(DecorationID);
        ResourceRequest request = Resources.LoadAsync<GameObject>(decPath);
        await request;
        return request.asset as GameObject;
    }
    public async UniTask SpawnDecorations(DecorationSaveData[] decs, Spaceship spaceship)
    {
        if (decs != null)
        {
            foreach (DecorationSaveData dsd in decs)
            {
                GameObject prefab = await GetDecoration(dsd.DecorationID);
                Decoration obj = Instantiate(prefab, spaceship.transform).GetComponent<Decoration>();
                obj.OnCreate(spaceship, dsd.DecorationPosition, dsd.DecorationRotation);

            }
        }
        else
        {
            Debug.Log("Cannot load decorations");
        }
    }
    public async UniTask<NetworkObject> spawnNetworkPrefab(string prefabID, ulong owner, string uid, Vector3 pos, Quaternion rot, Transform parent = null) //run by both server and client 
    {
        Debug.Log($"Created NetworkObject: {prefabID}, uid: {uid}");
        GameObject prefab = await GetPrefabObject(prefabID);

        GameObject obj = GameObject.Instantiate(prefab, pos, rot, parent);
        NetworkObject nobj = obj.gameObject.GetComponent<NetworkObject>();
        if (nobj == null)
        {
            nobj = gameObject.AddComponent<NetworkObject>();
        }

        nobj.Init(uid, owner, prefabID);
        nobj.SetMovement(pos, rot);
        return nobj;
    }
    public bool IsLocal(ulong id)
    {
        //if (!NetworkSystem.instance.IsOnline)
        //{
        //    return true;
        //}
        return id == Local_NetworkPlayer.steamID;
    }
    public Transform GetWorldReferenceTransform()
    { return WorldReference.transform; }
    private async UniTask<Texture2D> GetIcon(ulong steamid)
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
