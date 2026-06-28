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
        module moduleObject = (module)item;
        Quaternion worldRotation = moduleObject.transform.rotation;

        // Reparent to slot
        moduleObject.transform.SetParent(transform);

        // Reset position to slot's origin but preserve world rotation
        moduleObject.transform.localPosition = Vector3.zero;
        moduleObject.transform.rotation = worldRotation;  // Restore world rotation

        moduleObject.DisableRB();

        moduleObject.Init(this);
        this.item = item;
        attachedModule = (module)item;

        Connector.Instance.ConnectModule((module)item, this);
    }
}
