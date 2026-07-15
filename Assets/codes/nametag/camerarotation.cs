using UnityEngine;

public class UIrotator : MonoBehaviour
{
    private Vector3 offset = new Vector3(0, 180, 0);

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(offset);
    }
}