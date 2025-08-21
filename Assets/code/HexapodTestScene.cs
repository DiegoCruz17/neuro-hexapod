using UnityEngine;
using UnityEngine.UI;

namespace Assets.code
{
    public class HexapodTestScene : MonoBehaviour
    {
        [Header("Test Environment")]
        [Tooltip("The ramp test environment parent object")]
        public GameObject rampEnvironment;
        
        [Tooltip("The rough terrain environment parent object")]
        public GameObject roughTerrainEnvironment;
        
        [Tooltip("Terrain test manager component")]
        public TerrainTestManager terrainTestManager;
        
        [Tooltip("Rough terrain manager component")]
        public RoughTerrainManager roughTerrainManager;
        
        [Header("Robot Control")]
        [Tooltip("Default spawn position for the hexapod")]
        public Transform hexapodSpawnPoint;
        
        [Tooltip("Enable automatic robot reset when environment changes")]
        public bool autoResetRobot = true;
        
        [Header("UI Controls")]
        [Tooltip("UI Text to display current test info")]
        public Text testInfoText;
        
        [Tooltip("UI Slider for height difference control")]
        public Slider heightSlider;
        
        [Tooltip("UI Button to reset robot")]
        public Button resetRobotButton;
        
        [Header("Test Settings")]
        [Tooltip("Show debug information in UI")]
        public bool showDebugInfo = true;
        
        [Tooltip("Update UI every frame")]
        public bool enableUIUpdates = true;
        
        [Tooltip("Enable keyboard controls (optional if using HUD)")]
        public bool enableKeyboardControls = false;
        
        [Header("Environment Selection")]
        [Tooltip("Current active environment")]
        public EnvironmentType currentEnvironment = EnvironmentType.Ramp;
        
        private bool isInitialized = false;
        
        void Start()
        {
            
            InitializeTestScene();
        }
        
        void Update()
        {
            if (enableKeyboardControls)
            {
                HandleInput();
            }
            
            if (enableUIUpdates && showDebugInfo)
            {
                UpdateUI();
            }
        }
        
        void InitializeTestScene()
        {
            // Validate required components
            if (terrainTestManager == null)
            {
                terrainTestManager = FindObjectOfType<TerrainTestManager>();
            }
            
            if (roughTerrainManager == null)
            {
                roughTerrainManager = FindObjectOfType<RoughTerrainManager>();
            }
            
            // Setup UI controls
            SetupUIControls();
            
            // Enable environment based on current selection
            SwitchEnvironment(currentEnvironment);
            
            // Position hexapod at spawn point
            if (autoResetRobot)
            {
                ResetHexapodPosition();
            }
            
            isInitialized = true;
            Debug.Log("HexapodTestScene initialized successfully!");
        }
        
        void SetupUIControls()
        {
            // Setup height slider
            if (heightSlider != null)
            {
                heightSlider.minValue = 0f;
                heightSlider.maxValue = 10f;
                heightSlider.value = terrainTestManager.heightDifference;
                heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
            }
            
            // Setup reset button
            if (resetRobotButton != null)
            {
                resetRobotButton.onClick.AddListener(ResetHexapodPosition);
            }
        }
        
        void HandleInput()
        {
            if (!isInitialized) return;
            
            // Reset robot position
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetHexapodPosition();
            }
            
            // Quick height adjustments
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
            {
                AdjustHeight(0.5f);
            }
            
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                AdjustHeight(-0.5f);
            }
            

            
            // Environment switching
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchEnvironment(EnvironmentType.Ramp);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchEnvironment(EnvironmentType.RoughTerrain);
            }
            
            // Test presets
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetTestPreset(TestPreset.Easy);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetTestPreset(TestPreset.Medium);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SetTestPreset(TestPreset.Hard);
            }
        }
        
        void UpdateUI()
        {
            if (testInfoText != null)
            {
                string debugInfo = $"Current Environment: {currentEnvironment}\n\n";
                
                if (currentEnvironment == EnvironmentType.Ramp && terrainTestManager != null)
                {
                    float angle = terrainTestManager.GetRampAngle();
                    float steepness = terrainTestManager.GetRampSteepness();
                    
                    debugInfo += $"Ramp Test Environment\n";
                    debugInfo += $"Height Difference: {terrainTestManager.heightDifference:F1}m\n";
                    debugInfo += $"Ramp Length: {terrainTestManager.rampLength:F1}m (Fixed)\n";
                    debugInfo += $"Ramp Angle: {angle:F1}°\n";
                    debugInfo += $"Steepness: {steepness:F2}\n";
                    debugInfo += $"Difficulty: {GetDifficultyLevel(angle)}\n\n";
                }
                else if (currentEnvironment == EnvironmentType.RoughTerrain && roughTerrainManager != null)
                {
                    float complexity = roughTerrainManager.GetTerrainComplexity();
                    string difficulty = roughTerrainManager.GetDifficultyLevel();
                    
                    debugInfo += $"Rough Terrain Environment\n";
                    debugInfo += $"Roughness: {roughTerrainManager.terrainRoughness:F1}\n";
                    debugInfo += $"Amplitude: {roughTerrainManager.terrainAmplitude:F1}\n";
                    debugInfo += $"Complexity: {complexity:F2}\n";
                    debugInfo += $"Difficulty: {difficulty}\n\n";
                }
                
                debugInfo += $"Controls:\n";
                debugInfo += $"1 - Ramp Environment\n";
                debugInfo += $"2 - Rough Terrain Environment\n";
                debugInfo += $"R - Reset Robot\n";
                debugInfo += $"+/- - Adjust Parameters\n";
                debugInfo += $"3/4/5 - Easy/Medium/Hard";
                
                testInfoText.text = debugInfo;
            }
        }
        
        string GetDifficultyLevel(float angle)
        {
            if (angle < 15f) return "Easy";
            if (angle < 30f) return "Medium";
            if (angle < 45f) return "Hard";
            return "Extreme";
        }
        
        // UI Event Handlers
        void OnHeightSliderChanged(float value)
        {
            if (terrainTestManager != null)
            {
                terrainTestManager.SetHeightDifference(value);
            }
        }
        
        // Public Methods
        public void EnableRampTest()
        {
            SwitchEnvironment(EnvironmentType.Ramp);
        }
        
        public void EnableRoughTerrainTest()
        {
            SwitchEnvironment(EnvironmentType.RoughTerrain);
        }
        
        public void SwitchEnvironment(EnvironmentType environmentType)
        {
            currentEnvironment = environmentType;
            
            // Disable all environments first
            if (rampEnvironment != null)
                rampEnvironment.SetActive(false);
            if (roughTerrainEnvironment != null)
                roughTerrainEnvironment.SetActive(false);
            
            // Enable selected environment
            switch (environmentType)
            {
                case EnvironmentType.Ramp:
                    if (rampEnvironment != null)
                        rampEnvironment.SetActive(true);
                    Debug.Log("Switched to Ramp Environment");
                    break;
                    
                case EnvironmentType.RoughTerrain:
                    if (roughTerrainEnvironment != null)
                        roughTerrainEnvironment.SetActive(true);
                    Debug.Log("Switched to Rough Terrain Environment");
                    break;
            }
            
            // Reset robot position
            if (autoResetRobot)
            {
                ResetHexapodPosition();
            }
        }
        
        public void ResetHexapodPosition()
        {
            if (Globals.hexapod != null)
            {
                if (hexapodSpawnPoint != null)
                {
                    // Use custom spawn point
                    Globals.hexapod.hexapod.transform.position = hexapodSpawnPoint.position;
                    Globals.hexapod.hexapod.transform.rotation = hexapodSpawnPoint.rotation;
                }
                else if (currentEnvironment == EnvironmentType.Ramp && terrainTestManager != null)
                {
                    // Use ramp terrain test manager's reset method
                    terrainTestManager.ResetHexapodToStart();
                }
                else if (currentEnvironment == EnvironmentType.RoughTerrain && roughTerrainManager != null)
                {
                    // Use rough terrain manager's reset method
                    roughTerrainManager.ResetHexapodToCenter();
                }
                else
                {
                    // Default reset position
                    Globals.hexapod.hexapod.transform.position = new Vector3(0, 1, -5);
                    Globals.hexapod.hexapod.transform.rotation = Quaternion.identity;
                }
                
                Debug.Log("Hexapod reset to start position");
            }
        }
        
        public void AdjustHeight(float delta)
        {
            if (currentEnvironment == EnvironmentType.Ramp && terrainTestManager != null)
            {
                float newHeight = Mathf.Clamp(terrainTestManager.heightDifference + delta, 0f, 10f);
                terrainTestManager.SetHeightDifference(newHeight);
                
                // Update UI slider if available
                if (heightSlider != null)
                {
                    heightSlider.value = newHeight;
                }
            }
            else if (currentEnvironment == EnvironmentType.RoughTerrain && roughTerrainManager != null)
            {
                // For rough terrain, adjust roughness parameter
                float newRoughness = Mathf.Clamp(roughTerrainManager.terrainRoughness + delta, 0.1f, 2f);
                roughTerrainManager.SetRoughness(newRoughness);
            }
        }
        

        
        public enum TestPreset
        {
            Easy,
            Medium,
            Hard
        }
        
        public enum EnvironmentType
        {
            Ramp,
            RoughTerrain
        }
        
        public void SetTestPreset(TestPreset preset)
        {
            if (currentEnvironment == EnvironmentType.Ramp && terrainTestManager != null)
            {
                switch (preset)
                {
                    case TestPreset.Easy:
                        terrainTestManager.SetHeightDifference(1f);
                        break;
                        
                    case TestPreset.Medium:
                        terrainTestManager.SetHeightDifference(3f);
                        break;
                        
                    case TestPreset.Hard:
                        terrainTestManager.SetHeightDifference(5f);
                        break;
                }
            }
            else if (currentEnvironment == EnvironmentType.RoughTerrain && roughTerrainManager != null)
            {
                switch (preset)
                {
                    case TestPreset.Easy:
                        roughTerrainManager.SetEasyTerrain();
                        break;
                        
                    case TestPreset.Medium:
                        roughTerrainManager.SetMediumTerrain();
                        break;
                        
                    case TestPreset.Hard:
                        roughTerrainManager.SetHardTerrain();
                        break;
                }
            }
            
            Debug.Log($"Test preset set to: {preset} for {currentEnvironment} environment");
        }
        
        // Debug visualization
        void OnDrawGizmos()
        {
            if (hexapodSpawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(hexapodSpawnPoint.position, 0.5f);
                Gizmos.DrawRay(hexapodSpawnPoint.position, hexapodSpawnPoint.forward * 2f);
            }
        }
    }
} 