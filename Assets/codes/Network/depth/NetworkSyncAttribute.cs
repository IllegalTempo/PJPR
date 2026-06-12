using UnityEngine;
using System.Collections;
using System;

[AttributeUsage(AttributeTargets.Field)]
public class NetworkSyncAttribute : Attribute
{
    public NetworkSyncAuthority Authority { get; }
    public NetworkSyncMode Mode { get; }

    public NetworkSyncAttribute(
        NetworkSyncAuthority authority = NetworkSyncAuthority.Server,
        NetworkSyncMode mode = NetworkSyncMode.OnChange)
    {
        Authority = authority;
        Mode = mode;
    }
}
public enum NetworkSyncAuthority
{
    Server,
    Owner
}

public enum NetworkSyncMode
{
    OnChange,
    Manual,
    Interval
}