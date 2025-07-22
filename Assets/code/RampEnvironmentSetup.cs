using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.code
{
    [System.Serializable]
    [ExecuteInEditMode]
    public class RampEnvironmentSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [Tooltip("Automatically create ramp environment on Start (only in Play mode)")]
        public bool autoSetupOnStart = false;
        
        [Tooltip("Use existing floor materials if available")]
        public bool useExistingMaterials = true;
        
        [Header("Environment Settings")]
        [Tooltip("Initial height difference between platforms")]
        public float initialHeightDifference = 2f;
        
        [Header("Fixed Settings")]
        [Tooltip("Fixed ramp length")]
        public float initialRampLength = 10f;
        
        [Tooltip("Fixed distance between platforms")]
        public float platformDistance = 12f;
        
        [Header("Materials")]
        [Tooltip("Material for platforms")]
        public Material platformMaterial;
        
        [Tooltip("Material for ramp")]
        public Material rampMaterial;
        
        
        
        // References to created objects
        private GameObject environmentParent;
        private GameObject platform1;
        private GameObject platform2;
        private GameObject ramp;
        private TerrainTestManager terrainManager;
        private HexapodTestScene sceneManager;
        
        void Start()
        {
            // Only setup automatically if enabled and no environment exists
            if (autoSetupOnStart && Application.isPlaying && environmentParent == null)
            {
                SetupRampEnvironment();
            }
            else if (!Application.isPlaying)
            {
                // In editor mode, use the inspector buttons or context menu
                Debug.Log("RampEnvironmentSetup: Use inspector buttons or right-click context menu to setup environment in Edit mode");
            }
        }
        
        [ContextMenu("Setup Ramp Environment")]
        public void SetupRampEnvironment()
        {
            Debug.Log("Setting up ramp environment...");
            
            // Create main environment parent
            CreateEnvironmentParent();
            
            
            
            // Create platforms
            CreatePlatforms();
            
            // Create ramp
            CreateRamp();
            
            // Set up terrain manager
            SetupTerrainManager();
            
            // Set up scene manager
            SetupSceneManager();
            
            // Force initial geometry update
            if (terrainManager != null)
            {
                terrainManager.UpdateRampGeometry();
            }
            
            // Mark scene as dirty if in editor
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
            #endif
            
            Debug.Log("Ramp environment setup complete!");
        }
        
        void CreateEnvironmentParent()
        {
            // Find existing or create new parent
            environmentParent = GameObject.Find("RampEnvironment");
            if (environmentParent == null)
            {
                environmentParent = new GameObject("RampEnvironment");
                environmentParent.transform.position = Vector3.zero;
            }
            else
            {
                Debug.Log("RampEnvironment already exists, using existing one");
            }
        }
        
        
        
        void CreatePlatforms()
        {
            // Find existing or create platform 1 (lower)
            platform1 = environmentParent.transform.Find("Platform1_Lower")?.gameObject;
            if (platform1 == null)
            {
                platform1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform1.name = "Platform1_Lower";
                platform1.transform.SetParent(environmentParent.transform);
                platform1.transform.position = new Vector3(-platformDistance/2, 0, 0);
                platform1.transform.localScale = new Vector3(4, 0.5f, 4);
                
                if (useExistingMaterials)
                {
                    TryApplyExistingMaterial(platform1, "Floor");
                }
                else if (platformMaterial != null)
                {
                    platform1.GetComponent<Renderer>().material = platformMaterial;
                }
            }
            
            // Find existing or create platform 2 (higher)
            platform2 = environmentParent.transform.Find("Platform2_Higher")?.gameObject;
            if (platform2 == null)
            {
                platform2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform2.name = "Platform2_Higher";
                platform2.transform.SetParent(environmentParent.transform);
                platform2.transform.position = new Vector3(platformDistance/2, initialHeightDifference, 0);
                platform2.transform.localScale = new Vector3(4, 0.5f, 4);
                
                if (useExistingMaterials)
                {
                    TryApplyExistingMaterial(platform2, "Floor");
                }
                else if (platformMaterial != null)
                {
                    platform2.GetComponent<Renderer>().material = platformMaterial;
                }
            }
        }
        
        void CreateRamp()
        {
            // Find existing or create ramp
            ramp = environmentParent.transform.Find("Ramp")?.gameObject;
            if (ramp == null)
            {
                ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = "Ramp";
                ramp.transform.SetParent(environmentParent.transform);
                
                // Initial transform will be set by TerrainTestManager
                ramp.transform.position = new Vector3(0, 0, 0);
                ramp.transform.rotation = Quaternion.identity;
                ramp.transform.localScale = Vector3.one;
                
                // Apply material
                if (useExistingMaterials)
                {
                    TryApplyExistingMaterial(ramp, "Floor");
                }
                else if (rampMaterial != null)
                {
                    ramp.GetComponent<Renderer>().material = rampMaterial;
                }
            }
        }
        

        
        void SetupTerrainManager()
        {
            // Add TerrainTestManager component to environment parent
            terrainManager = environmentParent.GetComponent<TerrainTestManager>();
            if (terrainManager == null)
            {
                terrainManager = environmentParent.AddComponent<TerrainTestManager>();
            }
            
            // Assign references
            terrainManager.platform1 = platform1;
            terrainManager.platform2 = platform2;
            terrainManager.ramp = ramp;
            terrainManager.heightDifference = initialHeightDifference;
            terrainManager.rampLength = initialRampLength;
            terrainManager.enableRealTimeUpdate = true;
            
            Debug.Log("TerrainTestManager configured");
        }
        
        void SetupSceneManager()
        {
            // Find or create scene manager
            sceneManager = FindObjectOfType<HexapodTestScene>();
            if (sceneManager == null)
            {
                GameObject sceneManagerObj = new GameObject("HexapodTestScene");
                sceneManager = sceneManagerObj.AddComponent<HexapodTestScene>();
            }
            
            // Assign references
            sceneManager.rampEnvironment = environmentParent;
            sceneManager.terrainTestManager = terrainManager;
            sceneManager.hexapodSpawnPoint = null; // No spawn point needed
            sceneManager.autoResetRobot = true;
            sceneManager.showDebugInfo = true;
            sceneManager.enableKeyboardControls = false; // Use HUD instead
            
            // Set up HUD control system
            SetupHUDSystem();
            
            Debug.Log("HexapodTestScene configured");
        }
        
        void SetupHUDSystem()
        {
            // Find or create HUD controller
            RampControlHUD hudController = FindObjectOfType<RampControlHUD>();
            if (hudController == null)
            {
                GameObject hudObj = new GameObject("RampControlHUD");
                hudController = hudObj.AddComponent<RampControlHUD>();
            }
            
            // Assign references
            hudController.terrainTestManager = terrainManager;
            hudController.sceneManager = sceneManager;
            hudController.autoCreateUI = true;
            
            Debug.Log("RampControlHUD configured");
        }
        
        void TryApplyExistingMaterial(GameObject obj, string materialNameContains)
        {
            // Try to find existing materials in the project
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material mat in materials)
            {
                if (mat.name.Contains(materialNameContains))
                {
                    obj.GetComponent<Renderer>().material = mat;
                    return;
                }
            }
            
            // If no existing material found, create a simple colored material
            Material defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            obj.GetComponent<Renderer>().material = defaultMat;
        }
        
        [ContextMenu("Clear Environment")]
        public void ClearEnvironment()
        {
            if (environmentParent != null)
            {
                DestroyImmediate(environmentParent);
                
                // Mark scene as dirty if in editor
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
                #endif
                
                Debug.Log("Ramp environment cleared");
            }
        }
        
        [ContextMenu("Reset to Initial Settings")]
        public void ResetToInitialSettings()
        {
            if (terrainManager != null)
            {
                terrainManager.heightDifference = initialHeightDifference;
                terrainManager.rampLength = initialRampLength;
                terrainManager.UpdateRampGeometry();
                Debug.Log("Environment reset to initial settings");
            }
        }
        
        [ContextMenu("Recreate Environment")]
        public void RecreateEnvironment()
        {
            ClearEnvironment();
            SetupRampEnvironment();
        }
        
        // Inspector utilities
        void OnDrawGizmos()
        {
            if (Application.isPlaying) return;
            
            // Draw preview of environment layout
            Gizmos.color = Color.blue;
            Vector3 platform1Pos = new Vector3(-platformDistance/2, 0.25f, 0);
            Vector3 platform2Pos = new Vector3(platformDistance/2, initialHeightDifference + 0.25f, 0);
            
            Gizmos.DrawWireCube(platform1Pos, new Vector3(4, 0.5f, 4));
            Gizmos.DrawWireCube(platform2Pos, new Vector3(4, 0.5f, 4));
            
            // Draw ramp line connecting platform edges
            Gizmos.color = Color.green;
            Vector3 platform1EdgePos = new Vector3(platform1Pos.x + 2f, platform1Pos.y + 0.25f, platform1Pos.z);
            Vector3 platform2EdgePos = new Vector3(platform2Pos.x - 2f, platform2Pos.y + 0.25f, platform2Pos.z);
            Gizmos.DrawLine(platform1EdgePos, platform2EdgePos);
            
            // Draw edge connection points
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(platform1EdgePos, 0.1f);
            Gizmos.DrawSphere(platform2EdgePos, 0.1f);
        }
    }
} 