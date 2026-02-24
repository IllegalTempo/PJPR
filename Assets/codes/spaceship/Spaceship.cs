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
    private Transform dockTarget;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public override void Init(string uid, ulong Owner, string PrefabID)
    {
        base.Init(uid, Owner, PrefabID);
        
        string name = GameCore.INSTANCE.Connector.GetNewSpaceShipName() + "_connect";
        gameObject.name = name;
        dockTarget = GameCore.INSTANCE.Connector.connect(this);



    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if(dockTarget != null)
        {
            Vector3 direction = (dockTarget.position - transform.position);
            rb.linearVelocity = direction.normalized * 3;
            if (direction.magnitude < 0.03f)
            {
                Connect();
            }
        }
        
    }
    private void Update()
    {
        if (dockTarget != null)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, dockTarget.rotation, 20 * Time.deltaTime);

        }
    }
    public void Connect()
    {
        rb.linearVelocity = Vector3.zero;
        Connector connector = GameCore.INSTANCE.Connector;
        transform.SetParent(connector.transform,true);
        Sync_Transform = false;
        dockTarget = null;

    }

}
