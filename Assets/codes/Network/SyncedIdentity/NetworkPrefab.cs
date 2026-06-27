using Assets.codes.Network.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// NetworkObjects are gameObjects that requires a prefabID so it can be instantiated during runtime.
/// </summary>
public class NetworkPrefab : NetworkIdentity
{
    public string PrefabID;
    public Vector3 NetworkPos;
    public Quaternion NetworkRot;

    public ulong Sovereignty = 0; //owner = 0 -> Server Authority

    [Header("NetworkObject Setting")]
    [SerializeField]
    public bool Sync_Transform = true;

    

    public virtual void Instantiate_Init(string uid, string PrefabID, ulong sovereignty) //when a new object is created, server will send a packet to all client, this method is run by client
    {
        Identifier = uid;
        this.PrefabID = PrefabID;
        this.Sovereignty = sovereignty;
    }
    public void UpdateActive(bool status)
    {
        gameObject.SetActive(status);
        if (NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.DistributeMessageToReady(new NMS_Both_NetworkObjectActive(Identifier, status));
        }
    }


    
    protected virtual void FixedUpdate()
    {
        if (NetworkSystem.Instance == null)
        {
            return;
        }
        if (Sync_Transform)
        {
            SendTransform();
        }


    }
    private void SendTransform()
    {
        if (!NetworkSystem.Instance.IsOnline) return;
        NMS_Both_NetworkObjectInfo message = new NMS_Both_NetworkObjectInfo(Identifier, transform.position, transform.rotation);
        if (NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.DistributeMessageToReady(message);

        }
        else if (GameCore.Instance.IsLocal(Sovereignty))
        {
            NetworkRouter.Instance.SendMessageToServer(message);

        }

    }
    public void ChangeOwner(ulong newowner)
    {
        Sovereignty = newowner;
    }

    private void Update()
    {
        if (NetworkSystem.Instance.IsServer) return;
        if (GameCore.Instance != null && GameCore.Instance.IsLocal(Sovereignty)) return;
        if (Sync_Transform)
        {
            transform.position = Vector3.Lerp(transform.position, NetworkPos, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, NetworkRot, Time.deltaTime * 10f);
        }

    }
    public void SetMovement(Vector3 pos, Quaternion rot)
    {
        NetworkPos = pos;
        NetworkRot = rot;
    }
    public void SetServerMovement(Vector3 pos, Quaternion rot)
    {
        SetMovement(pos, rot);
        transform.position = pos;
        transform.rotation = rot;
    }
}
