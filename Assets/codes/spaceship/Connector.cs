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
    private int speedlevel = 0;
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
    public void SetSpeedLevel(int level)
    {
        speedlevel = level;
        GameCore.Instance.WorldReference.SetMovement(speedlevel * transform.forward * 1);

    }
    protected override void Start()
    {
        base.Start();
        
    }
}
