using NUnit.Framework;
using UnityEngine;

public class MissionPauseStateTests
{
    [Test]
    public void Restore_PreservesOriginallyInactiveRootsAndIgnoresDuplicates()
    {
        GameObject active = new GameObject("Active");
        GameObject inactive = new GameObject("Inactive");
        inactive.SetActive(false);

        try
        {
            var pause = new MissionPauseState(new[] { active, inactive, active, null });
            pause.Pause();
            pause.Pause();
            Assert.That(active.activeSelf, Is.False);
            Assert.That(inactive.activeSelf, Is.False);

            pause.Restore();
            pause.Restore();
            Assert.That(active.activeSelf, Is.True);
            Assert.That(inactive.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(active);
            Object.DestroyImmediate(inactive);
        }
    }
}
