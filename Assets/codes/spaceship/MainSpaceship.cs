using Assets.codes.Network.SyncedIdentity;
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
    private int speedlevel = 0;
    [SerializeField]
    private List<ModuleSlot> msts;

    public Dictionary<string, ModuleSlot> slots = new Dictionary<string, ModuleSlot>();

    [SerializeField]
    private OnSpaceshipCanvasDisplay spaceshipDisplay;
    public Transform ModuleControlSpawnPoint;



    private int waterLevel = 0;
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
    private void onUpdateWaterLevel()
    {
        spaceshipDisplay.SetWaterAmount(waterLevel);
    }
    
    public async UniTask<module> SpawnModuleAsync(string ModulePrefabName,Vector3 pos,Quaternion rot)
    {
        NetworkGameObject nobj = await NetworkSystem.Instance.CreateNetworkObject(ModulePrefabName, pos, rot, 0);
        module module = nobj.GetComponent<module>();
        
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

    public void ConnectModule(module module, ModuleSlot slot)
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
        if(Instance != null)
        {
            Destroy(Instance.gameObject);
        } else
        {
            Instance = this;
        }
        foreach (ModuleSlot mst in msts)
        {
            slots[mst.Identity.Identifier] = mst;
        }
    }
}
