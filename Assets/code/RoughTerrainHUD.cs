using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Assets.code
{
    public class RoughTerrainHUD : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The rough terrain manager to control")]
        public RoughTerrainManager roughTerrainManager;
        
        [Header("UI Setup")]
        [Tooltip("Automatically create UI on start")]
        public bool autoCreateUI = true;
        
        [Tooltip("UI Scale factor")]
        [Range(0.5f, 2f)]
        public float uiScale = 1f;
        
        [Header("UI Position")]
        [Tooltip("Position of the UI panel")]
        public Vector2 uiPosition = new Vector2(350, 20);
        
        // UI References
        private Canvas hudCanvas;
        private GameObject hudPanel;
        private Slider roughnessSlider;
        private Slider amplitudeSlider;
        private Text roughnessValueText;
        private Text amplitudeValueText;
        private Text infoText;
        private Button resetButton;
        private Button regenerateButton;
        private Button easyButton;
        private Button mediumButton;
        private Button hardButton;
        
        void Start()
        {
            if (autoCreateUI)
            {
                CreateHUD();
            }
            
            // Find components if not assigned
            if (roughTerrainManager == null)
                roughTerrainManager = FindObjectOfType<RoughTerrainManager>();
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
            
            // Create buttons
            CreateButtons();
            
            // Create info display
            CreateInfoDisplay();
            
            // Setup initial values
            UpdateUIValues();
            
            Debug.Log("Rough Terrain HUD created successfully!");
        }
        
        void CreateCanvas()
        {
            // Find existing canvas or create new one
            hudCanvas = FindObjectOfType<Canvas>();
            if (hudCanvas == null)
            {
                GameObject canvasObj = new GameObject("RoughTerrainCanvas");
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
            hudPanel = new GameObject("RoughTerrainPanel");
            hudPanel.transform.SetParent(hudCanvas.transform, false);
            
            // Add RectTransform
            RectTransform panelRect = hudPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = uiPosition;
            panelRect.sizeDelta = new Vector2(300 * uiScale, 320 * uiScale);
            
            // Add background image
            Image panelImage = hudPanel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.1f, 0.1f, 0.8f); // Dark red tint
            
            // Add title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(hudPanel.transform, false);
            
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Rough Terrain";
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
            float spacing = 80 * uiScale;
            
            // Roughness slider
            roughnessSlider = CreateSlider("Roughness", startY, 0.1f, 2f, 1f);
            roughnessSlider.onValueChanged.AddListener(OnRoughnessChanged);
            
            // Amplitude slider
            amplitudeSlider = CreateSlider("Amplitude", startY - spacing, 0.1f, 2f, 0.5f);
            amplitudeSlider.onValueChanged.AddListener(OnAmplitudeChanged);
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
            if (name == "Roughness") roughnessValueText = valueText;
            else if (name == "Amplitude") amplitudeValueText = valueText;
            
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
        
        void CreateButtons()
        {
            float buttonY = -170 * uiScale;
            float buttonSpacing = 35 * uiScale;
            
            // Reset button
            resetButton = CreateButton("Reset Robot", buttonY, Color.yellow, OnResetRobot);
            
            // Regenerate button
            regenerateButton = CreateButton("New Random", buttonY - buttonSpacing, Color.cyan, OnRegenerateRandom);
            
            // Preset buttons
            easyButton = CreateButton("Easy", buttonY - buttonSpacing * 2, Color.green, OnEasyPreset);
            mediumButton = CreateButton("Medium", buttonY - buttonSpacing * 2, Color.blue, OnMediumPreset);
            hardButton = CreateButton("Hard", buttonY - buttonSpacing * 2, Color.red, OnHardPreset);
            
            // Arrange preset buttons horizontally
            RectTransform easyRect = easyButton.GetComponent<RectTransform>();
            RectTransform mediumRect = mediumButton.GetComponent<RectTransform>();
            RectTransform hardRect = hardButton.GetComponent<RectTransform>();
            
            easyRect.sizeDelta = new Vector2(80 * uiScale, 25 * uiScale);
            mediumRect.sizeDelta = new Vector2(80 * uiScale, 25 * uiScale);
            hardRect.sizeDelta = new Vector2(80 * uiScale, 25 * uiScale);
            
            easyRect.anchoredPosition = new Vector2(-80 * uiScale, buttonY - buttonSpacing * 2);
            mediumRect.anchoredPosition = new Vector2(0, buttonY - buttonSpacing * 2);
            hardRect.anchoredPosition = new Vector2(80 * uiScale, buttonY - buttonSpacing * 2);
        }
        
        Button CreateButton(string text, float yPos, Color color, System.Action onClick)
        {
            GameObject buttonObj = new GameObject(text + "Button");
            buttonObj.transform.SetParent(hudPanel.transform, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1);
            buttonRect.anchorMax = new Vector2(0.5f, 1);
            buttonRect.pivot = new Vector2(0.5f, 1);
            buttonRect.anchoredPosition = new Vector2(0, yPos);
            buttonRect.sizeDelta = new Vector2(200 * uiScale, 30 * uiScale);
            
            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;
            
            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = Mathf.RoundToInt(12 * uiScale);
            buttonText.color = Color.black;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() => onClick());
            
            return button;
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
            if (roughTerrainManager == null) return;
            
            if (roughnessSlider != null)
                roughnessSlider.value = roughTerrainManager.terrainRoughness;
            if (amplitudeSlider != null)
                amplitudeSlider.value = roughTerrainManager.terrainAmplitude;
        }
        
        void UpdateInfoDisplay()
        {
            if (infoText != null && roughTerrainManager != null)
            {
                float complexity = roughTerrainManager.GetTerrainComplexity();
                string difficulty = roughTerrainManager.GetDifficultyLevel();
                
                infoText.text = $"Size: {roughTerrainManager.terrainSize:F1}m (Fixed)\nComplexity: {complexity:F2}\nDifficulty: {difficulty}";
            }
        }
        
        // Event handlers
        void OnRoughnessChanged(float value)
        {
            if (roughTerrainManager != null)
            {
                roughTerrainManager.SetRoughness(value);
                if (roughnessValueText != null)
                    roughnessValueText.text = value.ToString("F1");
            }
        }
        
        void OnAmplitudeChanged(float value)
        {
            if (roughTerrainManager != null)
            {
                roughTerrainManager.SetAmplitude(value);
                if (amplitudeValueText != null)
                    amplitudeValueText.text = value.ToString("F1");
            }
        }
        
        void OnResetRobot()
        {
            if (roughTerrainManager != null)
            {
                roughTerrainManager.ResetHexapodToCenter();
            }
        }
        
        void OnRegenerateRandom()
        {
            if (roughTerrainManager != null)
            {
                roughTerrainManager.RegenerateRandomTerrain();
            }
        }
        
        void OnEasyPreset()
        {
            if (roughTerrainManager != null)
            {
                roughTerrainManager.SetEasyTerrain();
                UpdateUIValues();
            }
        }
        
        void OnMediumPreset()
        {
            if (roughTerrainManager != null)
            {
                roughTerrainManager.SetMediumTerrain();
                UpdateUIValues();
            }
        }
        
        void OnHardPreset()
        {
            if (roughTerrainManager != null)
            {
                roughTerrainManager.SetHardTerrain();
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