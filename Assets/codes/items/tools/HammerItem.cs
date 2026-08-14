using System.Linq;
using UnityEngine;

public class HammerItem : Tool
{
    public override void OnUsingInteract(Selectable target)
    {
        if (target.GetComponent<SpaceshipPart>() != null)
        {
            target.GetComponent<SpaceshipPart>().Repair(10f);
        }
    }


}
