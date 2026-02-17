using UnityEngine;

public class options
{
    public float mouseSensitivity = 0.04f;
    public string saveAsJSON()
    {
        return JsonUtility.ToJson(this);

    }
}
