using UnityEngine;

namespace PJPR.EnvironmentObject
{
    public class SkySpawner : MonoBehaviour
    {
        [Tooltip("The background color of the outer space.")]
        public Color skyColor = Color.black;

        [Tooltip("Clip distance. Ensures the sky background reaches further than stars.")]
        public float skyFarClipPlane = 6000f;

        private void Start()
        {
            SetupSky();
        }

        private void SetupSky()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = skyColor;
                
                // Ensure camera can see stars that are very far away
                if (Camera.main.farClipPlane < skyFarClipPlane)
                {
                    Camera.main.farClipPlane = skyFarClipPlane;
                }
            }
            else
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                foreach (var cam in cameras)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = skyColor;
                    if (cam.farClipPlane < skyFarClipPlane)
                    {
                        cam.farClipPlane = skyFarClipPlane;
                    }
                }
            }
            
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.1f);
        }
    }
}