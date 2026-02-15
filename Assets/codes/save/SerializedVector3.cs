using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class SerializedVector3
{
    public float x;
    public float y; public float z;
    public SerializedVector3() { }
    public SerializedVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public static implicit operator Vector3(SerializedVector3 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    public static implicit operator SerializedVector3(Vector3 v)
    {
        return new SerializedVector3(v);
    }
}
