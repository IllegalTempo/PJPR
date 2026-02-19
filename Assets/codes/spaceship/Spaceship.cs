using System.Collections.Generic;
using UnityEngine;

public class Spaceship : NetworkObject
{
    public List<SpaceshipPart> parts = new List<SpaceshipPart>();
    public Dictionary<string, Decoration> GetDecorationByUUID_onShip = new Dictionary<string, Decoration>();
    public NetworkPlayerObject owner;
    public override void Init(string uid, ulong Owner, string PrefabID)
    {
        base.Init(uid, Owner, PrefabID);
        owner = NetworkSystem.instance.PlayerList[Owner];
        owner.spaceship = this;
    }

}
