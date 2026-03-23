using UnityEngine;

public class HammerItem : tools
{
    protected override void onUse(Selectable lookat)
    {
        base.onUse(lookat);
        if (lookat is SpaceshipPart ssp)
        {
            ssp.Repair(10f);
        }
    }

}
