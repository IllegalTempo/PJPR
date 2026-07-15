using UnityEngine;
using System.Collections;
using Assets.codes.Network.SyncedIdentity;
using Assets.codes.spaceship;
using System;

public class ModuleSlot : Slot
{
    [SerializeField]
    private ModuleSlotName slotName;
    public int slotIndex => (int)slotName;


    public module attachedModule;
    public ModuleController moduleController;


    public override void Attach(Item item,Quaternion rot)
    {
        Debug.Log($"Attaching item {item.name} to slot {slotName}");
        base.Attach(item,rot);
        module moduleObject = (module)item;
        // Reparent to slot
        moduleObject.transform.rotation = rot;  // Restore world rotation


        moduleObject.Init(this);
        
        MainSpaceship.Instance.ConnectModule((module)item, this);
    }
    public Vector3 GetModuleControlSpawnPoint()
    {
        return MainSpaceship.Instance.transform.position;
    }
    public override async void ServerActionOnAttach(Item item, Quaternion rot)
    {
        base.ServerActionOnAttach(item, rot);
        Debug.Log($"ServerActionOnAttach called for item {item.name} on slot {slotName}");
        module moduleObject = (module)item;

        ItemDefinition it = moduleObject.AbstractItem;
        if (it is ModuleDefinition md)
        {
            moduleController = (await NetworkSystem.Instance.CreateNetworkObject(md.controlPrefabID, GetModuleControlSpawnPoint(), Quaternion.identity, 0, uid:$"ModuleSlot_{slotIndex}_{it.itemName}_{Guid.NewGuid()}")).GetComponent<ModuleController>();
        }
        else
        {
            Debug.LogWarning($"Module {moduleObject.name} does not have a ModuleDefinition. Cannot spawn control prefab.");
        }
    }
    public override void Detach()
    {
        base.Detach();
        attachedModule = null;
        moduleController = null;
    }
    public override void ServerActionOnDetach()
    {
        base.ServerActionOnDetach();
        if (moduleController != null)
        {
            NetworkSystem.Instance.ServerDestroyNetworkItem(moduleController.GetComponent<Item>());
        }

    }
}
