using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// General Important method are saved here...
/// </summary>
[RequireComponent(typeof(LayerMasks))]
public class GameCore : MonoBehaviour
{
    public static GameCore instance;
    public LayerMasks Masks;

    private const string prefabPath = "Prefabs/";
    private const string decorationPath = "Prefabs/Decorations/";
    public Dictionary<string, string> getPrefab = new Dictionary<string, string> //PrefabID, Path
    {
        { "TestPrefab","testPrefab" },
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
    {
        if (NetworkSystem.instance.IsServer) return null;
        GameObject prefab = GetPrefabObject(prefabID);
        GameObject obj = Instantiate(prefab, pos, rot);
        NetworkObject nobj = obj.gameObject.AddComponent<NetworkObject>();
        string uid = Guid.NewGuid().ToString();
        nobj.Init(uid, obj);
        
        ServerSend.NewObject(prefabID, nobj.Identifier, pos, rot);

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
