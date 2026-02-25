using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Connector : NetworkObject
{
    private List<Spaceship> connectedSpaceship = new List<Spaceship>();
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Transform[] dockpos;
    public string GetNewSpaceShipName()
    {
        return "ss" + (connectedSpaceship.Count +1);
    }
    public void ResetScene()
    {
        connectedSpaceship.Clear();
    }
    public void disconnect(Spaceship s)
    {
        
        connectedSpaceship.Remove(s);
    }
    public Transform connect(Spaceship s,int slot)
    {
        connectedSpaceship.Add(s);
        return dockpos[slot];
    }
}
