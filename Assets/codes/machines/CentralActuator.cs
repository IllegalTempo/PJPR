using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CentralActuator : Port
{
    [Serializable]
    public class ItemProcessByItemDefinition
    {
        public ItemDefinition itemDefinition;
        public UnityEvent<ItemDefinition> onProcess;
    }

    [SerializeField]
    private List<ItemProcessByItemDefinition> processItemByItemDefinition = new List<ItemProcessByItemDefinition>();

    private readonly Dictionary<ItemDefinition, UnityEvent<ItemDefinition>> processItemLookup = new Dictionary<ItemDefinition, UnityEvent<ItemDefinition>>();

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
        Debug.Log("THIS ITEM IS " + item.AbstractItem.itemName);
        ItemDefinition itemDef = item.AbstractItem;

        if (processItemLookup.TryGetValue(itemDef, out UnityEvent<ItemDefinition> handler))
        {
            handler?.Invoke(item.AbstractItem);
            return;
        }

        Debug.Log("Unhandled Item: " + item.AbstractItem.itemName);
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
