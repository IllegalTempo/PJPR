using Assets.codes.Network.Messages;
using Assets.codes.Network.depth;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

//Network Object can be both
//1. Default in scene, please set good identifier
//2. Spawned by server , server will assign a unique identifier and send to all client, client will run Init() to set the identifier
public class NetworkObject : MonoBehaviour
{
    public string Identifier;
    public string PrefabID;
    public Vector3 NetworkPos;
    public Quaternion NetworkRot;

    [Header("Network Setting")]
    public bool Sync_Transform = true;

    private ulong owner = 0; //owner = 0 -> Server Authority
    public ulong Owner { 
        get => owner;
    }
    public bool Preset = false;
    private bool init = false;
    private readonly Dictionary<int, Action<byte[]>> syncedVariableHandlers = new Dictionary<int, Action<byte[]>>();
    private readonly Dictionary<int, NetworkSyncAuthority> syncedVariableAuthorities = new Dictionary<int, NetworkSyncAuthority>();
    private static readonly Dictionary<Type, FieldInfo[]> syncedVariableFieldsByComponentType = new Dictionary<Type, FieldInfo[]>();
    private bool syncedVariablesInitialized;

    protected virtual void Awake()
    {
        InitializeSyncedVariables();
    }

    public void RegisterSyncedVariable(int variableId, Action<byte[]> applyValue, NetworkSyncAuthority authority = NetworkSyncAuthority.Server)
    {
        if (applyValue == null)
        {
            Debug.LogWarning($"Cannot register null synced variable handler {variableId} on {name}.");
            return;
        }

        if (syncedVariableHandlers.ContainsKey(variableId))
        {
            Debug.LogWarning($"Duplicate synced variable id {variableId} on {name}. The latest handler will replace the previous one.");
        }

        syncedVariableHandlers[variableId] = applyValue;
        syncedVariableAuthorities[variableId] = authority;
    }

    public bool ApplySyncedVariable(int variableId, byte[] valueBytes)
    {
        if (!syncedVariableHandlers.TryGetValue(variableId, out Action<byte[]> applyValue))
        {
            Debug.LogWarning($"{name} has no synced variable handler registered for id {variableId}.");
            return false;
        }

        try
        {
            applyValue(valueBytes);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to apply synced variable {Identifier}.{variableId}: {exception.Message}");
            return false;
        }
    }

    public bool CanClientSyncVariable(int variableId, NetworkPlayer player)
    {
        if (!syncedVariableAuthorities.TryGetValue(variableId, out NetworkSyncAuthority authority))
        {
            Debug.LogWarning($"{name} has no synced variable authority registered for id {variableId}.");
            return false;
        }

        if (authority == NetworkSyncAuthority.Server)
        {
            Debug.LogWarning($"Rejected server-authority variable sync for {Identifier}.{variableId} from {player.steamId}.");
            return false;
        }

        if (authority == NetworkSyncAuthority.Owner && owner != player.steamId)
        {
            Debug.LogWarning($"Rejected owner-authority variable sync for {Identifier}.{variableId}: sender {player.steamId} does not own it.");
            return false;
        }

        return true;
    }

    public void SendSyncedVariable(int variableId, byte[] valueBytes, NetworkSyncAuthority authority)
    {
        if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline || NetworkRouter.Instance == null || string.IsNullOrWhiteSpace(Identifier))
        {
            return;
        }

        if (!CanSendSyncedVariable(authority))
        {
            return;
        }

        NMS_Both_SyncVariable message = new NMS_Both_SyncVariable(Identifier, variableId, valueBytes);
        if (NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.DistributeMessageToReady(message);
        }
        else
        {
            NetworkRouter.Instance.SendMessageToServer(message);
        }
    }

    private bool CanSendSyncedVariable(NetworkSyncAuthority authority)
    {
        if (NetworkSystem.Instance.IsServer)
        {
            return true;
        }

        if (authority == NetworkSyncAuthority.Owner && GameCore.Instance != null && GameCore.Instance.IsLocal(owner))
        {
            return true;
        }

        return false;
    }

    private void InitializeSyncedVariables()
    {
        if (syncedVariablesInitialized)
        {
            return;
        }

        syncedVariablesInitialized = true;
        Component[] components = GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
            {
                continue;
            }

            FieldInfo[] fields = GetSyncedVariableFields(component.GetType());
            foreach (FieldInfo field in fields)
            {
                ISyncedVar syncedVar = GetOrCreateSyncedVar(component, field);
                if (syncedVar == null)
                {
                    continue;
                }

                NetworkSyncAttribute syncAttribute = field.GetCustomAttribute<NetworkSyncAttribute>();
                NetworkSyncAuthority authority = syncAttribute?.Authority ?? NetworkSyncAuthority.Server;
                NetworkSyncMode mode = syncAttribute?.Mode ?? NetworkSyncMode.OnChange;
                int variableId = MakeVariableId(field);

                syncedVar.Initialize(this, variableId, authority, mode);
                RegisterSyncedVariable(variableId, syncedVar.ApplyNetworkValue, authority);
            }
        }
    }

    private static FieldInfo[] GetSyncedVariableFields(Type componentType)
    {
        if (syncedVariableFieldsByComponentType.TryGetValue(componentType, out FieldInfo[] cachedFields))
        {
            return cachedFields;
        }

        List<FieldInfo> fields = new List<FieldInfo>();
        Type currentType = componentType;
        while (currentType != null && currentType != typeof(MonoBehaviour))
        {
            FieldInfo[] declaredFields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in declaredFields)
            {
                if (typeof(ISyncedVar).IsAssignableFrom(field.FieldType))
                {
                    fields.Add(field);
                }
            }

            currentType = currentType.BaseType;
        }

        FieldInfo[] result = fields.ToArray();
        syncedVariableFieldsByComponentType[componentType] = result;
        return result;
    }

    private static ISyncedVar GetOrCreateSyncedVar(Component component, FieldInfo field)
    {
        object value = field.GetValue(component);
        if (value == null)
        {
            value = Activator.CreateInstance(field.FieldType);
            field.SetValue(component, value);
        }

        return value as ISyncedVar;
    }

    private static int MakeVariableId(FieldInfo field)
    {
        string key = $"{field.DeclaringType.FullName}.{field.Name}";
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < key.Length; i++)
            {
                hash = (hash * 31) + key[i];
            }

            return hash;
        }
    }

    protected virtual void Start()
    {
        if (!init)
        {
            Preset = true;
            if (string.IsNullOrWhiteSpace(Identifier))
            {
                Identifier = GameCore.Instance.newNOUID();
                Debug.LogWarning($"{name} has no scene NetworkObject Identifier. Generated {Identifier}, but scene objects should use a stable Identifier for multiplayer.");
            }

            if (NetworkSystem.Instance.FindNetworkObject.ContainsKey(Identifier))
            {
                Debug.LogError($"NetworkObject Identifier collision: {Identifier} is already registered by {NetworkSystem.Instance.FindNetworkObject[Identifier].name}. {name} will not be registered.");
                return;
            }

            NetworkSystem.Instance.FindNetworkObject.Add(Identifier, this);

        }

    }
    public void UpdateActive(bool status)
    {
        gameObject.SetActive(status);
        if (NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.DistributeMessageToReady(new NMS_Both_NetworkObjectActive(Identifier, status));
        }
    }


    public virtual void Init(string uid, ulong owner, string PrefabID) //when a new object is created, server will send a packet to all client, this method is run by client
    {
        init = true;
        Identifier = uid;
        this.owner = owner;
        this.PrefabID = PrefabID;
        if (!NetworkSystem.Instance.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.Instance.FindNetworkObject.Add(uid, this);
        }
        else
        {
            Debug.LogError($"NetworkObject with Identifier {uid} already exists in NetworkSystem. UID COLLISION??? END OF WORLD????");

        }


    }
    protected virtual void FixedUpdate()
    {
        if (NetworkSystem.Instance == null)
        {
            return;
        }
        if (Sync_Transform)
        {
            SendTransform();
        }


    }
    private void SendTransform()
    {
        if (!NetworkSystem.Instance.IsOnline) return;
        NMS_Both_NetworkObjectInfo message = new NMS_Both_NetworkObjectInfo(Identifier, transform.position, transform.rotation);
        if (NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.DistributeMessageToReady(message);

        }
        else if (GameCore.Instance.IsLocal(owner))
        {
            NetworkRouter.Instance.SendMessageToServer(message);

        }

    }
    public void ChangeOwner(ulong newowner)
    {
        owner = newowner;
    }

    private void Update()
    {
        if (NetworkSystem.Instance.IsServer) return;
        if (GameCore.Instance != null && GameCore.Instance.IsLocal(owner)) return;
        if (Sync_Transform)
        {
            transform.position = Vector3.Lerp(transform.position, NetworkPos, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, NetworkRot, Time.deltaTime * 10f);
        }

    }
    public void SetMovement(Vector3 pos, Quaternion rot)
    {
        NetworkPos = pos;
        NetworkRot = rot;
    }
    public void SetServerMovement(Vector3 pos, Quaternion rot)
    {
        SetMovement(pos, rot);
        transform.position = pos;
        transform.rotation = rot;
    }
}
