using UnityEngine;

public class Seat : Interactable
{
    bool isOccupied = false;
    private PlayerMain currentPlayer;
    public override void OnInteract_press(PlayerMain who)
    {
        base.OnInteract_press(who);

        if (isOccupied)
        {
            who.Stand();
            isOccupied = false;
            currentPlayer = null;
        } else
        {
            who.Sit(this);
            isOccupied = true;
            currentPlayer = who;

        }
    }
    
}
