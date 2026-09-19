using NUnit.Framework;
using UnityEngine;

public class MissionPortalTests
{
    [Test]
    public void Portal_AcceptsOnlyCurrentShipHierarchy()
    {
        GameObject shipObject = new GameObject("Ship");
        GameObject hull = new GameObject("Hull");
        GameObject rock = new GameObject("Rock");

        try
        {
            MainSpaceship ship = shipObject.AddComponent<MainSpaceship>();
            hull.transform.SetParent(ship.transform);
            BoxCollider hullCollider = hull.AddComponent<BoxCollider>();
            SphereCollider rockCollider = rock.AddComponent<SphereCollider>();

            Assert.That(MissionPortal.IsSpaceshipCollider(hullCollider, ship), Is.True);
            Assert.That(MissionPortal.IsSpaceshipCollider(rockCollider, ship), Is.False);
            Assert.That(MissionPortal.IsSpaceshipCollider(null, ship), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(shipObject);
            Object.DestroyImmediate(rock);
        }
    }
}
