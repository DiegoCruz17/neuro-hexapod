using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.code
{
    [System.Serializable]
    [ExecuteInEditMode]
    public class RoughTerrainSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [Tooltip("Automatically create rough terrain environment on Start (only in Play mode)")]
        public bool autoSetupOnStart = false;
        
        [Tooltip("Use existing floor materials if available")]
        public bool useExistingMaterials = true;
        
        [Header("Environment Settings")]
        [Tooltip("Initial terrain roughness")]
        public float initialRoughness = 1f;
        
        [Tooltip("Initial terrain amplitude")]
        public float initialAmplitude = 0.7f;
        
        [Header("Fixed Settings")]
        [Tooltip("Fixed terrain size")]
        public float terrainSize = 10f;
        
        [Tooltip("Fixed terrain resolution")]
        public int terrainResolution = 50;
        
        [Header("Materials")]
        [Tooltip("Material for terrain")]
        public Material terrainMaterial;
        
        // References to created objects
        private GameObject environmentParent;
        private GameObject terrainObject;
        private RoughTerrainGenerator terrainGenerator;
        private RoughTerrainManager terrainManager;
        private RoughTerrainHUD terrainHUD;
        
        void Start()
        {
            // Only setup automatically if enabled and no environment exists
            if (autoSetupOnStart && Application.isPlaying && environmentParent == null)
            {
                SetupRoughTerrainEnvironment();
            }
            else if (!Application.isPlaying)
            {
                // In editor mode, use the inspector buttons or context menu
                Debug.Log("RoughTerrainSetup: Use inspector buttons or right-click context menu to setup environment in Edit mode");
            }
        }
        
        [ContextMenu("Setup Rough Terrain Environment")]
        public void SetupRoughTerrainEnvironment()
        {
            Debug.Log("Setting up rough terrain environment...");
            
            // Create main environment parent
            CreateEnvironmentParent();
            
            // Create terrain object
            CreateTerrainObject();
            
            // Set up terrain generator
            SetupTerrainGenerator();
            
            // Set up terrain manager
            SetupTerrainManager();
            
            // Set up HUD system
            SetupHUDSystem();
            
            // Mark scene as dirty if in editor
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
            #endif
            
            Debug.Log("Rough terrain environment setup complete!");
        }
        
        void CreateEnvironmentParent()
        {
            // Find existing or create new parent
            environmentParent = GameObject.Find("RoughTerrainEnvironment");
            if (environmentParent == null)
            {
                environmentParent = new GameObject("RoughTerrainEnvironment");
                environmentParent.transform.position = Vector3.zero;
            }
            else
            {
                Debug.Log("RoughTerrainEnvironment already exists, using existing one");
            }
        }
        
        void CreateTerrainObject()
        {
            // Find existing or create terrain object
            terrainObject = environmentParent.transform.Find("RoughTerrain")?.gameObject;
            if (terrainObject == null)
            {
                terrainObject = new GameObject("RoughTerrain");
                terrainObject.transform.SetParent(environmentParent.transform);
                terrainObject.transform.position = Vector3.zero;
                terrainObject.transform.rotation = Quaternion.identity;
                terrainObject.transform.localScale = Vector3.one;
            }
        }
        
        void SetupTerrainGenerator()
        {
            // Add RoughTerrainGenerator component to terrain object
            terrainGenerator = terrainObject.GetComponent<RoughTerrainGenerator>();
            if (terrainGenerator == null)
            {
                terrainGenerator = terrainObject.AddComponent<RoughTerrainGenerator>();
            }
            
            // Configure generator
            terrainGenerator.roughnessScale = initialRoughness;
            terrainGenerator.amplitude = initialAmplitude;
            terrainGenerator.terrainSize = terrainSize;
            terrainGenerator.terrainResolution = terrainResolution;
            terrainGenerator.enableRealTimeUpdate = true;
            
            // Apply material if available
            if (useExistingMaterials)
            {
                TryApplyExistingMaterial(terrainObject, "Floor");
            }
            else if (terrainMaterial != null)
            {
                MeshRenderer renderer = terrainObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = terrainMaterial;
                }
            }
            
            Debug.Log("RoughTerrainGenerator configured");
        }
        
        void SetupTerrainManager()
        {
            // Add RoughTerrainManager component to environment parent
            terrainManager = environmentParent.GetComponent<RoughTerrainManager>();
            if (terrainManager == null)
            {
                terrainManager = environmentParent.AddComponent<RoughTerrainManager>();
            }
            
            // Assign references
            terrainManager.terrainGenerator = terrainGenerator;
            terrainManager.terrainRoughness = initialRoughness;
            terrainManager.terrainAmplitude = initialAmplitude;
            terrainManager.terrainSize = terrainSize;
            terrainManager.terrainResolution = terrainResolution;
            terrainManager.enableRealTimeUpdate = true;
            
            Debug.Log("RoughTerrainManager configured");
        }
        
        void SetupHUDSystem()
        {
            // Find or create HUD controller
            terrainHUD = FindObjectOfType<RoughTerrainHUD>();
            if (terrainHUD == null)
            {
                GameObject hudObj = new GameObject("RoughTerrainHUD");
                terrainHUD = hudObj.AddComponent<RoughTerrainHUD>();
            }
            
            // Assign references
            terrainHUD.roughTerrainManager = terrainManager;
            terrainHUD.autoCreateUI = true;
            
            Debug.Log("RoughTerrainHUD configured");
        }
        
        void TryApplyExistingMaterial(GameObject obj, string materialNameContains)
        {
            // Try to find existing materials in the project
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material mat in materials)
            {
                if (mat.name.Contains(materialNameContains))
                {
                    MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material = mat;
                        return;
                    }
                }
            }
            
            // If no existing material found, create a simple terrain material
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Material defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown terrain color
                meshRenderer.material = defaultMat;
            }
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
                
                Debug.Log("Rough terrain environment cleared");
            }
        }
        
        [ContextMenu("Reset to Initial Settings")]
        public void ResetToInitialSettings()
        {
            if (terrainManager != null)
            {
                terrainManager.terrainRoughness = initialRoughness;
                terrainManager.terrainAmplitude = initialAmplitude;
                terrainManager.UpdateTerrainParameters();
                Debug.Log("Environment reset to initial settings");
            }
        }
        
        [ContextMenu("Recreate Environment")]
        public void RecreateEnvironment()
        {
            ClearEnvironment();
            SetupRoughTerrainEnvironment();
        }
        
        // Inspector utilities
        void OnDrawGizmos()
        {
            if (Application.isPlaying) return;
            
            // Draw preview of terrain area
            Gizmos.color = Color.cyan;
            Vector3 terrainCenter = Vector3.zero;
            Vector3 terrainSize3D = new Vector3(terrainSize, initialAmplitude * 2, terrainSize);
            
            Gizmos.DrawWireCube(terrainCenter, terrainSize3D);
            
            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(terrainCenter, 0.2f);
            
            // Draw spawn area
            Gizmos.color = Color.green;
            Vector3 spawnPos = terrainCenter;
            spawnPos.y += 2f;
            Gizmos.DrawWireSphere(spawnPos, 0.5f);
        }
    }
} 