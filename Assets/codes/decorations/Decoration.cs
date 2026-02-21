using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Decoration can be picked up by ONLY the owner of its spaceship, netobj.owner is always the spaceship owner.
/// </summary>
public class Decoration : Item
{
    public string DecorationID;
    public string DecorationName;
    public string DecorationDescription;

    public override void OnClicked()
    {
        base.OnClicked();
        // Item is not in inventory, so pick it up
        if(GameCore.INSTANCE.IsLocal(netObj.Owner))
        PickUpItem();

    }
    public void OnCreate(Spaceship createdon,Vector3 pos,Quaternion rot)
    {
        GameCore.INSTANCE.Local_PlayerSpaceship.GetDecorationByUUID_onShip.Add(netObj.Identifier, this);
        transform.position = pos;
        transform.rotation = rot;
    }
}
