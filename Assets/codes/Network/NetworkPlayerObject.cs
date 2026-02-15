using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class NetworkPlayerObject : MonoBehaviour
{

    [Header("Network Data")]
    public ulong steamID;
    public bool IsLocal;

    public Vector3 NetworkPos;
    public Quaternion NetworkHeadRot;
    public Quaternion NetworkBodyrot;
    public float NetworkAnimationX;
    public float NetworkAnimationY;
    [Header("Network Setting")]
    public bool Sync_Pos = true;
    public bool Sync_HeadRot = true;
    public bool Sync_BodyRot = true;

    [Header("Object References")]
    public GameObject Head;
    public GameObject Body;
    public PlayerMain playerControl;
    public void Disconnect()
    {
        Destroy(gameObject);
    }
    private void Start()
    {
        if(IsLocal)
        {
            GameCore.instance.localNetworkPlayer = this;
            GameCore.instance.localPlayer = playerControl;
        }

    }
    private void FixedUpdate()
    {
        
        if (IsLocal)
        {
            if (!NetworkSystem.instance.IsServer)
            {
                ClientSend.Position(transform.position, Head.transform.rotation, transform.rotation);
            }
            else
            {
                ServerSend.DistributeMovement(0, transform.position, Head.transform.rotation, transform.rotation);
            }

        }
    }
    private void Update()
    {
        
        if (!IsLocal)
        {
            if (Sync_Pos)
            {
                transform.position = Vector3.Lerp(transform.position, NetworkPos, Time.deltaTime * 10f);
            }
            if (Sync_HeadRot)
            {
                Head.transform.rotation = Quaternion.Slerp(Head.transform.rotation, NetworkHeadRot, Time.deltaTime * 10f);
            }
            if (Sync_BodyRot)
            {
                Body.transform.localRotation = Quaternion.Slerp(Body.transform.rotation, NetworkBodyrot, Time.deltaTime * 10f);
            }
        }
    }
    public void SetMovement(Vector3 pos, Quaternion Headrot, Quaternion bodyrot)
    {
        NetworkPos = pos;
        NetworkHeadRot = Headrot;
        NetworkBodyrot = bodyrot;
    }
    public void SetAnimation(float x, float y)
    {
        NetworkAnimationX = x;
        NetworkAnimationY = y;
    }

}
