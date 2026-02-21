using Assets.codes.items;
using UnityEngine;

public class HammerItem : Item,IUsable
{
    protected new void OnEnable()
    {
        base.OnEnable();

        if (string.IsNullOrWhiteSpace(ItemName))
        {
            ItemName = "Hammer";
        }
    }
    public void OnInteract(PlayerMain who)
    {
        if (GameCore.INSTANCE.Local_Player.seenObject is SpaceshipPart ssp)
        {
            ssp.Repair(10f);
        }

    }

}
