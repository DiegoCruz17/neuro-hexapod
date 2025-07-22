using UnityEngine;

namespace Assets.code
{
    public class RoughTerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [Range(0.1f, 5f)]
        [Tooltip("Scale of the noise pattern - higher values create more frequent variations")]
        public float roughnessScale = 1f;
        
        [Range(0.1f, 2f)]
        [Tooltip("Maximum height variation of the terrain")]
        public float amplitude = 0.5f;
        
        [Range(20, 100)]
        [Tooltip("Resolution of the terrain mesh")]
        public int terrainResolution = 50;
        
        [Range(5f, 20f)]
        [Tooltip("Size of the terrain platform")]
        public float terrainSize = 10f;
        
        [Header("Noise Layers")]
        [Tooltip("Use multiple layers of noise for more complex terrain")]
        public bool useMultipleLayers = true;
        
        [Range(0f, 1f)]
        [Tooltip("Strength of the second noise layer")]
        public float secondLayerStrength = 0.5f;
        
        [Range(0f, 1f)]
        [Tooltip("Strength of the third noise layer")]
        public float thirdLayerStrength = 0.25f;
        
        [Header("Real-time Updates")]
        [Tooltip("Update terrain in real-time when parameters change")]
        public bool enableRealTimeUpdate = true;
        
        // Private variables
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private Mesh terrainMesh;
        private float previousRoughness;
        private float previousAmplitude;
        private int previousResolution;
        private float previousTerrainSize;
        
        void Start()
        {
            SetupComponents();
            GenerateRoughTerrain();
            
            // Store initial values
            previousRoughness = roughnessScale;
            previousAmplitude = amplitude;
            previousResolution = terrainResolution;
            previousTerrainSize = terrainSize;
        }
        
        void Update()
        {
            if (enableRealTimeUpdate)
            {
                // Check if any parameter has changed
                if (Mathf.Abs(roughnessScale - previousRoughness) > 0.01f ||
                    Mathf.Abs(amplitude - previousAmplitude) > 0.01f ||
                    terrainResolution != previousResolution ||
                    Mathf.Abs(terrainSize - previousTerrainSize) > 0.01f)
                {
                    GenerateRoughTerrain();
                    
                    // Update previous values
                    previousRoughness = roughnessScale;
                    previousAmplitude = amplitude;
                    previousResolution = terrainResolution;
                    previousTerrainSize = terrainSize;
                }
            }
        }
        
        void SetupComponents()
        {
            // Get or add MeshFilter
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            
            // Get or add MeshRenderer
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                // Apply default material
                meshRenderer.material = new Material(Shader.Find("Standard"));
                meshRenderer.material.color = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown terrain color
            }
            
            // Get or add MeshCollider
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        
        [ContextMenu("Generate Terrain")]
        public void GenerateRoughTerrain()
        {
            if (meshFilter == null)
                SetupComponents();
            
            // Create new mesh
            terrainMesh = new Mesh();
            terrainMesh.name = "RoughTerrain";
            
            // Generate vertices
            Vector3[] vertices = new Vector3[(terrainResolution + 1) * (terrainResolution + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            
            // Random offset for variation
            float offsetX = Random.Range(0f, 1000f);
            float offsetZ = Random.Range(0f, 1000f);
            
            for (int i = 0, z = 0; z <= terrainResolution; z++)
            {
                for (int x = 0; x <= terrainResolution; x++, i++)
                {
                    float xPos = (float)x / terrainResolution * terrainSize - terrainSize / 2;
                    float zPos = (float)z / terrainResolution * terrainSize - terrainSize / 2;
                    
                    // Generate height using Perlin noise
                    float height = GenerateHeightAtPosition(xPos, zPos, offsetX, offsetZ);
                    
                    vertices[i] = new Vector3(xPos, height, zPos);
                    uv[i] = new Vector2((float)x / terrainResolution, (float)z / terrainResolution);
                }
            }
            
            // Generate triangles
            int[] triangles = new int[terrainResolution * terrainResolution * 6];
            for (int ti = 0, vi = 0, z = 0; z < terrainResolution; z++, vi++)
            {
                for (int x = 0; x < terrainResolution; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + terrainResolution + 1;
                    triangles[ti + 5] = vi + terrainResolution + 2;
                }
            }
            
            // Apply mesh data
            terrainMesh.vertices = vertices;
            terrainMesh.triangles = triangles;
            terrainMesh.uv = uv;
            terrainMesh.RecalculateNormals();
            
            // Update components
            meshFilter.mesh = terrainMesh;
            meshCollider.sharedMesh = terrainMesh;
            
            Debug.Log($"Rough terrain generated - Resolution: {terrainResolution}, Size: {terrainSize}m, Roughness: {roughnessScale}, Amplitude: {amplitude}");
        }
        
        float GenerateHeightAtPosition(float x, float z, float offsetX, float offsetZ)
        {
            // Base layer
            float height = Mathf.PerlinNoise((x + offsetX) * roughnessScale * 0.1f, (z + offsetZ) * roughnessScale * 0.1f) * amplitude;
            
            if (useMultipleLayers)
            {
                // Second layer - more detailed
                height += Mathf.PerlinNoise((x + offsetX) * roughnessScale * 0.2f, (z + offsetZ) * roughnessScale * 0.2f) * amplitude * secondLayerStrength;
                
                // Third layer - fine details
                height += Mathf.PerlinNoise((x + offsetX) * roughnessScale * 0.4f, (z + offsetZ) * roughnessScale * 0.4f) * amplitude * thirdLayerStrength;
            }
            
            return height;
        }
        
        // Public methods for runtime control
        public void SetRoughness(float roughness)
        {
            roughnessScale = Mathf.Clamp(roughness, 0.1f, 5f);
            if (!enableRealTimeUpdate)
                GenerateRoughTerrain();
        }
        
        public void SetAmplitude(float amp)
        {
            amplitude = Mathf.Clamp(amp, 0.1f, 2f);
            if (!enableRealTimeUpdate)
                GenerateRoughTerrain();
        }
        
        public void SetTerrainSize(float size)
        {
            terrainSize = Mathf.Clamp(size, 5f, 20f);
            if (!enableRealTimeUpdate)
                GenerateRoughTerrain();
        }
        
        public void SetResolution(int resolution)
        {
            terrainResolution = Mathf.Clamp(resolution, 20, 100);
            if (!enableRealTimeUpdate)
                GenerateRoughTerrain();
        }
        
        // Get terrain info
        public float GetTerrainComplexity()
        {
            return roughnessScale * amplitude;
        }
        
        public string GetDifficultyLevel()
        {
            float complexity = GetTerrainComplexity();
            if (complexity < 0.5f) return "Easy";
            if (complexity < 1.5f) return "Medium";
            if (complexity < 3f) return "Hard";
            return "Extreme";
        }
        
        // Reset to default parameters
        public void ResetToDefault()
        {
            roughnessScale = 1f;
            amplitude = 0.5f;
            terrainResolution = 50;
            terrainSize = 10f;
            GenerateRoughTerrain();
        }
    }
} 