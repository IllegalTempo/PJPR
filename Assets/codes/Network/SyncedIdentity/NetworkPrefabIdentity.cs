using Assets.codes.Network.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// NetworkObjects are gameObjects that requires a prefabID so it can be instantiated during runtime.
/// </summary>
public class NetworkPrefabIdentity : NetworkIdentity
{
    [HideInInspector]
    public string PrefabID;

    private bool childInit = false;
    public virtual void OnInstantiate(string uid, string PrefabID, ulong sovereignty) //when a new object is created, server will send a packet to all client, this method is run by client
    {
        Identifier = uid;
        this.PrefabID = PrefabID;
        this.Sovereignty = sovereignty;
        foreach (NetworkIdentity childid in GetComponentsInChildren<NetworkIdentity>())
        {
            childid.Identifier = Identifier + "_" + childid.name;
        }
        childInit = true;
    }
    public void OnEnable()
    {
        if(!childInit)
        {
            foreach (NetworkIdentity childid in GetComponentsInChildren<NetworkIdentity>())
            {
                childid.Identifier = Identifier + "_" + childid.name;
            }
            childInit = true;
        }
    }



}
