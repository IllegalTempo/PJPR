using UnityEngine;
using System.Collections;

public interface IUsable
{

    public virtual void OnInteract_press(PlayerMain who)
    {
        if (who == null) return;
    }
    public virtual void OnInteract_release(PlayerMain who)
    {

    }
}