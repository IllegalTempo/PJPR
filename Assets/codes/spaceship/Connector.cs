using System.Collections.Generic;
using UnityEngine;

public class Connector : MonoBehaviour
{
    public Spaceship[] ConnectedShips = new Spaceship[5];
    private void Start()
    {
        GameCore.instance.connector = this;
    }
}
