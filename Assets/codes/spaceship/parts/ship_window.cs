using UnityEngine;

public class ship_window : interactable
{
    public override void OnClicked()
    {
        base.OnClicked();
        float dot = Vector3.Dot(transform.forward,GameCore.instance.localPlayer.transform.position - transform.position);
        if (dot < 0)
        {
            Debug.Log("opp");
        } else
        {
            Debug.Log("same");

        }
    }
}
