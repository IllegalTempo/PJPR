using Assets.codes.Network.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// NetworkObjects are gameObjects that requires a prefabID so it can be instantiated during runtime.
/// </summary>
public class NetworkPrefabIdentity : NetworkIdentity
{
    [HideInInspector]
    public string PrefabID;

    public virtual void OnInstantiate(string uid, string PrefabID, ulong sovereignty) //when a new object is created, server will send a packet to all client, this method is run by client
    {
        Identifier = uid;
        this.PrefabID = PrefabID;
        this.Sovereignty = sovereignty;
    }
    public static List<NetworkPrefabIdentity> GetNetworkPrefabInScene()
    {
        return NetworkSystem.Instance.FindNetworkIdentity.Values.OfType<NetworkPrefabIdentity>().ToList();
    }
    
    public NetworkObjectSnapshot GetSnapshot()
    {
        return new NetworkObjectSnapshot(Identifier, Sovereignty, PrefabID, transform.position, transform.rotation);
    }

}
