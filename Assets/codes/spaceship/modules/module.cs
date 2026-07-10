
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

    }
    public virtual void ModuleUpdate()
    {

    }
}
public enum ModuleSlotName
{
    back = 0,
    left1 = 1,
    left2 = 2,
    right1 = 3,
    right2 = 4,
    up = 5,
    back_left = 6,
    back_right = 7,
    below = 8,
}