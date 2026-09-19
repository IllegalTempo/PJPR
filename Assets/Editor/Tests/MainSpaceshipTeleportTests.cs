using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MainSpaceshipTeleportTests
{
    private GameObject shipObject;

    [TearDown]
    public void TearDown()
    {
        if (shipObject != null)
        {
            Object.DestroyImmediate(shipObject);
        }
    }

    [Test]
    public void Teleport_ClearsVelocityAngularVelocityAndAcceleration()
    {
        MainSpaceship ship = CreateShip();
        Rigidbody body = ship.GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.one * 20f;
        body.angularVelocity = Vector3.one * 5f;
        ship.SetHandleSpeed(3);

        Vector3 destination = new Vector3(50f, 1f, -20f);
        Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
        ship.Teleport(destination, rotation);

        Assert.That(ship.transform.position, Is.EqualTo(destination));
        Assert.That(ship.transform.rotation, Is.EqualTo(rotation));
        Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
        Assert.That(body.angularVelocity, Is.EqualTo(Vector3.zero));
        Assert.That(ship.GetAcceleration(), Is.EqualTo(Vector3.zero));
    }

    private MainSpaceship CreateShip()
    {
        shipObject = new GameObject("MainSpaceshipTeleportTests Ship");
        Rigidbody body = shipObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        MainSpaceship ship = shipObject.AddComponent<MainSpaceship>();

        FieldInfo rigidbodyField = typeof(MainSpaceship).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(rigidbodyField, Is.Not.Null);
        rigidbodyField.SetValue(ship, body);
        return ship;
    }
}
