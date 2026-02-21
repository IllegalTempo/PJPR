
public class NO_Not_Found: System.Exception
{
    public NO_Not_Found(string uid) : base($"Network Object Not Found, UID: {uid}")
    { 

    }
}
public class InvalidLobbyCreated : System.Exception
{
    public InvalidLobbyCreated() : base($"Invalid Lobby Created")
    {

    }
}
public class ServerSocketInitializationFailed : System.Exception
{
    public ServerSocketInitializationFailed() : base($"Server Socket Failed to initialize")
    {

    }
}
