using Steamworks.Data;
using System;
using UnityEngine;

public class NetworkListener 
{
    public static event Action<NetworkPlayer> Server_OnPlayerJoinSuccessful;
    public static event Action<ConnectionInfo> Server_OnPlayerJoining;
    public static event Action<NetworkPlayer,int> Server_ReadyStateReceived;
    public static void RaiseReadyState(NetworkPlayer player,int state)
    {
        Server_ReadyStateReceived?.Invoke(player,state);
    }
    public static void RaisePlayerJoinSuccessful(NetworkPlayer player)
    {
        Server_OnPlayerJoinSuccessful?.Invoke(player);
    }
    public static void RaisePlayerJoining(ConnectionInfo info)
    {
        Server_OnPlayerJoining?.Invoke(info);
    }
}
