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
        Connector.Instance.ConnectModule((module)item, this);
        this.item = item;
        attachedModule = (module)item;
    }
}
