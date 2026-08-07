
using Assets.codes.spaceship;
using UnityEngine;

public class Module : SpaceshipPart
{
    
    private ModuleSlot ConnectedTo;

    public void Init(ModuleSlot connectedTo)
    {
        OnInstall(connectedTo);
        if(netObj.AbstractObject is not ModuleDefinition)
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
    protected override void Update()
    {
        base.Update();
        if (ConnectedTo != null)
        {
            ModuleUpdate();
        }
    }
    protected virtual void ModuleUpdate()
    {
        // Called every frame when the module is installed on the spaceship
    }

}
public class Module<T> : Module
{
    private T data;
    public void SetData(T newData)
    {
        data = newData;
        OnDataChanged(newData);
    }
    protected virtual void OnDataChanged(T newData)
    {
        // Called when the module data is changed
    }
    public T GetModuleData()
    {
        return data;
    }
}