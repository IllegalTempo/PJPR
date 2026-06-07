
using UnityEngine;

public class module : SpaceshipPart
{
    protected Connector connected;
    public string PrefabID { get; private set; }

    public void Init(string prefabID, Connector connectedTo)
    {
        PrefabID = prefabID;
        connected = connectedTo;
        OnInstall();
    }
    
    public virtual void OnInstall()
    {
        // Called when the module is installed on the spaceship

    }
    public virtual void ModuleUpdate()
    {

    }
}
