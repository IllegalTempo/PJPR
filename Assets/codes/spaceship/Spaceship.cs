using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(Rigidbody))]
public class Spaceship : NetworkObject
{
    public List<SpaceshipPart> Parts = new List<SpaceshipPart>();
    public List<Decoration> Decorations = new List<Decoration>();
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
        
        
        OwnerPlayer = NetworkSystem.Instance.PlayerList[Owner];
        string name = $"Spaceship {OwnerPlayer.index}";
        gameObject.name = name;
        OwnerPlayer.spaceship = this;
        if(NetworkSystem.Instance.IsOnline)
        {
            ConnectTo(OwnerPlayer.index);
        }
        if (NetworkSystem.Instance.IsServer && !OwnerPlayer.IsLocal)
        {
            rb.isKinematic = true;
        }


    }
    public void ConnectTo(int index)
    {
        Debug.Log($"{gameObject.name} connecting to dock {index}");
        dockTarget = GameCore.Instance.Connector.connect(this,index);

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
                OnConnect();
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
    public void OnConnect()
    {
        rb.linearVelocity = Vector3.zero;
        Connector connector = GameCore.Instance.Connector;
        transform.SetParent(connector.transform,true);
        Sync_Transform = false;
        dockTarget = null;
        rb.isKinematic = true;

    }

}
