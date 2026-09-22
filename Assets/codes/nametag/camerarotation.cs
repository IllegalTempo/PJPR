using UnityEngine;

public class CameraFacingUI : MonoBehaviour
{
    private Vector3 offset = new Vector3(0, 180, 0);

    // Start is called before the first frame update
    protected virtual void Start()
    {
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        transform.LookAt(mainCamera.transform);
        transform.Rotate(offset);
    }
}

[System.Obsolete("Use CameraFacingUI.")]
public class UIrotator : CameraFacingUI
{
}
