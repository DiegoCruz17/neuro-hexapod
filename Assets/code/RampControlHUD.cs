using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Assets.code
{
    public class RampControlHUD : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The terrain test manager to control")]
        public TerrainTestManager terrainTestManager;
        
        [Tooltip("The scene manager for robot control")]
        public HexapodTestScene sceneManager;
        
        [Header("UI Setup")]
        [Tooltip("Automatically create UI on start")]
        public bool autoCreateUI = true;
        
        [Tooltip("UI Scale factor")]
        [Range(0.5f, 2f)]
        public float uiScale = 1f;
        
        [Header("UI Position")]
        [Tooltip("Position of the UI panel")]
        public Vector2 uiPosition = new Vector2(50, 50);
        
        // UI References
        private Canvas hudCanvas;
        private GameObject hudPanel;
        private Slider heightSlider;
        private Text heightValueText;
        private Text infoText;
        
        
        void Start()
        {
            if (autoCreateUI)
            {
                CreateHUD();
            }
            
            // Find components if not assigned
            if (terrainTestManager == null)
                terrainTestManager = FindObjectOfType<TerrainTestManager>();
            if (sceneManager == null)
                sceneManager = FindObjectOfType<HexapodTestScene>();
        }
        
        void Update()
        {
            UpdateInfoDisplay();
        }
        
        [ContextMenu("Create HUD")]
        public void CreateHUD()
        {
            // Create main canvas
            CreateCanvas();
            
            // Create main panel
            CreateMainPanel();
            
            // Create sliders
            CreateSliders();
                        
            // Create info display
            CreateInfoDisplay();
            
            // Setup initial values
            UpdateUIValues();
            
            Debug.Log("Ramp Control HUD created successfully!");
        }
        
        void CreateCanvas()
        {
            // Find existing canvas or create new one
            hudCanvas = FindObjectOfType<Canvas>();
            if (hudCanvas == null)
            {
                GameObject canvasObj = new GameObject("RampControlCanvas");
                hudCanvas = canvasObj.AddComponent<Canvas>();
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = 10;
                
                // Add Canvas Scaler
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // Add GraphicRaycaster
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Ensure EventSystem exists for UI interaction
            EnsureEventSystem();
        }
        
        void EnsureEventSystem()
        {
            // Check if EventSystem already exists
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("EventSystem created for UI interaction");
            }
        }
        
        void CreateMainPanel()
        {
            hudPanel = new GameObject("RampControlPanel");
            hudPanel.transform.SetParent(hudCanvas.transform, false);
            
            // Add RectTransform
            RectTransform panelRect = hudPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = uiPosition;
            panelRect.sizeDelta = new Vector2(300 * uiScale, 300 * uiScale);
            
            // Add background image
            Image panelImage = hudPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Add title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(hudPanel.transform, false);
            
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Ramp Control";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = Mathf.RoundToInt(18 * uiScale);
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 30 * uiScale);
        }
        
        void CreateSliders()
        {
            float startY = -50 * uiScale;
            
            heightSlider = CreateSlider("Height", startY, 0f, 10f, 2f);
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }
        
        Slider CreateSlider(string name, float yPos, float minVal, float maxVal, float defaultVal)
        {
            GameObject sliderObj = new GameObject(name + "Slider");
            sliderObj.transform.SetParent(hudPanel.transform, false);
            
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(1, 1);
            sliderRect.pivot = new Vector2(0.5f, 1);
            sliderRect.anchoredPosition = new Vector2(0, yPos);
            sliderRect.sizeDelta = new Vector2(-20, 60 * uiScale);
            
            // Create label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sliderObj.transform, false);
            
            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = name + ":";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = Mathf.RoundToInt(14 * uiScale);
            labelText.color = Color.white;
            
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0.4f, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(10, -5);
            labelRect.sizeDelta = new Vector2(0, 20 * uiScale);
            
            // Create value text
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(sliderObj.transform, false);
            
            Text valueText = valueObj.AddComponent<Text>();
            valueText.text = defaultVal.ToString("F1");
            valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueText.fontSize = Mathf.RoundToInt(12 * uiScale);
            valueText.color = Color.cyan;
            valueText.alignment = TextAnchor.MiddleRight;
            
            RectTransform valueRect = valueObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.6f, 1);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.pivot = new Vector2(1, 1);
            valueRect.anchoredPosition = new Vector2(-10, -5);
            valueRect.sizeDelta = new Vector2(0, 20 * uiScale);
            
            // Store text reference
            if (name == "Height") heightValueText = valueText;
            
            // Create actual slider
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value = defaultVal;
            
            // Create background
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(sliderObj.transform, false);
            
            Image backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0);
            backgroundRect.anchorMax = new Vector2(1, 0);
            backgroundRect.pivot = new Vector2(0.5f, 0);
            backgroundRect.anchoredPosition = new Vector2(0, 10);
            backgroundRect.sizeDelta = new Vector2(-20, 20 * uiScale);
            
            // Create handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(sliderObj.transform, false);
            
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;
            
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20 * uiScale, 20 * uiScale);
            
            // Assign slider components
            slider.targetGraphic = handleImage;
            slider.handleRect = handleRect;
            
            return slider;
        }
        
        
        void CreateInfoDisplay()
        {
            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(hudPanel.transform, false);
            
            infoText = infoObj.AddComponent<Text>();
            infoText.text = "Info";
            infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            infoText.fontSize = Mathf.RoundToInt(10 * uiScale);
            infoText.color = Color.white;
            infoText.alignment = TextAnchor.UpperLeft;
            
            RectTransform infoRect = infoObj.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(1, 0);
            infoRect.pivot = new Vector2(0, 0);
            infoRect.anchoredPosition = new Vector2(10, 10);
            infoRect.sizeDelta = new Vector2(-20, 80 * uiScale);
        }
        
        void UpdateUIValues()
        {
            if (terrainTestManager == null) return;
            
            if (heightSlider != null)
                heightSlider.value = terrainTestManager.heightDifference;
        }
        
        void UpdateInfoDisplay()
        {
            if (infoText != null && terrainTestManager != null)
            {
                float angle = terrainTestManager.GetRampAngle();
                string difficulty = angle < 15f ? "Easy" : angle < 30f ? "Medium" : "Hard";
                
                infoText.text = $"Length: {terrainTestManager.rampLength:F1}m (Fixed)\nWidth: {terrainTestManager.rampWidth:F1}m (Fixed)\nAngle: {angle:F1}°\nDifficulty: {difficulty}";
            }
        }
        
        // Event handlers
        void OnHeightChanged(float value)
        {
            if (terrainTestManager != null)
            {
                terrainTestManager.SetHeightDifference(value);
                if (heightValueText != null)
                    heightValueText.text = value.ToString("F1");
            }
        }
        

        
        void OnResetRobot()
        {
            if (sceneManager != null)
            {
                sceneManager.ResetHexapodPosition();
            }
            else if (terrainTestManager != null)
            {
                terrainTestManager.ResetHexapodToStart();
            }
        }
        
        void OnRefreshRamp()
        {
            if (terrainTestManager != null)
            {
                terrainTestManager.UpdateRampGeometry();
                Debug.Log("Ramp geometry refreshed manually");
            }
        }
        
        void OnPresetChanged(HexapodTestScene.TestPreset preset)
        {
            if (sceneManager != null)
            {
                sceneManager.SetTestPreset(preset);
                UpdateUIValues();
            }
        }
        
        [ContextMenu("Clear HUD")]
        public void ClearHUD()
        {
            if (hudCanvas != null)
            {
                DestroyImmediate(hudCanvas.gameObject);
            }
        }
    }
} 