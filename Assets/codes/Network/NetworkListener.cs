using Steamworks.Data;
using System;

public class NetworkListener 
{
    public event Action<NetworkPlayer> Server_OnPlayerJoinSuccessful;
    public event Action<ConnectionInfo> Server_OnPlayerJoining;
    public event Action<NetworkPlayer,int> Server_ReadyStateReceived;
    public void RaiseReadyState(NetworkPlayer player,int state)
    {
        Server_ReadyStateReceived?.Invoke(player,state);
    }
    public void RaisePlayerJoinSuccessful(NetworkPlayer player)
    {
        Server_OnPlayerJoinSuccessful?.Invoke(player);
    }
    public void RaisePlayerJoining(ConnectionInfo info)
    {
        Server_OnPlayerJoining?.Invoke(info);
    }
}
