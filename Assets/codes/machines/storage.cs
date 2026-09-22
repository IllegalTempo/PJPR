using System.Collections.Generic;
using UnityEngine;

public class Storage //This is only a data type class, used it in some thing like a machine when the machine need to store something
{
    private readonly string name;
    private readonly int capacity;
    private readonly List<PrefabDefinition> content;

    public Storage(string name,int size = 1)
    {
        capacity = Mathf.Max(1, size);
        this.name = name;
        content = new List<PrefabDefinition>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            content.Add(null);
        }
    }
    public bool IsFull()
    {
        for (int i = 0; i < content.Count; i++)
        {
            if (content[i] == null)
            {
                return false;
            }
        }

        return true;
    }
    public PrefabDefinition[] GetItems()
    {
        return content.ToArray();

    }
    public string GetStorageName()
    {
        return name;
    }
    public void AddItem(Item item)
    {
        for (int i = 0; i < capacity; i++)
        {
            if (content[i] == null)
            {
                content[i] = GameCore.Instance.RemoveItemFromWorld(item.GetNetworkObject());
                return;
            }
        }
        Debug.LogWarning("Storage is full, cannot add item.");
    }
    public void RemoveItem(int index,Vector3 outputPos)
    {
        if (index < 0 || index >= capacity)
        {
            Debug.LogWarning("Invalid index, cannot remove item.");
            return;
        }
        GameCore.Instance.RealizeItemDefinition(content[index], outputPos, Quaternion.identity);

        content[index] = null;
    }

}

[System.Obsolete("Use Storage.")]
public class storage : Storage
{
    public storage(string name, int size = 1) : base(name, size)
    {
    }
}
