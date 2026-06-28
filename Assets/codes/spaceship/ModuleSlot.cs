using UnityEngine;
using System.Collections;

public class ModuleSlot : Slot
{
    [SerializeField]
    private ModuleSlotName slotName;
    public int slotIndex => (int)slotName;


    private module attachedModule;
    public override void Attach(Item item)
    {
        Debug.Log($"Attaching item {item.name} to slot {slotName}");
        base.Attach(item);
        module moduleObject = (module)item;
        Quaternion worldRotation = moduleObject.transform.rotation;

        // Reparent to slot
        moduleObject.transform.rotation = worldRotation;  // Restore world rotation


        moduleObject.Init(this);
        attachedModule = (module)item;

        Connector.Instance.ConnectModule((module)item, this);
    }
}
