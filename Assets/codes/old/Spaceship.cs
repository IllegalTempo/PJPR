//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using static UnityEngine.GraphicsBuffer;
//using static UnityEngine.UI.GridLayoutGroup;

//[RequireComponent(typeof(Rigidbody))]
//public class Spaceship : NetworkObject
//{
//    public List<SpaceshipPart> Parts = new List<SpaceshipPart>();
//    public List<Decoration> Decorations = new List<Decoration>();
//    public NetworkPlayerObject OwnerPlayer;
//    [SerializeField]
//    private Animator animator;
//    private Rigidbody rb;
//    [SerializeField]
//    private Transform dockTarget;

//    [SerializeField]
//    private SkinnedMeshRenderer GlassRenderer;
//    private Coroutine blendShapeCoroutine;
//    [SerializeField]
//    private GameObject GlassColliderGroup;

//    private void Awake()
//    {
//        rb = GetComponent<Rigidbody>();

//    }
//    public override void Init(string uid, ulong Owner, string PrefabID)
//    {
//        base.Init(uid, Owner, PrefabID);


//        OwnerPlayer = NetworkSystem.Instance.PlayerList[Owner];
//        string name = $"Spaceship {OwnerPlayer.index}";
//        gameObject.name = name;
//        OwnerPlayer.spaceship = this;
//        if (NetworkSystem.Instance.IsOnline)
//        {
//            ConnectTo(OwnerPlayer.index);
//        }
//        if (NetworkSystem.Instance.IsServer && !OwnerPlayer.IsLocal)
//        {
//            rb.isKinematic = true;
//        }


//    }
//    public void ConnectTo(int index)
//    {
//        Debug.Log($"{gameObject.name} connecting to dock {index}");
//        dockTarget = GameCore.Instance.Connector.connect(this, index);

//    }
//    private void Update()
//    {
//        if (dockTarget != null)
//        {
//            transform.rotation = Quaternion.RotateTowards(transform.rotation, dockTarget.rotation, 20 * Time.deltaTime);
//            transform.position = Vector3.MoveTowards(transform.position, dockTarget.position, 3 * Time.deltaTime);
//            if (Vector3.Distance(dockTarget.position, transform.position) < 0.03f)
//            {
//                OnConnect();
//            }
//        }
//    }
//    public void OnConnect()
//    {
//        rb.linearVelocity = Vector3.zero;
//        Connector connector = GameCore.Instance.Connector;
//        transform.SetParent(connector.transform, true);
//        Sync_Transform = false;
//        dockTarget = null;
//        rb.isKinematic = true;

//    }
//    public void SwitchOnGlass()
//    {
//        ShiftGlassSwitch(0);
//        GlassColliderGroup.SetActive(true);
//    }
//    public void SwitchOffGlass()
//    {
//        ShiftGlassSwitch(100);
//        GlassColliderGroup.SetActive(false);


//    }
//    private void ShiftGlassSwitch(float targetValue = 100f)
//    {
//        if (GlassRenderer == null) return;

//        int blendShapeIndex = GlassRenderer.sharedMesh.GetBlendShapeIndex("switch");
//        if (blendShapeIndex == -1)
//        {
//            Debug.LogWarning("Blend shape 'switch' not found on GlassRenderer");
//            return;
//        }

//        if (blendShapeCoroutine != null)
//        {
//            StopCoroutine(blendShapeCoroutine);
//        }

//        blendShapeCoroutine = StartCoroutine(AnimateBlendShape(blendShapeIndex, targetValue));
//    }

//    private IEnumerator AnimateBlendShape(int blendShapeIndex, float targetWeight)
//    {
//        float currentWeight = GlassRenderer.GetBlendShapeWeight(blendShapeIndex);

//        while (Mathf.Abs(currentWeight - targetWeight) > 0.1f)
//        {
//            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, 50 * Time.deltaTime);
//            GlassRenderer.SetBlendShapeWeight(blendShapeIndex, currentWeight);
//            yield return null;
//        }

//        GlassRenderer.SetBlendShapeWeight(blendShapeIndex, targetWeight);
//        blendShapeCoroutine = null;
//    }

//}
