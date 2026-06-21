using UnityEngine;
using System.Collections;


public class ModuleSlot : slot
{
    [SerializeField]
    private ModuleSlotName slotName;
    public int slotIndex => (int)slotName;


    private module attachedModule;
    public override void AttachItem(Item item)
    {
        Debug.Log($"Attaching item {item.name} to slot {slotName}");
        Connector.Instance.ConnectModule((module)item, this);
        attachedModule = (module)item;
    }
}
