using UnityEngine;
using System.Collections;

public interface IUsable
{

    public void OnInteract_press(PlayerMain who);
    public virtual void OnInteract_release(PlayerMain who)
    {

    }
}