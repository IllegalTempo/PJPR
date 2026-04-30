using UnityEngine;

public class storage //This is only a data type class, used it in some thing like a machine when the machine need to store something
{
    string name;
    int capacity;
    ItemDefinition[] content;

    public storage(string name,int size = 1)
    {
        capacity = size;
        this.name = name;
        content = new ItemDefinition[capacity];
    }
    public ItemDefinition[] GetItems()
    {
        return content;

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
                content[i] = GameCore.Instance.RemoveItemFromWorld(item);
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
