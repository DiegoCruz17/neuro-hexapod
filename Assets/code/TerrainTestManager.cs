using UnityEngine;

namespace Assets.code
{
    public class TerrainTestManager : MonoBehaviour
    {
        [Header("Ramp Environment")]
        [Tooltip("First platform (lower)")]
        public GameObject platform1;
        
        [Tooltip("Second platform (higher)")]
        public GameObject platform2;
        
        [Tooltip("Ramp connecting the platforms")]
        public GameObject ramp;
        
        [Header("Ramp Settings")]
        [Range(0f, 10f)]
        [Tooltip("Height difference between platforms")]
        public float heightDifference = 2f;
        
        [Header("Fixed Settings")]
        [Tooltip("Fixed horizontal distance between platforms")]
        public float rampLength = 10f;
        
        [Tooltip("Fixed width of the ramp")]
        public float rampWidth = 2f;
        
        [Header("Platform Settings")]
        [Range(2f, 8f)]
        [Tooltip("Size of each platform")]
        public float platformSize = 4f;
        
        [Header("Real-time Updates")]
        [Tooltip("Update ramp geometry in real-time during play")]
        public bool enableRealTimeUpdate = true;
        
        private float previousHeight;
        private float previousPlatformSize;
        
        void Start()
        {
            ValidateComponents();
            UpdateRampGeometry();
            
            // Store initial values
            previousHeight = heightDifference;
            previousPlatformSize = platformSize;
        }
        
        void Update()
        {
            if (enableRealTimeUpdate)
            {
                // Check if any parameter has changed
                if (Mathf.Abs(heightDifference - previousHeight) > 0.01f ||
                    Mathf.Abs(platformSize - previousPlatformSize) > 0.01f)
                {
                    UpdateRampGeometry();
                    
                    // Update previous values
                    previousHeight = heightDifference;
                    previousPlatformSize = platformSize;
                }
            }
        }
        
        void ValidateComponents()
        {
            if (platform1 == null || platform2 == null || ramp == null)
            {
                Debug.LogError("TerrainTestManager: Missing required GameObjects. Please assign platform1, platform2, and ramp in the inspector.");
                return;
            }
            
            // Ensure objects have the necessary components
            EnsureCollider(platform1);
            EnsureCollider(platform2);
            EnsureCollider(ramp);
        }
        
        void EnsureCollider(GameObject obj)
        {
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<BoxCollider>();
            }
        }
        
        public void UpdateRampGeometry()
        {
            if (platform1 == null || platform2 == null || ramp == null)
                return;
            
            // Update platform sizes
            UpdatePlatformSize(platform1, platformSize);
            UpdatePlatformSize(platform2, platformSize);
            
            // Position platforms
            Vector3 platform1Pos = platform1.transform.position;
            platform1Pos.y = 0.25f; // Half the platform height (0.5f / 2)
            platform1.transform.position = platform1Pos;
            
            Vector3 platform2Pos = platform2.transform.position;
            platform2Pos.y = heightDifference + 0.25f; // Height difference + half platform height
            platform2.transform.position = platform2Pos;
            
            // Calculate top surface positions of platforms
            float platform1TopY = platform1Pos.y + 0.25f; // Platform center + half height = top surface
            float platform2TopY = platform2Pos.y + 0.25f; // Platform center + half height = top surface
            
            // Calculate connection points at the edges of platforms
            float platformHalfSize = platformSize / 2f;
            Vector3 platform1EdgePos = new Vector3(
                platform1.transform.position.x + platformHalfSize, // Right edge of platform 1
                platform1TopY,
                platform1.transform.position.z
            );
            
            Vector3 platform2EdgePos = new Vector3(
                platform2.transform.position.x - platformHalfSize, // Left edge of platform 2
                platform2TopY,
                platform2.transform.position.z
            );
            
            // Calculate actual distance between platform edges
            float actualDistance = Vector3.Distance(platform1EdgePos, platform2EdgePos);
            float horizontalDistance = platform2EdgePos.x - platform1EdgePos.x;
            float verticalDistance = platform2EdgePos.y - platform1EdgePos.y;
            
            // Calculate ramp properties based on edge-to-edge connection
            float angle = Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg;
            
            // Position ramp between platform edges
            Vector3 rampPosition = new Vector3(
                (platform1EdgePos.x + platform2EdgePos.x) / 2,
                (platform1EdgePos.y + platform2EdgePos.y) / 2,
                platform1EdgePos.z
            );
            
            // Apply ramp transformation
            ramp.transform.position = rampPosition;
            // Rotate around Z-axis to create the slope
            ramp.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Scale ramp to fit between platform edges
            ramp.transform.localScale = new Vector3(
                actualDistance,  // Length along the slope (edge to edge)
                0.1f,           // Thickness of the ramp (very thin)
                rampWidth       // Width of the ramp (Z-axis)
            );
            
            // Update ramp material/color based on steepness for visual feedback
            UpdateRampVisualFeedback(angle);
            
            Debug.Log($"Ramp updated - Angle: {angle:F1}°, Position: {rampPosition}, Distance: {actualDistance:F1}m");
        }
        
        void UpdatePlatformSize(GameObject platform, float size)
        {
            platform.transform.localScale = new Vector3(size, 0.5f, size);
        }
        
        void UpdateRampVisualFeedback(float angle)
        {
            // Change ramp color based on steepness
            Renderer rampRenderer = ramp.GetComponent<Renderer>();
            if (rampRenderer != null)
            {
                Material rampMaterial = rampRenderer.material;
                
                // Green for easy slopes, yellow for moderate, red for steep
                if (angle < 15f)
                {
                    rampMaterial.color = Color.green;
                }
                else if (angle < 30f)
                {
                    rampMaterial.color = Color.yellow;
                }
                else
                {
                    rampMaterial.color = Color.red;
                }
            }
        }
        
        // Public methods for runtime control
        public void SetHeightDifference(float height)
        {
            heightDifference = Mathf.Clamp(height, 0f, 10f);
            UpdateRampGeometry();
        }
        

        
        public float GetRampAngle()
        {
            // Calculate based on actual edge-to-edge connection
            float platformHalfSize = platformSize / 2f;
            float horizontalDistance = rampLength - platformSize; // Distance between platform edges
            return Mathf.Atan2(heightDifference, horizontalDistance) * Mathf.Rad2Deg;
        }
        
        public float GetRampSteepness()
        {
            // Calculate based on actual edge-to-edge connection
            float platformHalfSize = platformSize / 2f;
            float horizontalDistance = rampLength - platformSize; // Distance between platform edges
            return heightDifference / horizontalDistance;
        }
        
        // Method to reset hexapod to start position
        public void ResetHexapodToStart()
        {
            if (Globals.hexapod != null && platform1 != null)
            {
                // Place hexapod on top surface of platform 1
                Vector3 startPos = platform1.transform.position;
                startPos.y = platform1.transform.position.y + 0.25f + 0.5f; // Platform center + half height + robot height
                startPos.z -= platformSize / 3f; // Place towards the back of platform 1
                
                Globals.hexapod.hexapod.transform.position = startPos;
                Globals.hexapod.hexapod.transform.rotation = Quaternion.identity;
                
                Debug.Log($"Hexapod reset to platform 1 surface at position: {startPos}");
            }
        }
        
        // Debug info
        void OnDrawGizmos()
        {
            if (platform1 != null && platform2 != null)
            {
                // Calculate platform edge positions
                float platformHalfSize = platformSize / 2f;
                float platform1TopY = platform1.transform.position.y + 0.25f;
                float platform2TopY = platform2.transform.position.y + 0.25f;
                
                Vector3 platform1EdgePos = new Vector3(
                    platform1.transform.position.x + platformHalfSize,
                    platform1TopY,
                    platform1.transform.position.z
                );
                
                Vector3 platform2EdgePos = new Vector3(
                    platform2.transform.position.x - platformHalfSize,
                    platform2TopY,
                    platform2.transform.position.z
                );
                
                // Draw connection line between platform edges
                Gizmos.color = Color.green;
                Gizmos.DrawLine(platform1EdgePos, platform2EdgePos);
                
                // Draw edge connection points
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(platform1EdgePos, 0.1f);
                Gizmos.DrawSphere(platform2EdgePos, 0.1f);
                
                // Draw platform top surfaces
                Gizmos.color = Color.yellow;
                Vector3 platform1TopCenter = new Vector3(platform1.transform.position.x, platform1TopY, platform1.transform.position.z);
                Vector3 platform2TopCenter = new Vector3(platform2.transform.position.x, platform2TopY, platform2.transform.position.z);
                Gizmos.DrawWireCube(platform1TopCenter, new Vector3(platformSize, 0.01f, platformSize));
                Gizmos.DrawWireCube(platform2TopCenter, new Vector3(platformSize, 0.01f, platformSize));
            }
        }
    }
} 