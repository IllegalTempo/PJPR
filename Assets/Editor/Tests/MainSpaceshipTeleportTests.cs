using System.Reflection;
using Assets.codes.machines;
using NUnit.Framework;
using UnityEngine;

public class MainSpaceshipTeleportTests
{
    private GameObject shipObject;
    private GameObject handleObject;

    [TearDown]
    public void TearDown()
    {
        if (shipObject != null)
        {
            Object.DestroyImmediate(shipObject);
        }

        if (handleObject != null)
        {
            Object.DestroyImmediate(handleObject);
        }
    }

    [Test]
    public void SpeedHandle_ZeroStepIsVisualCenter()
    {
        HandleControl handle = CreateSpeedHandle();

        handle.VisualOnStep(0);

        Assert.That(handle.HandleTransform.localEulerAngles.x, Is.EqualTo(45f).Within(0.001f));
    }

    [TestCase(0, 1, 1)]
    [TestCase(0, -1, -1)]
    [TestCase(3, 1, 3)]
    [TestCase(-3, -1, -3)]
    public void SpeedHandle_NextStepMovesInDirectionAndClamps(int currentStep, int direction, int expectedStep)
    {
        HandleControl handle = CreateSpeedHandle();
        handle.OnStepChanged(currentStep);

        int nextStep = handle.GetNextStep(direction);

        Assert.That(nextStep, Is.EqualTo(expectedStep));
    }

    [Test]
    public void SyncedMachine_SecondaryPressRunsOnlyServerHandler()
    {
        handleObject = new GameObject("MainSpaceshipTeleportTests SyncedMachine");
        SecondaryInteractionMachine machine = handleObject.AddComponent<SecondaryInteractionMachine>();

        machine.OnNetworkApplyAction((int)SyncedMachine.InteractionType.SecondaryPress, null);
        Assert.That(machine.ServerSecondaryPressCount, Is.Zero);

        machine.OnNetworkApplyActionServer((int)SyncedMachine.InteractionType.SecondaryPress, null);
        Assert.That(machine.ServerSecondaryPressCount, Is.EqualTo(1));
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
        Assert.That(Quaternion.Angle(ship.transform.rotation, rotation), Is.LessThan(0.001f));
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

    private HandleControl CreateSpeedHandle()
    {
        handleObject = new GameObject("MainSpaceshipTeleportTests Handle");
        handleObject.AddComponent<NetworkIdentity>();
        HandleControl handle = handleObject.AddComponent<HandleControl>();
        handle.HandleTransform = handleObject.transform;
        handle.minPitch = 0f;
        handle.maxPitch = 90f;
        handle.stepCount = 7;
        return handle;
    }

    private sealed class SecondaryInteractionMachine : SyncedMachine
    {
        public int ServerSecondaryPressCount { get; private set; }

        protected override void ServerActionOnSecondaryInteract_press(PlayerMain who)
        {
            ServerSecondaryPressCount++;
        }
    }
}
