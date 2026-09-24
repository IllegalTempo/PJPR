using UnityEngine;
using System.Collections;
using Assets.codes.Network.SyncedIdentity;
using Assets.codes.spaceship;
using System;

public class ModuleSlot : Slot
{


    public Module attachedModule;
    public ModuleController moduleController;
    public override void Attach(Item item,Quaternion rot)
    {
        base.Attach(item,rot);
        Module moduleObject = (Module)item;
        // Reparent to slot
        moduleObject.transform.rotation = rot;  // Restore world rotation


        moduleObject.Init(this);
        
        MainSpaceship.Instance.ConnectModule(moduleObject, this);
    }
    public override async void ServerActionOnAttach(Item item, Quaternion rot)
    {
        base.ServerActionOnAttach(item, rot);
        Module moduleObject = (Module)item;
        if (moduleController != null)
        {
            Debug.LogWarning($"Module slot {Identity.Identifier} already has a control prefab. Skipping duplicate spawn for {moduleObject.name}.");
            return;
        }

        Debug.Log($"Spawning control prefab for module {moduleObject.name} at slot {Identity.Identifier}");
        PrefabDefinition it = moduleObject.GetNetworkObject()?.AbstractObject;
        if (it is ModuleDefinition md)
        {
            Slot assignedControllerSlot = MainSpaceship.Instance.GetAvailableControllerSlot(null);
            if (assignedControllerSlot == null)
            {
                Debug.LogWarning($"No available controller slot for module {moduleObject.name}. Cannot spawn control prefab.");
                return;
            }

            ModuleController spawnedController = (await NetworkSystem.Instance.CreateNetworkObject(
                md.controlPrefab,
                assignedControllerSlot.transform.position,
                assignedControllerSlot.transform.rotation,
                0,
                networkID: ModuleControllerSlotLink.CreateNetworkId(this))).GetComponent<ModuleController>();
            Item controllerItem = spawnedController.GetComponent<Item>();
            assignedControllerSlot.Attach(controllerItem, Quaternion.identity);
            moduleController = spawnedController;
            MainSpaceship.Instance.AssignControllerSlot(moduleController, assignedControllerSlot);
            moduleController.Initialize(this);
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
        //moduleController = null;
    }
    public override void ServerActionOnDetach()
    {
        base.ServerActionOnDetach();
        if (moduleController != null)
        {
            moduleController.GetComponent<Item>().AttachedSlot?.Detach();
            NetworkSystem.Instance.ServerDestroyNetworkItem(moduleController.GetComponent<Item>());
            MainSpaceship.Instance.ReleaseControllerSlot(moduleController);
            moduleController = null;
        }

    }
}
