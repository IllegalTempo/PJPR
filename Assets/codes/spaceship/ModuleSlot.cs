using UnityEngine;
using System.Collections;

public class ModuleSlot : Slot
{
    [SerializeField]
    private ModuleSlotName slotName;
    public int slotIndex => (int)slotName;


    private module attachedModule;
    public override void Attach(Item item,Quaternion rot)
    {
        Debug.Log($"Attaching item {item.name} to slot {slotName}");
        base.Attach(item,rot);
        module moduleObject = (module)item;
        // Reparent to slot
        moduleObject.transform.rotation = rot;  // Restore world rotation


        moduleObject.Init(this);
        attachedModule = (module)item;

        MainSpaceship.Instance.ConnectModule((module)item, this);
    }
}
