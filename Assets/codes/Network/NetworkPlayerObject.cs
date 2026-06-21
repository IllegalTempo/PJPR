using Assets.codes.Network.Messages;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static UnityEngine.GraphicsBuffer;

public class NetworkPlayerObject : MonoBehaviour
{

    [Header("Network Data")]
    public ulong steamID;
    public bool IsLocal;
    public int index;

    public Vector3 NetworkPos;
    public Quaternion NetworkHeadRot;
    public Quaternion NetworkBodyrot;
    public float NetworkAnimationX;
    public float NetworkAnimationY;
    private Friend steamIdentity;
    [Header("Network Setting")]
    public bool Sync_Pos = true;
    public bool Sync_HeadRot = true;
    public bool Sync_BodyRot = true;

    [Header("Object References")]
    public GameObject Head;
    public GameObject Body;
    public PlayerMain playerControl;
    //public Spaceship spaceship;
    public async UniTask Init(ulong steamid,int index)
    {
        steamID = steamid;
        IsLocal = steamid == NetworkSystem.Instance.SteamID;
        this.index = index;
        steamIdentity = new Friend(steamid);
        gameObject.name = $"Player {index} ({steamid})";
        await UIManager.Instance.NewPlayerDisplay(steamid,steamIdentity.Name);
    }

    public void Disconnect()
    {
        Destroy(gameObject);
        //Connector.Instance.disconnect(spaceship);
        //Destroy(spaceship);

    }
    private void Start()
    {
        if(IsLocal)
        {
            GameCore.Instance.Local_NetworkPlayer = this;
            GameCore.Instance.Local_Player = playerControl;
        }

    }
    private void FixedUpdate()
    {
        
        SendTransform();
    }
    private void SendTransform()
    {
        if (!NetworkSystem.Instance.IsOnline) return;
        if (IsLocal)
        {
            NMS_Both_PositionUpdate msg = new NMS_Both_PositionUpdate(NetworkSystem.Instance.SteamID, transform.position, Head.transform.rotation, transform.rotation);
            if (NetworkSystem.Instance.IsServer)
            {
                NetworkRouter.Instance.DistributeMessageToReady(msg);
            }
            else
            {
                NetworkRouter.Instance.SendMessageToServer(msg);
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
