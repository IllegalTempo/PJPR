using UnityEngine;
using System.Collections.Generic;

namespace PJPR.EnvironmentObject
{
    public class AlgorithmStarsSpawner : MonoBehaviour
    {
        [Header("Algorithm Constellations Configuration")]
        public int constellationCount = 35;
        public float spawnRadius = 3800f; 
        public float algorithmStarSize = 40f; 

        [Header("Constellation Groupings")]
        public int minStarsPerGroup = 4;
        public int maxStarsPerGroup = 9;
        public float groupSpread = 500f; 
        public float lineWidth = 20f; 

        [Header("Visuals")]
        public Color algorithmColor = new Color(0f, 0.8f, 1f, 1f); 
        public float emissionIntensity = 5f;

        private void Start()
        {
            if (Camera.main != null && Camera.main.farClipPlane < spawnRadius + 1000f)
            {
                Camera.main.farClipPlane = spawnRadius + 1000f;
            }
            GenerateConstellations();
        }

        private void GenerateConstellations()
        {
            GameObject algoHolder = new GameObject("AlgorithmStars_Background");
            algoHolder.transform.SetParent(this.transform);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (litShader == null)
            {
                litShader = Shader.Find("Unlit/Color");
            }

            Material solidMaterial = new Material(litShader);
            solidMaterial.color = algorithmColor;
            solidMaterial.EnableKeyword("_EMISSION");
            solidMaterial.SetColor("_EmissionColor", algorithmColor * emissionIntensity);

            int totalStars = 0;

            for (int cluster = 0; cluster < constellationCount; cluster++)
            {
                int starsInThisGroup = Random.Range(minStarsPerGroup, maxStarsPerGroup + 1);
                
                Vector3 clusterCenterDirection = Random.onUnitSphere;
                
                List<Vector3> clusterPositions = new List<Vector3>();

                for (int i = 0; i < starsInThisGroup; i++)
                {
                    Vector3 drift = Random.insideUnitSphere * (groupSpread / spawnRadius);
                    Vector3 starPos = (clusterCenterDirection + drift).normalized * spawnRadius;
                    clusterPositions.Add(starPos);
                    GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    star.name = $"AlgoStar_{cluster}_{i}";
                    star.transform.position = starPos;
                    star.transform.localScale = Vector3.one * algorithmStarSize;
                    star.transform.SetParent(algoHolder.transform);
                    Destroy(star.GetComponent<Collider>());

                    Renderer rend = star.GetComponent<Renderer>();
                    rend.material = solidMaterial;
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows = false;
                    totalStars++;
                }
                ConnectCluster(clusterPositions, algoHolder.transform, solidMaterial, cluster);
            }

            Debug.Log($"[AlgorithmStarsSpawner] Generated {constellationCount} isolated constellations with {totalStars} total stars.");
        }

        private void ConnectCluster(List<Vector3> positions, Transform parent, Material solidMaterial, int clusterId)
        {
            HashSet<string> connectedPairs = new HashSet<string>();

            void MakeSecureConnection(int idx1, int idx2, int id)
            {
                int min = Mathf.Min(idx1, idx2);
                int max = Mathf.Max(idx1, idx2);
                string key = $"{min}_{max}";
                if (!connectedPairs.Contains(key))
                {
                    connectedPairs.Add(key);
                    CreateLine(positions[idx1], positions[idx2], parent, solidMaterial, clusterId, id);
                }
            }
            for (int i = 0; i < positions.Count - 1; i++)
            {
                MakeSecureConnection(i, i + 1, i);
            }
            if (positions.Count > 3)
            {
                MakeSecureConnection(0, positions.Count - 1, 99); 

                if (positions.Count > 5)
                {
                    MakeSecureConnection(1, positions.Count - 2, 98); 
                }
            }
        }

        private void CreateLine(Vector3 pos1, Vector3 pos2, Transform parent, Material solidMaterial, int clusterId, int lineIdentifier)
        {
            GameObject lineObj = new GameObject($"AlgoLine_C{clusterId}_{lineIdentifier}");
            lineObj.transform.SetParent(parent);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = solidMaterial;
            lr.startColor = algorithmColor;
            lr.endColor = algorithmColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.SetPosition(0, pos1);
            lr.SetPosition(1, pos2);
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }
    }
}