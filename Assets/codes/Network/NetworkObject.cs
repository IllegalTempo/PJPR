using UnityEngine;

public class NetworkObject : MonoBehaviour
{
    public string Identifier;
    public Vector3 NetworkPos;
    public Quaternion NetworkRot;

    [Header("Network Setting")]
    public bool Sync_Position = true;
    public bool Sync_Rotation = true;

    public int Owner = -1;

    private void Start()
    {
        //If not set, set to gameobject name
        if (string.IsNullOrEmpty(Identifier))
        {
            Identifier = gameObject.name;
        }
        if (!NetworkSystem.instance.FindNetworkObject.ContainsKey(Identifier))
        {
            NetworkSystem.instance.FindNetworkObject.Add(Identifier, this);
        }
        else
        {
            Debug.LogError($"NetworkObject with Identifier {Identifier} already exists in NetworkSystem. Please use unique Identifiers.");
        }
    }
    private void FixedUpdate()
    {
        if (NetworkSystem.instance.IsServer)
        {
            ServerSend.DistributeNOInfo(Identifier, transform.position, transform.rotation);
        }
        else
        {
            if (GameCore.instance.IsLocal(Owner))
            {
                ClientSend.SendNOInfo(Identifier, transform.position, transform.rotation);

            }
        }



    }
    public void ReceivedNetwork_PickUp(int newowner)
    {
        Owner = newowner;
    }
    private void Update()
    {
        if (NetworkSystem.instance.IsServer) return;
        if (Sync_Position)
        {
            transform.position = Vector3.Lerp(transform.position, NetworkPos, Time.deltaTime * 10f);
        }
        if (Sync_Rotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, NetworkRot, Time.deltaTime * 10f);
        }

    }
    public void SetMovement(Vector3 pos, Quaternion rot)
    {
        NetworkPos = pos;
        NetworkRot = rot;
    }
}
