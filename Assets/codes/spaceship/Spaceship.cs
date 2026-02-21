using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(Rigidbody))]
public class Spaceship : NetworkObject
{
    public List<SpaceshipPart> parts = new List<SpaceshipPart>();
    public Dictionary<string, Decoration> GetDecorationByUUID_onShip = new Dictionary<string, Decoration>();
    public NetworkPlayerObject owner;
    [SerializeField]
    private Animator animator;
    private Rigidbody rb;
    private Vector3 dockTarget;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public override void Init(string uid, ulong Owner, string PrefabID)
    {
        base.Init(uid, Owner, PrefabID);
        owner = NetworkSystem.INSTANCE.PlayerList[Owner];
        owner.spaceship = this;
        string name = GameCore.INSTANCE.Connector.GetNewSpaceShipName() + "_connect";
        dockTarget = GameCore.INSTANCE.Connector.connect(this);



    }
    private void FixedUpdate()
    {
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
        Sync_Position = false;
        Sync_Rotation = false;
        dockTarget = Vector3.zero;

    }

}
