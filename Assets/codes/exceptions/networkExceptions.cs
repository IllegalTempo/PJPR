
public class NO_Not_Found: System.Exception
{
    public NO_Not_Found(string uid) : base($"Network Object Not Found, UID: {uid}")
    { 

    }
}
