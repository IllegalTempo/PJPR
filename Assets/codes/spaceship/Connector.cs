using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Connector : NetworkObject
{
    private List<SpaceshipPart> connectedParts = new List<SpaceshipPart>();
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Transform[] dockpos;
    public string GetNewSpaceShipName()
    {
        return "ss" + (connectedParts.Count +1);
    }
    public Vector3 connect(Spaceship s)
    {
        connectedParts.Add(s.GetComponent<SpaceshipPart>());
        return dockpos[connectedParts.Count - 1].position;
    }
}
