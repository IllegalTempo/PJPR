using System;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

//Network Object can be both
//1. Default in scene, please set good identifier
//2. Spawned by server , server will assign a unique identifier and send to all client, client will run Init() to set the identifier
public class NetworkObject : MonoBehaviour
{
    public string Identifier;
    public string PrefabID;
    public Vector3 NetworkPos;
    public Quaternion NetworkRot;

    [Header("Network Setting")]
    public bool Sync_Transform = true;

    public ulong Owner = 0; //owner = 0 is no owner
    public bool InScene = false;
    private bool init = false;
    private void Start()
    {
        if (!init)
        {
            InScene = true;
            Identifier = gameObject.name;
            NetworkSystem.INSTANCE.FindNetworkObject.Add(Identifier, this);

        }

    }
    public void UpdateActive(bool status)
    {
        gameObject.SetActive(status);
        if (NetworkSystem.INSTANCE.IsServer)
        {
            ServerSend.DistributeNOactive(Identifier, status);
        }
    }


    public virtual void Init(string uid, ulong owner, string PrefabID) //when a new object is created, server will send a packet to all client, this method is run by client
    {
        init = true;
        Identifier = uid;
        Owner = owner;
        this.PrefabID = PrefabID;
        if (!NetworkSystem.INSTANCE.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.INSTANCE.FindNetworkObject.Add(uid, this);
        }
        else
        {
            Debug.LogError($"NetworkObject with Identifier {uid} already exists in NetworkSystem. UID COLLISION??? END OF WORLD????");

        }


    }
    protected virtual void FixedUpdate()
    {
        Debug.Log($"{gameObject.name} fixed update");
        if (NetworkSystem.INSTANCE == null)
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
        if (NetworkSystem.INSTANCE.IsServer)
        {
            ServerSend.DistributeNOInfo(Identifier, transform.position, transform.rotation);
            Debug.Log($"[Server] Send Packet DistributeNOInfo " + Identifier);

        }
        else if (GameCore.INSTANCE.IsLocal(Owner))
        {
            ClientSend.SendNOInfo(Identifier, transform.position, transform.rotation);

        }

    }
    public void Network_ChangeOwner(ulong newowner)
    {
        Owner = newowner;
    }

    private void Update()
    {
        if (NetworkSystem.INSTANCE.IsServer) return;
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
