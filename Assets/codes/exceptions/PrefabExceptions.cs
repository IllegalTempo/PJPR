using System;
public class PrefabNotFound : Exception
{
    public PrefabNotFound(string prefabID) : base($"Prefab {prefabID} not found")
    {

    }
}