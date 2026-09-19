using System;
using UnityEngine;

public static class MissionPortalPlacement
{
    public static Vector3 Calculate(
        Vector3 origin,
        float minimumDistance,
        float maximumDistance,
        Vector3 direction,
        float distance01)
    {
        if (minimumDistance < 0f)
            throw new ArgumentOutOfRangeException(nameof(minimumDistance));
        if (maximumDistance < minimumDistance)
            throw new ArgumentOutOfRangeException(nameof(maximumDistance));
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            throw new ArgumentException("Portal direction must be non-zero.", nameof(direction));

        float distance = Mathf.Lerp(minimumDistance, maximumDistance, Mathf.Clamp01(distance01));
        return origin + direction.normalized * distance;
    }
}
