using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LayerMasks))]
public class GameCore : MonoBehaviour
{
    public static GameCore instance;
    public Dictionary<string,Item> GetItemByUUID = new Dictionary<string,Item>();
    public LayerMasks Masks;



    //Local Player Info
    public PlayerMain localPlayer;
    public NetworkPlayerObject localNetworkPlayer;
    private void Awake()
    {
        Masks = GetComponent<LayerMasks>();
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
    public bool IsLocal(int id)
    {
        
        return id == localNetworkPlayer.NetworkID;
    }
}
