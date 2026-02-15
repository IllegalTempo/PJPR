using System.Collections.Generic;
using UnityEngine;

public class Spaceship : MonoBehaviour
{
    public List<SpaceshipPart> parts = new List<SpaceshipPart>();
    public Dictionary<string, Decoration> GetDecorationByUUID_onShip = new Dictionary<string, Decoration>();

}
