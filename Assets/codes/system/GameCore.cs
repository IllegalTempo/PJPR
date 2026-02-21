using Cysharp.Threading.Tasks;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// General Important method are saved here...
/// </summary>
[RequireComponent(typeof(LayerMasks))]
public class GameCore : MonoBehaviour
{
    public static GameCore INSTANCE;
    public LayerMasks Masks;
    public options Option;
    private const string _prefabPath = "Prefabs/";
    private const string _decorationPath = "Prefabs/Decorations/";
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
    public Dictionary<string,string> GetDecorationWithID = new Dictionary<string, string> 
    {
        { "TestDecoration","testDecoration" },
    }; 


    //Local Player Info
    public PlayerMain Local_Player;
    public Spaceship Local_PlayerSpaceship;
    public NetworkPlayerObject Local_NetworkPlayer;
    private void Awake()
    {
        InitPlayerControl();
        // Convert the serialized list to dictionary

        if (INSTANCE == null)
        {
            INSTANCE = this;
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
    private void SavePlayerPrefs()
    {
        string rebinds = PlayerControl.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("inputRebinds", rebinds);
        PlayerPrefs.SetString("options", Option.saveAsJSON());
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
    public async UniTask<NetworkObject> spawnNetworkPrefab(string prefabID,ulong owner,string uid,Vector3 pos,Quaternion rot,Transform parent=null) //run by both server and client 
    {
        Debug.Log($"Created NetworkObject: {prefabID}, uid: {uid}");
        GameObject prefab = await GetPrefabObject(prefabID);

        GameObject obj = GameObject.Instantiate(prefab, pos, rot, parent);
        NetworkObject nobj = obj.gameObject.GetComponent<NetworkObject>();
        if (nobj == null) {
            nobj = gameObject.AddComponent<NetworkObject>();
        }
        
        nobj.Init(uid, owner,prefabID);
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
}
