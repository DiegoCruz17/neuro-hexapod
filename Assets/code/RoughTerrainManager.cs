using UnityEngine;

namespace Assets.code
{
    public class RoughTerrainManager : MonoBehaviour
    {
        [Header("Terrain Environment")]
        [Tooltip("The rough terrain generator component")]
        public RoughTerrainGenerator terrainGenerator;
        
        [Header("Simple Controls")]
        [Range(0.1f, 2f)]
        [Tooltip("Terrain roughness/difficulty")]
        public float terrainRoughness = 1f;
        
        [Range(0.1f, 2f)]
        [Tooltip("Height variation amplitude")]
        public float terrainAmplitude = 0.5f;
        
        [Header("Fixed Settings")]
        [Tooltip("Fixed terrain platform size")]
        public float terrainSize = 10f;
        
        [Tooltip("Fixed terrain resolution")]
        public int terrainResolution = 50;
        
        [Header("Real-time Updates")]
        [Tooltip("Update terrain in real-time during play")]
        public bool enableRealTimeUpdate = true;
        
        private float previousRoughness;
        private float previousAmplitude;
        
        void Start()
        {
            ValidateComponents();
            
            // Store initial values
            previousRoughness = terrainRoughness;
            previousAmplitude = terrainAmplitude;
            
            // Initial terrain generation
            UpdateTerrainParameters();
        }
        
        void Update()
        {
            if (enableRealTimeUpdate)
            {
                // Check if any parameter has changed
                if (Mathf.Abs(terrainRoughness - previousRoughness) > 0.01f ||
                    Mathf.Abs(terrainAmplitude - previousAmplitude) > 0.01f)
                {
                    UpdateTerrainParameters();
                    
                    // Update previous values
                    previousRoughness = terrainRoughness;
                    previousAmplitude = terrainAmplitude;
                }
            }
        }
        
        void ValidateComponents()
        {
            if (terrainGenerator == null)
            {
                terrainGenerator = GetComponent<RoughTerrainGenerator>();
                if (terrainGenerator == null)
                {
                    Debug.LogError("RoughTerrainManager: RoughTerrainGenerator component not found!");
                    return;
                }
            }
        }
        
        public void UpdateTerrainParameters()
        {
            if (terrainGenerator == null)
            {
                ValidateComponents();
                return;
            }
            
            // Apply parameters to terrain generator
            terrainGenerator.roughnessScale = terrainRoughness;
            terrainGenerator.amplitude = terrainAmplitude;
            terrainGenerator.terrainSize = terrainSize;
            terrainGenerator.terrainResolution = terrainResolution;
            
            // Generate terrain
            terrainGenerator.GenerateRoughTerrain();
            
            Debug.Log($"Rough terrain updated - Roughness: {terrainRoughness:F1}, Amplitude: {terrainAmplitude:F1}");
        }
        
        // Public methods for runtime control (HUD interface)
        public void SetRoughness(float roughness)
        {
            terrainRoughness = Mathf.Clamp(roughness, 0.1f, 2f);
            UpdateTerrainParameters();
        }
        
        public void SetAmplitude(float amplitude)
        {
            terrainAmplitude = Mathf.Clamp(amplitude, 0.1f, 2f);
            UpdateTerrainParameters();
        }
        
        public void RegenerateRandomTerrain()
        {
            if (terrainGenerator != null)
            {
                terrainGenerator.GenerateRoughTerrain();
                Debug.Log("Random terrain regenerated");
            }
        }
        
        // Get terrain info
        public float GetTerrainComplexity()
        {
            return terrainRoughness * terrainAmplitude;
        }
        
        public string GetDifficultyLevel()
        {
            float complexity = GetTerrainComplexity();
            if (complexity < 0.5f) return "Easy";
            if (complexity < 1f) return "Medium";
            if (complexity < 2f) return "Hard";
            return "Extreme";
        }
        
        // Reset hexapod to center of terrain
        public void ResetHexapodToCenter()
        {
            if (Globals.hexapod != null)
            {
                Vector3 centerPos = transform.position;
                centerPos.y += 2f; // Place above terrain
                
                Globals.hexapod.hexapod.transform.position = centerPos;
                Globals.hexapod.hexapod.transform.rotation = Quaternion.identity;
                
                Debug.Log($"Hexapod reset to terrain center at position: {centerPos}");
            }
        }
        
        // Preset difficulty levels
        public void SetEasyTerrain()
        {
            SetRoughness(0.5f);
            SetAmplitude(0.3f);
        }
        
        public void SetMediumTerrain()
        {
            SetRoughness(1f);
            SetAmplitude(0.7f);
        }
        
        public void SetHardTerrain()
        {
            SetRoughness(1.5f);
            SetAmplitude(1.2f);
        }
        
        // Debug visualization
        void OnDrawGizmos()
        {
            if (terrainGenerator != null)
            {
                // Draw terrain bounds
                Gizmos.color = Color.cyan;
                Vector3 center = transform.position;
                Vector3 size = new Vector3(terrainSize, terrainAmplitude * 2, terrainSize);
                Gizmos.DrawWireCube(center, size);
                
                // Draw center point
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(center, 0.2f);
                
                // Draw spawn area
                Gizmos.color = Color.green;
                Vector3 spawnPos = center;
                spawnPos.y += 2f;
                Gizmos.DrawWireSphere(spawnPos, 0.5f);
            }
        }
    }
} 