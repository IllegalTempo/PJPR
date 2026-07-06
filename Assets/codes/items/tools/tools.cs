using UnityEngine;
using System.Collections;

public class tools : Item, IUsable
{
    protected virtual void onUse(Selectable lookat)
    {

    }
    public void OnInteract_press(PlayerMain who)
    {
        if (who.seenObject != null)
        {
            onUse(who.seenObject);

        }
    }
}
