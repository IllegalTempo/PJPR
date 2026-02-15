using System;
using System.Collections;
using UnityEngine;
[Serializable]
public class DecorationSaveData
{
    public string DecorationID;
    public SerializedVector3 DecorationPosition;
    public SerializedQuaternion DecorationRotation;
    public DecorationSaveData(string id, Vector3 pos, Quaternion rot)
    {
        DecorationID = id;
        DecorationPosition = pos;
        DecorationRotation = rot;

    }

}
