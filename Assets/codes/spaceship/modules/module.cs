using UnityEngine;

public class module : MonoBehaviour
{
    public string PrefabID { get; private set; }

    public void Init(string prefabID)
    {
        PrefabID = prefabID;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
