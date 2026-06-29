using Assets.codes.Network.SyncedIdentity;
using Assets.codes.spaceship.modules;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class MainSpaceship: MonoBehaviour
{
    public static MainSpaceship Instance { get; private set; }
    private Animator animator;
    private int speedlevel = 0;
    private Dictionary<int, module> slotModulePair = new Dictionary<int, module>();
    [SerializeField]
    private List<ModuleSlot> msts;

    public Dictionary<int, ModuleSlot> slot = new Dictionary<int, ModuleSlot>();

    private void Update()
    {
        foreach (module m in slotModulePair.Values)
        {
            m?.ModuleUpdate();

        }
    }
    public module GetConnectedModule(int slot)
    {
        return slotModulePair.TryGetValue(slot, out module connectedModule) ? connectedModule : null;
    }
    public async UniTask<module> SpawnModuleAsync(string ModulePrefabName,Vector3 pos,Quaternion rot)
    {
        NetworkGameObject nobj = await NetworkSystem.Instance.CreateNetworkObject(ModulePrefabName, pos, rot, 0);
        return nobj.GetComponent<module>(); 


    }
    public module ConnectModule(module moduleObject, ModuleSlot moduleslot)
    {
        Debug.Log($"Connecting module {moduleObject.name} to slot {moduleslot.slotIndex}");
        // Store the module's world rotation before reparenting
        
        slotModulePair[moduleslot.slotIndex] = moduleObject;
        return moduleObject;
    }
    public List<InstalledModuleSaveData> GetInstalledModuleSaveData()
    {
        List<InstalledModuleSaveData> modules = new List<InstalledModuleSaveData>();
        foreach (KeyValuePair<int, module> pair in slotModulePair)
        {
            string PrefabID = ((NetworkPrefabIdentity)pair.Value.GetNetworkObject().Identity).PrefabID;
            if (pair.Value == null || string.IsNullOrWhiteSpace(PrefabID))
            {
                continue;
            }

            modules.Add(new InstalledModuleSaveData(pair.Key,PrefabID,pair.Value.transform.localRotation));
        }

        return modules;
    }

    public async UniTask LoadInstalledModules(List<InstalledModuleSaveData> modules)
    {
        ClearInstalledModules();
        if (modules == null)
        {
            return;
        }

        foreach (InstalledModuleSaveData moduleData in modules)
        {
            if (moduleData == null || string.IsNullOrWhiteSpace(moduleData.PrefabID))
            {
                continue;
            }

            module md = await SpawnModuleAsync(moduleData.PrefabID, Vector3.zero, Quaternion.identity);
            slot[moduleData.Slot].Attach(md,Quaternion.identity);
            md.transform.localRotation = moduleData.Rotation;
        }
    }

    public void ClearInstalledModules()
    {
        foreach (module connectedModule in slotModulePair.Values)
        {
            if (connectedModule != null)
            {
                Destroy(connectedModule.gameObject);
            }
        }

        slotModulePair.Clear();
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
            slot[mst.slotIndex] = mst;
        }
    }
}
