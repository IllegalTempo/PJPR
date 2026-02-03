using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LayerMasks))]
public class GameCore : MonoBehaviour
{
    public static GameCore Instance;
    public Dictionary<string,Item> GetItemByUUID = new Dictionary<string,Item>();
    public LocalInfo LocalInfo = new LocalInfo();
    public LayerMasks Masks;
    private void Awake()
    {
        Masks = GetComponent<LayerMasks>();
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
}
