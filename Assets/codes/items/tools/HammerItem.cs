using Assets.codes.items;
using UnityEngine;

public class HammerItem : usableItem
{
    protected new void OnEnable()
    {
        base.OnEnable();

        if (string.IsNullOrWhiteSpace(ItemName))
        {
            ItemName = "Hammer";
        }
    }
    public override void OnInteract()
    {
        base.OnInteract();
        if (GameCore.instance.localPlayer.seenObject is SpaceshipPart ssp)
        {
            ssp.Repair(10f);
        }

    }

}
