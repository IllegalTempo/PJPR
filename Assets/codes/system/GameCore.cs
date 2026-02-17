using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public Dictionary<string, string> getPrefab = new Dictionary<string, string> //PrefabID, Path
    {
        { "TestPrefab","testPrefab" },
        { "Meteorite_Test","Meteorite_Test" },
        { "Meteorite_Fragment","Meteorite_Fragment" },
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
    }
    private void OnApplicationQuit()
    {
        string rebinds = localPlayer.control.SaveBindingOverridesAsJson();
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
    /// <summary>
    /// Server Only. Good method to create a network object such as Meteorite.
    /// </summary>
    /// <param name="prefabID"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    /// <returns></returns>
    public NetworkObject CreateNetworkObject(string prefabID, Vector3 pos, Quaternion rot) //Server Only
    { //more check added
        NetworkSystem networkSystem = NetworkSystem.instance;
        if (networkSystem != null && !networkSystem.IsServer) return null;

        if (!getPrefab.ContainsKey(prefabID))
        {
            Debug.LogError($"CreateNetworkObject failed: prefabID '{prefabID}' not found in GameCore.getPrefab dictionary.");
            return null;
        }

        GameObject prefab = GetPrefabObject(prefabID);
        if (prefab == null)
        {
            Debug.LogError($"CreateNetworkObject failed: Resources prefab not found at 'Resources/{prefabPath}{getPrefab[prefabID]}'.");
            return null;
        }

        GameObject obj = Instantiate(prefab, pos, rot);
        NetworkObject nobj = obj.gameObject.AddComponent<NetworkObject>();
        string uid = Guid.NewGuid().ToString();
        nobj.Init(uid, obj);

        if (networkSystem != null && networkSystem.server != null)
        {
            ServerSend.NewObject(prefabID, nobj.Identifier, pos, rot);
        }

        return nobj;

    }
    public Spaceship SpawnSpaceShip(DecorationSaveData[] decs)
    {
        Spaceship ss = Instantiate(GetPrefabObject("Spaceship")).GetComponent<Spaceship>();
        if (decs != null)
        {
            foreach (DecorationSaveData dsd in decs)
            {
                GameObject obj = Instantiate(GetDecoration(dsd.DecorationID),localSpaceship.transform);
                obj.transform.localPosition = dsd.DecorationPosition;
                obj.transform.localRotation = dsd.DecorationRotation;

            }
        }
        else
        {
            Debug.Log("Cannot load decorations");
        }
        return ss;

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
