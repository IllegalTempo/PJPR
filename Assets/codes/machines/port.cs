using UnityEngine;

public enum portType
{
    input,
    output,
    both
}
public class Port : Slot //port are slots that will unrealize the item that is attached to it
{
    public storage LinkedStorage;
    public portType type;
    public override void Attach(Item item)
    {
        if (LinkedStorage == null || LinkedStorage.IsFull()) return;

    }

}
