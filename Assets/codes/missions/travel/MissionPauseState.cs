using System.Collections.Generic;
using UnityEngine;

public sealed class MissionPauseState
{
    private readonly Dictionary<GameObject, bool> originalStates = new();
    private bool captured;

    public MissionPauseState(IEnumerable<GameObject> roots)
    {
        if (roots == null)
            return;

        foreach (GameObject root in roots)
        {
            if (root != null && !originalStates.ContainsKey(root))
                originalStates.Add(root, false);
        }
    }

    public void Pause()
    {
        if (captured)
            return;

        var roots = new List<GameObject>(originalStates.Keys);
        foreach (GameObject root in roots)
            originalStates[root] = root.activeSelf;

        captured = true;
        foreach (GameObject root in roots)
            root.SetActive(false);
    }

    public void Restore()
    {
        if (!captured)
            return;

        foreach (KeyValuePair<GameObject, bool> entry in originalStates)
        {
            if (entry.Key != null)
                entry.Key.SetActive(entry.Value);
        }

        captured = false;
    }
}
