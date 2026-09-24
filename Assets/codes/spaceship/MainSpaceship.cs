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
    private Vector3 acceleration;
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
    private float nextRigidbodySyncTime;
    private uint rigidbodySyncTick;
    private uint lastReceivedRigidbodySyncTick;
    public Slot[] controllerSlot;
    private readonly Dictionary<ModuleController, Slot> controllerSlotAssignments = new Dictionary<ModuleController, Slot>();
    private readonly HashSet<Slot> reservedControllerSlots = new HashSet<Slot>();
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
        float accPerStep = 5f;
        //SetVelocity(-transform.right * step * velocityPerStep);
        acceleration = -transform.right * step * accPerStep;
    }
    private void onUpdateWaterLevel()
    {
    }

    public Vector3 GetAcceleration()
    {
        return acceleration;
    }




    public void StopMovement()
    {
        acceleration = Vector3.zero;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        StopMovement();

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            transform.SetPositionAndRotation(position, rotation);
            return;
        }

        transform.SetPositionAndRotation(position, rotation);
        rb.position = position;
        rb.rotation = rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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

        SendRigidbodyStateIfServer();
        rb.AddForce(acceleration, ForceMode.Acceleration);
    }


    private void TrackForceDisplayWeight(Vector3 force, Vector3 position)
    {
        float forceMagnitude = force.magnitude;
        if (forceMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        float localX = transform.InverseTransformPoint(position).x;
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

    public Slot GetAvailableControllerSlot(ModuleController controller)
    {
        if (controllerSlot == null)
        {
            return null;
        }

        foreach (Slot candidate in controllerSlot)
        {
            if (candidate == null)
            {
                continue;
            }

            if (!reservedControllerSlots.Contains(candidate))
            {
                reservedControllerSlots.Add(candidate);
                if (controller != null)
                {
                    controllerSlotAssignments[controller] = candidate;
                }
                return candidate;
            }
        }

        return null;
    }

    public void AssignControllerSlot(ModuleController controller, Slot slot)
    {
        if (controller != null && slot != null)
        {
            controllerSlotAssignments[controller] = slot;
        }
    }

    public void ReleaseControllerSlot(ModuleController controller)
    {
        if (controller != null)
        {
            if (controllerSlotAssignments.TryGetValue(controller, out Slot slot))
            {
                reservedControllerSlots.Remove(slot);
                controllerSlotAssignments.Remove(controller);
            }
        }
    }

    public void ResetScene()
    {
        //connectedSpaceship.Clear();
    }
    protected void Start()
    {
        Physics.gravity = Vector3.zero;
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
        rb = GetComponent<Rigidbody>();
        foreach (ModuleSlot mst in msts)
        {
            slots[mst.Identity.Identifier] = mst;
        }
    }
}
