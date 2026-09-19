using System;
using NUnit.Framework;
using UnityEngine;

public class MissionPortalPlacementTests
{
    [TestCase(100f, 300f, 0f, 100f)]
    [TestCase(100f, 300f, 0.5f, 200f)]
    [TestCase(100f, 300f, 1f, 300f)]
    public void Calculate_UsesConfiguredDistance(float minimum, float maximum, float sample, float expected)
    {
        Vector3 result = MissionPortalPlacement.Calculate(Vector3.one, minimum, maximum, Vector3.right, sample);

        Assert.That(Vector3.Distance(Vector3.one, result), Is.EqualTo(expected).Within(0.001f));
    }

    [Test]
    public void Calculate_RejectsInvalidRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MissionPortalPlacement.Calculate(Vector3.zero, 20f, 10f, Vector3.forward, 0.5f));
    }

    [Test]
    public void Calculate_RejectsZeroDirection()
    {
        Assert.Throws<ArgumentException>(() =>
            MissionPortalPlacement.Calculate(Vector3.zero, 10f, 20f, Vector3.zero, 0.5f));
    }
}
