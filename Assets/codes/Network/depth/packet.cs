using Assets.codes.Network.Messages;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Packet : IDisposable
{
    public const int ProtocolVersion = 1;
    public const int HeaderSize = 16;

    public NetworkPlayer sentBy;
    public int PacketID { get; private set; }
    public int Version { get; private set; }
    public uint Sequence { get; private set; }
    public int PayloadLength { get; private set; }
    public int Length => readerbuffer?.Length ?? buffer?.Count ?? 0;
    public int ReadIndex => readindex;
    public int BytesRemaining => readerbuffer == null ? 0 : readerbuffer.Length - readindex;

    public Packet(int packetid)
    {
        buffer = new List<byte>();
        PacketID = packetid;
        readindex = 0;
    }

    public Packet(byte[] data, NetworkPlayer sentBySteamId)
    {
        readerbuffer = data;
        this.sentBy = sentBySteamId;
        ReadHeader();
    }

    public byte[] GetPacketData(uint sequence)
    {
        byte[] payload = GetPayloadData();
        List<byte> framedBuffer = new List<byte>(HeaderSize + payload.Length);
        framedBuffer.AddRange(BitConverter.GetBytes(ProtocolVersion));
        framedBuffer.AddRange(BitConverter.GetBytes(sequence));
        framedBuffer.AddRange(BitConverter.GetBytes(PacketID));
        framedBuffer.AddRange(BitConverter.GetBytes(payload.Length));
        framedBuffer.AddRange(payload);
        return framedBuffer.ToArray();
    }

    public byte[] GetPayloadData()
    {
        if (buffer == null)
        {
            throw new InvalidOperationException("Packet is not in write mode.");
        }

        return buffer.ToArray();
    }

    public byte[] GetPacketData()
    {
        return GetPacketData(0);
    }

    private List<byte> buffer;
    private byte[] readerbuffer;
    private int readindex;
    private bool disposed = false;

    private void ReadHeader()
    {
        readindex = 0;
        Version = Readint();
        if (Version != ProtocolVersion)
        {
            throw new InvalidOperationException($"Unsupported packet protocol {Version}. Expected {ProtocolVersion}.");
        }

        Sequence = Readuint();
        PacketID = Readint();
        PayloadLength = Readint();
        if (PayloadLength < 0)
        {
            throw new InvalidOperationException($"Invalid payload length {PayloadLength}.");
        }

        if (BytesRemaining != PayloadLength)
        {
            throw new InvalidOperationException($"Packet payload length mismatch. Header says {PayloadLength}, buffer has {BytesRemaining}.");
        }
    }

    private void EnsureCanRead(int byteCount)
    {
        if (readerbuffer == null)
        {
            throw new InvalidOperationException("Packet is not in read mode.");
        }

        if (byteCount < 0 || readindex + byteCount > readerbuffer.Length)
        {
            throw new InvalidOperationException($"Packet read overflow. Tried to read {byteCount} byte(s) at {readindex}, size {readerbuffer.Length}.");
        }
    }

    //Credit to https://stackoverflow.com/questions/151051/when-should-i-use-gc-suppressfinalize for dispose method
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                buffer = null;
                readerbuffer = null;
                readindex = 0;
            }           
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #region Write To Packet
    public void Write(int i)
    {
        buffer.AddRange(BitConverter.GetBytes(i));
    }
    public void Write(float i)
    {
        buffer.AddRange(BitConverter.GetBytes(i));
    }
    public void Write(short i)
    {
        buffer.AddRange(BitConverter.GetBytes(i));
    }
    public void Write(long i)
    {
        buffer.AddRange(BitConverter.GetBytes(i));
    }
    public void Write(uint i)
    {
        buffer.AddRange(BitConverter.GetBytes(i));
    }
    public void Write(Guid uuid)
    {
        buffer.AddRange(uuid.ToByteArray());
    }
    public void Write(bool i)
    {
        buffer.AddRange(BitConverter.GetBytes(i));
    }
    public void Write(ulong i)
    {
        buffer.AddRange(BitConverter.GetBytes(i));
    }
    public void Write(SteamId i)
    {
        buffer.AddRange(BitConverter.GetBytes(i.Value));
    }
    public void Write(Vector3 i)
    {
        Write(i.x);
        Write(i.y);
        Write(i.z);
    }
    public void Write(Quaternion i)
    {
        Write(i.x);
        Write(i.y);
        Write(i.z);
        Write(i.w);
    }

    public void Write(Mission m)
    {
        Write(m.missionName);
        Write(m.missionDescription);
        Write(m.rewardCredits);
        Write(m.difficulty);
        Write(m.estimatedDuration);
    }
    public void Write(NetworkObjectSnapshot snapshot)
    {
        Write(snapshot.Uid);
        Write(snapshot.Owner);
        Write(snapshot.PrefabId);
        Write(snapshot.Position);
        Write(snapshot.Rotation);
    }
    public void Write(SlotSnapshot snapshot)
    {
        Write(snapshot.SlotId);
        Write(snapshot.AttachedItemId);
        Write(snapshot.rotation);
    }
    public void Write(Array array)
    {
        if (array == null)
        {
            Write(-1);
            Debug.LogError("ATTEMPTING TO WRITE FUCKING EMPTY ARRAY WTFFWTWFW ");
            return;
        }

        Write(array.Length);
        foreach (var item in array)
        {
            WriteObject(item);
        }
    }

    private void WriteObject(object item)
    {
        switch (item)
        {
            case null:
                throw new InvalidOperationException("Cannot write null array elements.");
            case int value:
                Write(value);
                break;
            case float value:
                Write(value);
                break;
            case short value:
                Write(value);
                break;
            case long value:
                Write(value);
                break;
            case uint value:
                Write(value);
                break;
            case bool value:
                Write(value);
                break;
            case ulong value:
                Write(value);
                break;
            case Guid value:
                Write(value);
                break;
            case SteamId value:
                Write(value);
                break;
            case Vector3 value:
                Write(value);
                break;
            case Quaternion value:
                Write(value);
                break;
            case Mission value:
                Write(value);
                break;
            case NetworkObjectSnapshot value:
                Write(value);
                break;
            case SlotSnapshot value:
                Write(value);
                break;
            case string value:
                Write(value);
                break;
            case byte[] value:
                Write(value);
                break;
            case Array value:
                Write(value);
                break;
            default:
                throw new NotSupportedException($"Unsupported array element type: {item.GetType()}.");
        }
    }
    public void Write(string text)
    {
        text ??= string.Empty;
        Write(text.Length * 2);
        buffer.AddRange(Encoding.Unicode.GetBytes(text));
    }
    public void Write(byte[] bytes)
    {
        bytes ??= Array.Empty<byte>();
        Write(bytes.Length);
        buffer.AddRange(bytes);
    }
    #endregion
    #region Read Packet
    public int Readint()
    {
        EnsureCanRead(4);
        int data = BitConverter.ToInt32(readerbuffer, readindex);
        readindex += 4;
        return data;
    }
    public uint Readuint()
    {
        EnsureCanRead(4);
        uint data = BitConverter.ToUInt32(readerbuffer, readindex);
        readindex += 4;
        return data;
    }
    public float Readfloat()
    {
        EnsureCanRead(4);
        float data = BitConverter.ToSingle(readerbuffer, readindex);
        readindex += 4;
        return data;
    }
    public long Readlong()
    {
        EnsureCanRead(8);
        long data = BitConverter.ToInt64(readerbuffer, readindex);
        readindex += 8;
        return data;
    }
    public short Readshort()
    {
        EnsureCanRead(2);
        short data = BitConverter.ToInt16(readerbuffer, readindex);
        readindex += 2;
        return data;
    }
    public ulong Readulong()
    {
        EnsureCanRead(8);
        ulong data = BitConverter.ToUInt64(readerbuffer, readindex);
        readindex += 8;
        return data;
    }
    public bool Readbool()
    {
        EnsureCanRead(1);
        bool data = BitConverter.ToBoolean(readerbuffer, readindex);
        readindex += 1;
        return data;
    }
    public Guid ReadGuid()
    {
        EnsureCanRead(16);
        byte[] data = new byte[16];
        Buffer.BlockCopy(readerbuffer, readindex, data, 0, 16);
        readindex += 16;
        return new Guid(data);
    }
    public string ReadstringUNICODE()
    {
        int stringlength = Readint();
        if (stringlength < 0 || stringlength % 2 != 0)
        {
            throw new InvalidOperationException($"Invalid unicode string byte length {stringlength}.");
        }

        EnsureCanRead(stringlength);
        string data = Encoding.Unicode.GetString(readerbuffer, readindex, stringlength);
        readindex += stringlength;
        return data;
    }
    public Vector3 Readvector3()
    {
        return new Vector3(Readfloat(),Readfloat(),Readfloat());
    }
    public Quaternion Readquaternion()
    {
        return new Quaternion(Readfloat(),Readfloat(),Readfloat(),Readfloat());
    }
    public Mission ReadMission()
    {
        string name = ReadstringUNICODE();
        string description = ReadstringUNICODE();
        int reward = Readint();
        float difficulty = Readfloat();
        int duration = Readint();
        return new Mission(name, description, reward, difficulty, duration);
    }
    public byte[] ReadBytesArray()
    {
        int intlength = Readint();
        if (intlength < 0)
        {
            throw new InvalidOperationException($"Invalid byte array length {intlength}.");
        }

        EnsureCanRead(intlength);
        byte[] data = new byte[intlength];
        Buffer.BlockCopy(readerbuffer, readindex, data, 0, intlength);
        readindex += intlength;
        return data;
    }

    public T[] ReadArray<T>()
    {
        int length = Readint();
        if (length == -1)
        {
            return null;
        }

        if (length < -1)
        {
            throw new InvalidOperationException($"Invalid array length {length}.");
        }

        T[] data = new T[length];
        Type elementType = typeof(T);

        for (int i = 0; i < length; i++)
        {
            data[i] = (T)ReadArrayItem(elementType);
        }

        return data;
    }

    private object ReadArrayItem(Type elementType)
    {
        if (elementType == typeof(int)) return Readint();
        if (elementType == typeof(float)) return Readfloat();
        if (elementType == typeof(short)) return Readshort();
        if (elementType == typeof(long)) return Readlong();
        if (elementType == typeof(uint)) return Readuint();
        if (elementType == typeof(bool)) return Readbool();
        if (elementType == typeof(ulong)) return Readulong();
        if (elementType == typeof(Guid)) return ReadGuid();
        if (elementType == typeof(SteamId)) return new SteamId(Readulong());
        if (elementType == typeof(Vector3)) return Readvector3();
        if (elementType == typeof(Quaternion)) return Readquaternion();
        if (elementType == typeof(Mission)) return ReadMission();
        if (elementType == typeof(NetworkObjectSnapshot)) return ReadNetworkObjectSnapshot();
        if (elementType == typeof(SlotSnapshot)) return ReadSlotSnapshot();
        if (elementType == typeof(string)) return ReadstringUNICODE();
        if (elementType == typeof(byte[])) return ReadBytesArray();

        throw new NotSupportedException($"Unsupported array element type: {elementType}.");
    }
    #endregion
}
