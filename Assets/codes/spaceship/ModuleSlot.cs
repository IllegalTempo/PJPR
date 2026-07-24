using UnityEngine;
using System.Collections;
using Assets.codes.Network.SyncedIdentity;
using Assets.codes.spaceship;
using System;

public class ModuleSlot : Slot
{


    public module attachedModule;
    public ModuleController moduleController;


    public override void Attach(Item item,Quaternion rot)
    {
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
        module moduleObject = (module)item;

        ItemDefinition it = moduleObject.AbstractItem;
        if (it is ModuleDefinition md)
        {
            int slotIndex = MainSpaceship.Instance.GetModuleSlotIndex(this);
            if (slotIndex < 0)
            {
                Debug.LogWarning($"Module slot {Identity.Identifier} is not registered on MainSpaceship. Cannot spawn control prefab.");
                return;
            }

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
