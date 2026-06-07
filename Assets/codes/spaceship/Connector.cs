using Assets.codes.spaceship.modules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Connector : NetworkObject
{
    [SerializeField]
    private Animator animator;
    private int speedlevel = 0;
    private Dictionary<int, module> slotModulePair = new Dictionary<int, module>();
    [SerializeField]
    private List<GameObject> slot;

    [Header("Default Spaceship Parts")]
    [SerializeField]
    private module[] DEFAULT_MODULE;
    private void Update()
    {
        foreach (module m in slotModulePair.Values)
        {
            m?.ModuleUpdate();

        }
        foreach (module m in DEFAULT_MODULE)
        {
            m?.ModuleUpdate();
        }
    }
    public module GetConnectedModule(int slot)
    {
        return slotModulePair.TryGetValue(slot, out module connectedModule) ? connectedModule : null;
    }
    public void ConnectModule(int slot, string ModulePrefabName)
    {
        if (!GameCore.Instance.TryGetNetworkPrefab(ModulePrefabName, out GameObject prefab))
        {
            Debug.LogError($"Module prefab {ModulePrefabName} not found.");
            return;
        }

        if (slot < 0 || slot >= this.slot.Count || this.slot[slot] == null)
        {
            Debug.LogError($"Connector slot {slot} is invalid.");
            return;
        }

        if (slotModulePair.TryGetValue(slot, out module existingModule) && existingModule != null)
        {
            Destroy(existingModule.gameObject);
            slotModulePair.Remove(slot);
        }

        GameObject moduleObject = Instantiate(prefab, this.slot[slot].transform);

        moduleObject.transform.localPosition = Vector3.zero;
        moduleObject.transform.localRotation = Quaternion.identity;

        module connectedModule = moduleObject.GetComponent<module>();
        if (connectedModule == null)
        {
            Debug.LogError($"Module prefab {ModulePrefabName} does not have a module component.");
            Destroy(moduleObject);
            return;
        }

        connectedModule.Init(ModulePrefabName, this);
        slotModulePair[slot] = connectedModule;

    }
    public List<InstalledModuleSaveData> GetInstalledModuleSaveData()
    {
        List<InstalledModuleSaveData> modules = new List<InstalledModuleSaveData>();
        foreach (KeyValuePair<int, module> pair in slotModulePair)
        {
            if (pair.Value == null || string.IsNullOrWhiteSpace(pair.Value.PrefabID))
            {
                continue;
            }

            modules.Add(new InstalledModuleSaveData(pair.Key, pair.Value.PrefabID));
        }

        return modules;
    }

    public void LoadInstalledModules(List<InstalledModuleSaveData> modules)
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

            ConnectModule(moduleData.Slot, moduleData.PrefabID);
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
    protected override void Start()
    {
        base.Start();

    }
}
