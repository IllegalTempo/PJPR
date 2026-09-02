using UnityEngine;

public class HammerItem : Tool
{
    public override void OnUsingInteract(Selectable target)
    {
        SpaceshipPart ssp = target != null ? target.GetComponent<SpaceshipPart>() : null;
        if (ssp != null)
        {
            ssp.Repair(10f);
            return;
        }

        // Peak Of Energy: the hammer is the EVA melee weapon.
        if (target is RustEater rustEater)
        {
            rustEater.HammerHit();
            return;
        }

        if (target is PeakOfEnergyMeteorite meteorite)
        {
            meteorite.HammerHit();
        }
    }

}
