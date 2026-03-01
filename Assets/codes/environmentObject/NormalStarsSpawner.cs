using UnityEngine;

namespace PJPR.EnvironmentObject
{
    public class NormalStarsSpawner : MonoBehaviour
    {
        [Header("Normal Stars (Background layer)")]
        public int starCount = 4000;
        public float spawnRadius = 3000f; // Adjusted closer so it doesn't instantly get culled
        public float minSize = 10f;
        public float maxSize = 30f;
        
        [Tooltip("Variety of star colors")]
        public Color[] starColors = { Color.white, new Color(0.85f, 0.95f, 1f), new Color(1f, 0.95f, 0.85f) };

        private void Start()
        {
            // Fully force the camera to extend its vision so you can see far away stars
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.farClipPlane < spawnRadius + 1000f)
                {
                    cam.farClipPlane = spawnRadius + 1000f;
                }
            }

            GenerateGameObjectStars();
        }

        private void GenerateGameObjectStars()
        {
            GameObject starsHolder = new GameObject("NormalStars_Background");
            starsHolder.transform.SetParent(this.transform);

            // Fetch absolute solid base materials like Algorithm stars to guarantee rendering
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (litShader == null) 
            {
                litShader = Shader.Find("Unlit/Color"); 
            }

            // Create 3 separate shared materials based on the chosen star colors so we can batch them visually
            Material[] coloredMats = new Material[starColors.Length];
            for (int j = 0; j < starColors.Length; j++)
            {
                coloredMats[j] = new Material(litShader);
                coloredMats[j].color = starColors[j];
                coloredMats[j].EnableKeyword("_EMISSION");
                coloredMats[j].SetColor("_EmissionColor", starColors[j] * 2f);
            }

            for (int i = 0; i < starCount; i++)
            {
                // Generate un-harmful rigid stars far away in a spherical shape
                Vector3 pos = Random.onUnitSphere * spawnRadius;
                
                // Using basic flat Quads instead of particle systems entirely bypasses Unity Particle Render issues!
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Quad);
                star.name = $"NormalStar_{i}";
                star.transform.position = pos;
                
                // Make the star face the center of the universe so it looks proportional
                star.transform.LookAt(Vector3.zero); 
                
                // Assign size
                star.transform.localScale = Vector3.one * Random.Range(minSize, maxSize);
                star.transform.SetParent(starsHolder.transform);

                // Absolutely CRITICAL: Remove the collider so it causes no physics issues
                Destroy(star.GetComponent<Collider>());

                Renderer rend = star.GetComponent<Renderer>();
                
                // Randomly pick one of our shared colored materials
                int colorIndex = Random.Range(0, starColors.Length);
                rend.material = coloredMats[colorIndex];
                
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            Debug.Log($"[NormalStarsSpawner] Hard-generated {starCount} normal stars using primitives.");
        }
    }
}