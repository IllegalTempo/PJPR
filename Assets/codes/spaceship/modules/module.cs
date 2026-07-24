
using Assets.codes.spaceship;
using UnityEngine;

public class module : SpaceshipPart
{
    
    private ModuleSlot ConnectedTo;


    public void Init(ModuleSlot connectedTo)
    {
        OnInstall(connectedTo);
        if(AbstractItem is not ModuleDefinition)
        {
            Debug.LogError("<!> AbstractItem is not a ModuleDefinition");
        }
    }
    
    public virtual void OnInstall(ModuleSlot connectedTo)
    {
        // Called when the module is installed on the spaceship
        ConnectedTo = connectedTo;
        connectedTo.attachedModule = this;

    }
    public virtual void ModuleUpdate()
    {

    }
}