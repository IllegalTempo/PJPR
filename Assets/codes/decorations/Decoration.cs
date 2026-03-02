using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Decoration can be picked up by ONLY the owner of its spaceship, netobj.owner is always the spaceship owner.
/// </summary>
public class Decoration : Selectable
{
    public string DecorationID;
    public string DecorationName;
    public string DecorationDescription;

    public override void OnClicked()
    {
        base.OnClicked();

    }
    public void OnCreate(Spaceship createdon,Vector3 pos,Quaternion rot)
    {
        GameCore.Instance.Local_PlayerSpaceship.Decorations.Add(this);
        transform.position = pos;
        transform.rotation = rot;
    }
}
