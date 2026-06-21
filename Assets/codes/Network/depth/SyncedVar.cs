using System;
using System.Text;
using UnityEngine;

namespace Assets.codes.Network.depth
{
    public interface ISyncedVar
    {
        void Initialize(NetworkObject owner, int fieldId, NetworkSyncAuthority authority, NetworkSyncMode mode);
        void ApplyNetworkValue(byte[] valueBytes);
    }

    [System.Serializable]
    public class SyncedVar<T> : ISyncedVar
    {
        [SerializeField] private T value;

        private NetworkObject owner;
        private int fieldId;
        private NetworkSyncAuthority authority = NetworkSyncAuthority.Server;
        private NetworkSyncMode mode = NetworkSyncMode.OnChange;
        private bool initialized;

        public void SetValue(T newValue, bool sync = true)
        {
            if (Equals(value, newValue))
            {
                return;
            }
            value = newValue;
            if (initialized && sync && mode == NetworkSyncMode.OnChange)
            {
                Sync();
            }
        }
        public SyncedVar()
        {
        }

        public SyncedVar(T value)
        {
            this.value = value;
        }

        public SyncedVar(NetworkObject owner, int fieldId)
        {
            this.owner = owner;
            this.fieldId = fieldId;
            initialized = true;
        }

        public void Initialize(NetworkObject owner, int fieldId, NetworkSyncAuthority authority, NetworkSyncMode mode)
        {
            this.owner = owner;
            this.fieldId = fieldId;
            this.authority = authority;
            this.mode = mode;
            initialized = true;
        }

        public void Sync()
        {
            if (!initialized || owner == null)
            {
                return;
            }

            owner.SendSyncedVariable(fieldId, ToBytes(value), authority);
        }

        public void ApplyNetworkValue(byte[] valueBytes)
        {
            value = FromBytes(valueBytes);
        }

        private static byte[] ToBytes(T typedValue)
        {
            Type type = typeof(T);

            if (type == typeof(bool))
            {
                return BitConverter.GetBytes((bool)(object)typedValue);
            }

            if (type == typeof(int))
            {
                return BitConverter.GetBytes((int)(object)typedValue);
            }

            if (type == typeof(float))
            {
                return BitConverter.GetBytes((float)(object)typedValue);
            }

            if (type == typeof(long))
            {
                return BitConverter.GetBytes((long)(object)typedValue);
            }

            if (type == typeof(ulong))
            {
                return BitConverter.GetBytes((ulong)(object)typedValue);
            }

            if (type == typeof(string))
            {
                string text = (string)(object)typedValue ?? string.Empty;
                return Encoding.Unicode.GetBytes(text);
            }

            if (type == typeof(Vector3))
            {
                Vector3 vector = (Vector3)(object)typedValue;
                byte[] bytes = new byte[12];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 8, 4);
                return bytes;
            }

            if (type == typeof(Quaternion))
            {
                Quaternion rotation = (Quaternion)(object)typedValue;
                byte[] bytes = new byte[16];
                Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, bytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, bytes, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, bytes, 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, bytes, 12, 4);
                return bytes;
            }

            if (type.IsEnum)
            {
                return BitConverter.GetBytes(Convert.ToInt32(typedValue));
            }

            throw new NotSupportedException($"SyncedVar does not support type {type.FullName} yet.");
        }

        private static T FromBytes(byte[] valueBytes)
        {
            Type type = typeof(T);

            if (type == typeof(bool))
            {
                return (T)(object)BitConverter.ToBoolean(valueBytes, 0);
            }

            if (type == typeof(int))
            {
                return (T)(object)BitConverter.ToInt32(valueBytes, 0);
            }

            if (type == typeof(float))
            {
                return (T)(object)BitConverter.ToSingle(valueBytes, 0);
            }

            if (type == typeof(long))
            {
                return (T)(object)BitConverter.ToInt64(valueBytes, 0);
            }

            if (type == typeof(ulong))
            {
                return (T)(object)BitConverter.ToUInt64(valueBytes, 0);
            }

            if (type == typeof(string))
            {
                return (T)(object)Encoding.Unicode.GetString(valueBytes);
            }

            if (type == typeof(Vector3))
            {
                return (T)(object)new Vector3(
                    BitConverter.ToSingle(valueBytes, 0),
                    BitConverter.ToSingle(valueBytes, 4),
                    BitConverter.ToSingle(valueBytes, 8));
            }

            if (type == typeof(Quaternion))
            {
                return (T)(object)new Quaternion(
                    BitConverter.ToSingle(valueBytes, 0),
                    BitConverter.ToSingle(valueBytes, 4),
                    BitConverter.ToSingle(valueBytes, 8),
                    BitConverter.ToSingle(valueBytes, 12));
            }

            if (type.IsEnum)
            {
                return (T)Enum.ToObject(type, BitConverter.ToInt32(valueBytes, 0));
            }

            throw new NotSupportedException($"SyncedVar does not support type {type.FullName} yet.");
        }
    }
}
