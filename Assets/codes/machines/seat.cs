using UnityEngine;

public class seat : Interactable
{
    bool isOccupied = false;
    public override void OnInteract_press(PlayerMain who)
    {
        base.OnInteract_press(who);

        if (isOccupied)
        {
            who.Stand();
            isOccupied = false;
        } else
        {
            who.Sit(this);
            isOccupied = true;

        }
    }
}
