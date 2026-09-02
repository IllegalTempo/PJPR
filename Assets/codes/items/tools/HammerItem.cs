using UnityEngine;

public class HammerItem : tools
{
    protected override void onUse(Selectable lookat)
    {
        base.onUse(lookat);
        if (lookat is SpaceshipPart ssp)
        {
            ssp.Repair(10f);
            return;
        }

        // Peak Of Energy: the hammer is the EVA melee weapon.
        if (lookat is RustEater rustEater)
        {
            rustEater.HammerHit();
            return;
        }

        if (lookat is PeakOfEnergyMeteorite meteorite)
        {
            meteorite.HammerHit();
        }
    }

}
