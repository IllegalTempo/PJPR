using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class SerializedQuaternion
{
    public float x;
    public float y; 
    public float z;
    public float w;
    public SerializedQuaternion() { }
    public SerializedQuaternion(Quaternion v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
        w = v.w;


    }
    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }

    public static implicit operator Quaternion(SerializedQuaternion v)
    {
        return new Quaternion(v.x, v.y, v.z,v.w);
    }

    public static implicit operator SerializedQuaternion(Quaternion v)
    {
        return new SerializedQuaternion(v);
    }
}
