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
    public static GameCore instance;
    public LayerMasks Masks;
    public options option;
    private const string prefabPath = "Prefabs/";
    private const string decorationPath = "Prefabs/Decorations/";
    public Connector connector;
    public PlayerInputAction control;

    public Dictionary<string, string> getPrefab = new Dictionary<string, string> //PrefabID, Path
    {
        { "TestPrefab","testPrefab" },
        { "Meteorite_Test","Meteorite_Test" },
        { "Meteorite_Fragment","Meteorite_Fragment" },
        { "Spaceship","Spaceships/default"},
        { "Spaceship_connector","Spaceships/connector"}
    };
    public Dictionary<string,string> getDecoration = new Dictionary<string, string> 
    {
        { "TestDecoration","testDecoration" },
    }; 


    //Local Player Info
    public PlayerMain localPlayer;
    public Spaceship localSpaceship;
    public NetworkPlayerObject localNetworkPlayer;
    private void Awake()
    {

#if UNITY_EDITOR
        PlayerPrefs.DeleteAll();
#endif
        option = JsonUtility.FromJson<options>(PlayerPrefs.GetString("options", JsonUtility.ToJson(new options())));
        Masks = GetComponent<LayerMasks>();
        
        // Convert the serialized list to dictionary

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
        control = new PlayerInputAction();
        string rebinds = PlayerPrefs.GetString("inputRebinds", string.Empty);
        control.LoadBindingOverridesFromJson(rebinds);
        control.Enable();
    }
    private void OnApplicationQuit()
    {
        string rebinds = control.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("inputRebinds", rebinds);
        PlayerPrefs.SetString("options", option.saveAsJSON());

    }
    public GameObject GetPrefabObject(string PrefabID) //Get the gameobject reference using the PrefabID
    {
        return Resources.Load<GameObject>(prefabPath + getPrefab[PrefabID]);

    }
    public GameObject GetDecoration(string DecorationID)
    {
        return Resources.Load<GameObject>(decorationPath + getDecoration[DecorationID]);
    }
    public NetworkObject spawnNetworkPrefab(string prefabID,ulong owner,string uid,Vector3 pos,Quaternion rot,Transform parent=null) //run by both server and client 
    {
        if (!getPrefab.ContainsKey(prefabID))
        {
            Debug.LogError($"CreateNetworkObject failed: prefabID '{prefabID}' not found in GameCore.getPrefab dictionary.");
            return null;
        }

        GameObject prefab = GetPrefabObject(prefabID);

        GameObject obj = GameObject.Instantiate(prefab, pos, rot, parent);
        NetworkObject nobj = obj.gameObject.GetComponent<NetworkObject>();
        if (nobj == null) {
            nobj = gameObject.AddComponent<NetworkObject>();
        }
        
        nobj.Init(uid, owner,prefabID);
        return nobj;
    }
    public bool IsLocal(ulong id)
    {
        //if (!NetworkSystem.instance.IsOnline)
        //{
        //    return true;
        //}
        return id == localNetworkPlayer.steamID;
    }
}
