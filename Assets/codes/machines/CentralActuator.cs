using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CentralActuator : Port
{
    [Serializable]
    public class ItemProcessByItemDefinition
    {
        public PrefabDefinition itemDefinition;
        public UnityEvent<PrefabDefinition> onProcess;
    }

    [SerializeField]
    private List<ItemProcessByItemDefinition> processItemByItemDefinition = new List<ItemProcessByItemDefinition>();

    private readonly Dictionary<PrefabDefinition, UnityEvent<PrefabDefinition>> processItemLookup = new Dictionary<PrefabDefinition, UnityEvent<PrefabDefinition>>();

    private void Awake()
    {
        RebuildProcessItemLookup();
    }

    private void OnValidate()
    {
        RebuildProcessItemLookup();
    }

    private void RebuildProcessItemLookup()
    {
        processItemLookup.Clear();

        foreach (ItemProcessByItemDefinition process in processItemByItemDefinition)
        {
            if (process == null || process.itemDefinition == null)
            {
                continue;
            }

            if (processItemLookup.ContainsKey(process.itemDefinition))
            {
                Debug.LogWarning("Duplicate central actuator item process for: " + process.itemDefinition.itemName, this);
                continue;
            }

            processItemLookup.Add(process.itemDefinition, process.onProcess);
        }
    }

    private void ProcessItem(Item item)
    {
        PrefabDefinition objtype = item.GetNetworkObject().AbstractObject;
        Debug.Log("THIS ITEM IS " + objtype.itemName);

        if (processItemLookup.TryGetValue(objtype, out UnityEvent<PrefabDefinition> handler))
        {
            handler?.Invoke(item.GetNetworkObject().AbstractObject);
            return;
        }

        Debug.Log("Unhandled Item: " + objtype.itemName);
    }

    public override void Attach(Item item, Quaternion rot)
    {
        base.Attach(item, rot);
        ProcessItem(item);
    }








    public void Process_WaterCube()
    {
        MainSpaceship.Instance.WaterLevel += 1;
    }
}
