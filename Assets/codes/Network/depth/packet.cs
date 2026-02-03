using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class packet : IDisposable
{
    public packet(int packetid)
    {
        buffer = new List<byte>();
        Write(packetid);
        readindex = 0;
    }
    public packet(byte[] data)
    {
        readerbuffer = data;
    }
    public byte[] GetPacketData()
    {
        return buffer.ToArray();
    }
    private List<byte> buffer;
    private byte[] readerbuffer;
    private int readindex;
    private bool disposed = false;
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
    public void WriteASCII(string text)
    {
        Write(text.Length);
        buffer.AddRange(Encoding.ASCII.GetBytes(text));
    }
    public void WriteUNICODE(string text)
    {
        Write(text.Length * 2);
        buffer.AddRange(Encoding.Unicode.GetBytes(text));
    }
    #endregion
    #region Read Packet
    public int Readint()
    {
        int data = BitConverter.ToInt32(readerbuffer, readindex);
        readindex += 4;
        return data;
    }
    public float Readfloat()
    {
        float data = BitConverter.ToSingle(readerbuffer, readindex);
        readindex += 4;
        return data;
    }
    public long Readlong()
    {
        long data = BitConverter.ToInt64(readerbuffer, readindex);
        readindex += 8;
        return data;
    }
    public ulong Readulong()
    {
        ulong data = BitConverter.ToUInt64(readerbuffer, readindex);
        readindex += 8;
        return data;
    }
    public bool Readbool()
    {
        bool data = BitConverter.ToBoolean(readerbuffer, readindex);
        readindex += 1;
        return data;
    }
    public string ReadstringASCII()
    {
        int stringlength = Readint();
        string data = Encoding.ASCII.GetString(readerbuffer, readindex, stringlength);
        readindex += data.Length;
        return data;
    }
    public string ReadstringUNICODE()
    {
        int stringlength = Readint();
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
    #endregion
}
