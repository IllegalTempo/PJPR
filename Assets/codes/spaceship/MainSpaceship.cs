using Assets.codes.Network.SyncedIdentity;
using Assets.codes.Network.Messages;
using Assets.codes.spaceship;
using Assets.codes.spaceship.modules;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;


public class MainSpaceship : MonoBehaviour
{
    public static MainSpaceship Instance { get; private set; }
    private Animator animator;
    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 acceleration;
    private Vector3 previousVelocity;
    private float speed;
    [SerializeField]
    private List<ModuleSlot> msts;

    public Dictionary<string, ModuleSlot> slots = new Dictionary<string, ModuleSlot>();

    [SerializeField]
    private OnSpaceshipCanvasDisplay spaceshipDisplay;
    [SerializeField, Min(0f)]
    private float forceDisplayMaxLocalOffset = 0f;
    [SerializeField, Min(1f)]
    private float rigidbodySyncRate = 20f;
    public Transform ModuleControlSpawnPoint;

    public static string MainSpaceshipNetworkID = "MAINSPACESHIP";

    private int waterLevel = 0;
    private float forcePositionWeightSum;
    private float forceWeightedLocalXSum;
    private float nextRigidbodySyncTime;
    private uint rigidbodySyncTick;
    private uint lastReceivedRigidbodySyncTick;
    public int WaterLevel
    {
        set
        {
            waterLevel = value;
            onUpdateWaterLevel();

        }
        get
        {
            return waterLevel;
        }
    }
    public void AddNonCentralForce(Vector3 force, Vector3 position)
    {
        rb.AddForceAtPosition(force, position);
        TrackForceDisplayWeight(force, position);
    }
    public void SetHandleSpeed(int step)
    {
        float velocityPerStep = 5f;
        //SetVelocity(-transform.right * step * velocityPerStep);
        speed = step * velocityPerStep;
    }
    private void onUpdateWaterLevel()
    {
        spaceshipDisplay.SetWaterAmount(waterLevel);
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }

    public Vector3 GetAcceleration()
    {
        return acceleration;
    }




    public void StopMovement()
    {
        velocity = Vector3.zero;
        acceleration = Vector3.zero;
    }

    public void RotateToward(Quaternion targetRotation)
    {
        float maxDegreesPerSecond = 30f;
        float maxDegreesDelta = maxDegreesPerSecond * Time.deltaTime;
        Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxDegreesDelta);

        if (rb != null)
        {
            rb.MoveRotation(newRotation);
            return;
        }

        transform.rotation = newRotation;
    }

    private void FixedUpdate()
    {

        UpdateForceDisplayWeight();
        ApplyConstantVelocityDrive();
        SendRigidbodyStateIfServer();
    }

    private void ApplyConstantVelocityDrive()
    {
        if (rb == null)
        {
            return;
        }

        Vector3 targetVelocity = -transform.right * speed;
        rb.AddForce(targetVelocity - rb.linearVelocity, ForceMode.VelocityChange);

        velocity = rb.linearVelocity;
        acceleration = Time.fixedDeltaTime > Mathf.Epsilon
            ? (velocity - previousVelocity) / Time.fixedDeltaTime
            : Vector3.zero;
        previousVelocity = velocity;
    }


    private void TrackForceDisplayWeight(Vector3 force, Vector3 position)
    {
        float forceMagnitude = force.magnitude;
        if (forceMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        float localX = transform.InverseTransformPoint(position).x;
        forceWeightedLocalXSum += localX * forceMagnitude;
        forcePositionWeightSum += forceMagnitude;
    }

    private void UpdateForceDisplayWeight()
    {
        if (spaceshipDisplay == null)
        {
            ResetForceDisplayWeight();
            return;
        }

        float weight = 0.5f;
        if (forcePositionWeightSum > Mathf.Epsilon)
        {
            float averageLocalX = forceWeightedLocalXSum / forcePositionWeightSum;
            float maxLocalOffset = GetForceDisplayMaxLocalOffset();

            if (maxLocalOffset > Mathf.Epsilon)
            {
                weight = Mathf.InverseLerp(-maxLocalOffset, maxLocalOffset, averageLocalX);
            }
            else if (averageLocalX < -Mathf.Epsilon)
            {
                weight = 0f;
            }
            else if (averageLocalX > Mathf.Epsilon)
            {
                weight = 1f;
            }
        }

        spaceshipDisplay.SetWeight(weight);
        ResetForceDisplayWeight();
    }

    private void ResetForceDisplayWeight()
    {
        forcePositionWeightSum = 0f;
        forceWeightedLocalXSum = 0f;
    }

    private void SendRigidbodyStateIfServer()
    {
        if (rb == null || NetworkSystem.Instance == null || NetworkRouter.Instance == null)
        {
            return;
        }

        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer)
        {
            return;
        }

        if (Time.time < nextRigidbodySyncTime)
        {
            return;
        }

        nextRigidbodySyncTime = Time.time + 1f / rigidbodySyncRate;
        rigidbodySyncTick++;

        NetworkRouter.Instance.DistributeMessageToReady(
            new NMS_Server_SyncMainSpaceshipRigidbody(rb.position, rb.rotation, rb.linearVelocity, rb.angularVelocity, rigidbodySyncTick),
            sendType: NetworkSendProfiles.State);
    }

    public void ApplyNetworkRigidbodyState(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, uint tick)
    {
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsServer)
        {
            return;
        }

        if (tick <= lastReceivedRigidbodySyncTick)
        {
            return;
        }

        lastReceivedRigidbodySyncTick = tick;
        this.velocity = velocity;
        acceleration = Vector3.zero;

        if (rb == null)
        {
            transform.SetPositionAndRotation(position, rotation);
            return;
        }

        rb.position = position;
        rb.rotation = rotation;
        rb.linearVelocity = velocity;
        rb.angularVelocity = angularVelocity;
    }

    private float GetForceDisplayMaxLocalOffset()
    {
        if (forceDisplayMaxLocalOffset > Mathf.Epsilon)
        {
            return forceDisplayMaxLocalOffset;
        }

        float maxLocalOffset = 0f;
        if (msts != null)
        {
            foreach (ModuleSlot slot in msts)
            {
                if (slot == null)
                {
                    continue;
                }

                maxLocalOffset = Mathf.Max(maxLocalOffset, Mathf.Abs(transform.InverseTransformPoint(slot.transform.position).x));
            }
        }

        return maxLocalOffset;
    }
    
    public async UniTask<Module> SpawnModuleAsync(string ModulePrefabName,Vector3 pos,Quaternion rot)
    {
        NetworkGameObject nobj = await NetworkSystem.Instance.CreateNetworkObject(ModulePrefabName, pos, rot, 0);
        Module module = nobj.GetComponent<Module>();
        
        return module;


    }
    public List<SlotSnapshot> GetSlotsSnapshot()
    {
        List<SlotSnapshot> slotSnapshots = new List<SlotSnapshot>();
        foreach (var slot in slots.Values)
        {
            string attachedItemId = slot.GetAttachedItem()?.GetNetworkObject()?.Identity?.Identifier ?? string.Empty;
            slotSnapshots.Add(new SlotSnapshot
            (
                slot.Identity.Identifier,
                attachedItemId,
                slot.GetAttachedItem()?.transform.rotation ?? Quaternion.identity
            ));
        }
        return slotSnapshots;
    }

    public void ConnectModule(Module module, ModuleSlot slot)
    {
        if (module == null || slot == null)
        {
            return;
        }

        slot.attachedModule = module;
        slots[slot.Identity.Identifier] = slot;
    }

    public int GetModuleSlotIndex(ModuleSlot slot)
    {
        return msts != null ? msts.IndexOf(slot) : -1;
    }

    public ModuleSlot GetModuleSlot(int index)
    {
        if (msts == null || index < 0 || index >= msts.Count)
        {
            return null;
        }

        return msts[index];
    }

    public void ResetScene()
    {
        //connectedSpaceship.Clear();
    }
    protected void Start()
    {
        Physics.gravity = Vector3.zero;
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        } else
        {
            Instance = this;
        }

        rb = GetComponent<Rigidbody>();
        foreach (ModuleSlot mst in msts)
        {
            slots[mst.Identity.Identifier] = mst;
        }
    }
}
