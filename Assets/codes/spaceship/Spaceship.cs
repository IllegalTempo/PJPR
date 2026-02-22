using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(Rigidbody))]
public class Spaceship : NetworkObject
{
    public List<SpaceshipPart> Parts = new List<SpaceshipPart>();
    public Dictionary<string, Decoration> GetDecorationByUUID_onShip = new Dictionary<string, Decoration>();
    public NetworkPlayerObject OwnerPlayer;
    [SerializeField]
    private Animator animator;
    private Rigidbody rb;
    [SerializeField]
    private Vector3 dockTarget;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public override void Init(string uid, ulong Owner, string PrefabID)
    {
        base.Init(uid, Owner, PrefabID);
        OwnerPlayer = NetworkSystem.INSTANCE.PlayerList[Owner];
        OwnerPlayer.spaceship = this;
        string name = GameCore.INSTANCE.Connector.GetNewSpaceShipName() + "_connect";
        gameObject.name = name;
        dockTarget = GameCore.INSTANCE.Connector.connect(this);



    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if(dockTarget != Vector3.zero)
        {
            Vector3 direction = (dockTarget - transform.position);
            rb.linearVelocity = direction.normalized * 3;
            if (direction.magnitude < 0.03f)
            {
                Connect();
            }
        }
        
    }
    public void Connect()
    {
        rb.linearVelocity = Vector3.zero;
        Connector connector = GameCore.INSTANCE.Connector;
        transform.SetParent(connector.transform,true);
        Sync_Transform = false;
        dockTarget = Vector3.zero;

    }

}
