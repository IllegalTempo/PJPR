using UnityEngine;

public class HammerItem : tools
{
    protected new void OnEnable()
    {
        base.OnEnable();

        if (string.IsNullOrWhiteSpace(ItemName))
        {
            ItemName = "Hammer";
        }
    }
    protected override void onUse(Selectable lookat)
    {
        base.onUse(lookat);
        if (lookat is SpaceshipPart ssp)
        {
            ssp.Repair(10f);
        }
    }

}
