using Steamworks.Data;
using System;
using UnityEngine;

public class NetworkListener 
{
    public static Action<NetworkPlayer> Server_OnPlayerJoinSuccessful;
    public static Action<ConnectionInfo> Server_OnPlayerJoining;

}
