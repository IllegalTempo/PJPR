using UnityEngine;

public class WaypointTarget : MonoBehaviour
{
    [SerializeField] private string waypointLabel = "Waypoint";
    [SerializeField] private bool showOnEnable;
    [SerializeField] private bool hideOnDisable = true;

    private void OnEnable()
    {
        if (showOnEnable)
        {
            Show();
        }
    }

    private void OnDisable()
    {
        if (hideOnDisable && UIManager.Instance != null)
        {
            UIManager.Instance.HideWaypoint();
        }
    }

    public void Show()
    {
        UIManager.Instance?.SetWaypoint(transform, waypointLabel);
    }

    public void Hide()
    {
        UIManager.Instance?.HideWaypoint();
    }
}
