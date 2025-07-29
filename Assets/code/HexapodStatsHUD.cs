using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HexapodStatsHUD : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private GameObject antaresGameObject; // Single reference to the Antares GameObject
    [SerializeField] private Canvas hudCanvas;
    
    [Header("UI Settings")]
    [SerializeField] private bool showHUD = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private float updateFrequency = 30f; // Updates per second
    
    // Component references (automatically found from Antares GameObject)
    private AntaresController hexapodController;
    private Sensors sensors;
    
    // Main HUD Container
    private GameObject hudContainer;
    private RectTransform hudRect;
    
    // Expandable Sections
    private Dictionary<string, ExpandableSection> sections = new Dictionary<string, ExpandableSection>();
    
    // Graph System
    private GraphRenderer angleGraphRenderer;
    private GraphRenderer physicsGraphRenderer;
    private GraphRenderer sensorGraphRenderer;
    
    // Data Storage for Graphs
    private Queue<float>[] legAngleData = Enumerable.Repeat(new Queue<float>(new float[] { 0f }), 18).ToArray(); // 6 legs * 3 joints, filled with 0s
    private Queue<float>[] physicsData = Enumerable.Repeat(new Queue<float>(new float[] { 0f }), 12).ToArray(); // Position, rotation, velocity, angular velocity, filled with 0s
    private Queue<float>[] sensorData = Enumerable.Repeat(new Queue<float>(new float[] { 0f }), 8).ToArray(); // LIDAR sectors, filled with 0s
    
    private float lastUpdateTime;
    private int maxDataPoints = 200; // Maximum points to keep in history
    
    // Colors for different legs
    private Color[] legColors = {
        new Color(1f, 0.2f, 0.2f, 1f),    // Red - Leg 1
        new Color(1f, 0.6f, 0.2f, 1f),    // Orange - Leg 2
        new Color(1f, 1f, 0.2f, 1f),      // Yellow - Leg 3
        new Color(0.2f, 1f, 0.2f, 1f),    // Green - Leg 4
        new Color(0.2f, 0.6f, 1f, 1f),    // Blue - Leg 5
        new Color(0.8f, 0.2f, 1f, 1f)     // Purple - Leg 6
    };
    
    // Joint colors
    private Color[] jointColors = {
        new Color(1f, 0.3f, 0.3f, 1f),    // Coxa
        new Color(0.3f, 1f, 0.3f, 1f),    // Femur
        new Color(0.3f, 0.3f, 1f, 1f)     // Tibia
    };

    void Start()
    {
        InitializeComponentReferences();
        InitializeHUD();
        InitializeDataQueues();
    }

    void Update()
    {
        HandleInput();
        
        if (showHUD && Time.time - lastUpdateTime >= 1f / updateFrequency)
        {
            UpdateHUDData();
            lastUpdateTime = Time.time;
        }
    }

    private void InitializeComponentReferences()
    {
        // Auto-find Antares GameObject if not assigned
        if (antaresGameObject == null)
        {
            // Try to find by name first
            antaresGameObject = GameObject.Find("ANTARES");
            if (antaresGameObject == null)
            {
                // Try to find by component
                AntaresController controller = FindObjectOfType<AntaresController>();
                if (controller != null)
                {
                    antaresGameObject = controller.gameObject;
                }
            }
        }

        // Get components from the Antares GameObject
        if (antaresGameObject != null)
        {
            hexapodController = antaresGameObject.GetComponent<AntaresController>();
            sensors = antaresGameObject.GetComponent<Sensors>();
            
            if (hexapodController == null)
            {
                Debug.LogWarning($"HexapodStatsHUD: No AntaresController found on {antaresGameObject.name}!");
            }
            
            if (sensors == null)
            {
                Debug.LogWarning($"HexapodStatsHUD: No Sensors component found on {antaresGameObject.name}. Sensor data will not be available.");
            }
            else
            {
                Debug.Log($"HexapodStatsHUD: Successfully connected to Antares GameObject '{antaresGameObject.name}' with AntaresController and Sensors.");
            }
        }
        else
        {
            Debug.LogError("HexapodStatsHUD: Could not find Antares GameObject! Please assign it in the Inspector or ensure it exists in the scene.");
        }
        
        // Auto-find Canvas if not assigned
        if (hudCanvas == null)
        {
            hudCanvas = FindObjectOfType<Canvas>();
            if (hudCanvas == null)
            {
                Debug.LogError("HexapodStatsHUD: No Canvas found in scene! Please add a Canvas or assign one in the Inspector.");
            }
        }
    }

    private void InitializeHUD()
    {
        if (hudCanvas == null) return;
        
        CreateHUDContainer();
        CreateExpandableSections();
        CreateGraphRenderers();
        
        // Add helpful info section at the top
        CreateInfoSection();
        
        // Set initial HUD visibility
        hudContainer.SetActive(showHUD);
    }

    private void CreateInfoSection()
    {
        GameObject infoObj = new GameObject("InfoSection");
        infoObj.transform.SetParent(hudContainer.transform, false);
        
        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.5f, 1f);
        infoRect.anchorMax = new Vector2(0.5f, 1f);
        infoRect.sizeDelta = new Vector2(800, 50);
        infoRect.anchoredPosition = new Vector2(0, -25);
        
        // Background
        Image infoBg = infoObj.AddComponent<Image>();
        infoBg.color = new Color(0.1f, 0.1f, 0.3f, 0.9f);
        
        // Info text
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(infoObj.transform, false);
        
        Text infoText = textObj.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        infoText.fontSize = 14;
        infoText.fontStyle = FontStyle.Bold;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.supportRichText = true;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Set info text based on connection status
        string statusText = "";
        if (antaresGameObject != null)
        {
            statusText += $"<color=#00FF00>✓</color> Connected to: <color=#FFFF00>{antaresGameObject.name}</color>";
            if (hexapodController != null)
                statusText += $" <color=#00FF00>✓ Controller</color>";
            else
                statusText += $" <color=#FF0000>✗ Controller</color>";
                
            if (sensors != null)
                statusText += $" <color=#00FF00>✓ Sensors</color>";
            else
                statusText += $" <color=#FFAA00>⚠ No Sensors</color>";
        }
        else
        {
            statusText = "<color=#FF0000>✗ No Antares GameObject Found</color>";
        }
        
        statusText += " | <color=#AAAAAA>Press TAB to toggle | 1-5 for sections</color>";
        infoText.text = statusText;
        
        // Add outline
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);
    }

    private void CreateHUDContainer()
    {
        hudContainer = new GameObject("HexapodStatsHUD");
        hudContainer.transform.SetParent(hudCanvas.transform, false);
        
        hudRect = hudContainer.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 0);
        hudRect.anchorMax = new Vector2(1, 1);
        hudRect.sizeDelta = Vector2.zero;
        hudRect.anchoredPosition = Vector2.zero;
        
        // Add background
        Image background = hudContainer.AddComponent<Image>();
        background.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);
        background.raycastTarget = false;
    }

    private void CreateExpandableSections()
    {
        // Adjust positions to account for info section
        // Section 1: Leg Angles
        sections["LegAngles"] = CreateExpandableSection("Leg Angles", new Vector2(-400, 120), new Vector2(380, 300));
        
        // Section 2: Physics Data
        sections["Physics"] = CreateExpandableSection("Physics Data", new Vector2(0, 120), new Vector2(380, 250));
        
        // Section 3: Control Parameters
        sections["Control"] = CreateExpandableSection("Control Parameters", new Vector2(400, 120), new Vector2(350, 200));
        
        // Section 4: Sensor Data
        sections["Sensors"] = CreateExpandableSection("Sensor Data", new Vector2(-400, -200), new Vector2(380, 250));
        
        // Section 5: Performance
        sections["Performance"] = CreateExpandableSection("Performance", new Vector2(0, -200), new Vector2(350, 150));
    }

    private ExpandableSection CreateExpandableSection(string title, Vector2 position, Vector2 size)
    {
        GameObject sectionObj = new GameObject($"Section_{title}");
        sectionObj.transform.SetParent(hudContainer.transform, false);
        
        RectTransform rect = sectionObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        // Add background with rounded corners effect
        Image bg = sectionObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Add outline
        Outline outline = sectionObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.6f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);
        
        ExpandableSection section = sectionObj.AddComponent<ExpandableSection>();
        section.Initialize(title, size);
        
        return section;
    }

    private void CreateGraphRenderers()
    {
        // Create graph renderers for each section
        angleGraphRenderer = sections["LegAngles"].gameObject.AddComponent<GraphRenderer>();
        angleGraphRenderer.Initialize(new Vector2(360, 200), 18, "Leg Angles (°)");
        
        physicsGraphRenderer = sections["Physics"].gameObject.AddComponent<GraphRenderer>();
        physicsGraphRenderer.Initialize(new Vector2(360, 150), 12, "Physics Data");
        
        sensorGraphRenderer = sections["Sensors"].gameObject.AddComponent<GraphRenderer>();
        sensorGraphRenderer.Initialize(new Vector2(360, 150), 8, "Sensor Data");
    }

    private void InitializeDataQueues()
    {
        for (int i = 0; i < legAngleData.Length; i++)
            legAngleData[i] = new Queue<float>();
            
        for (int i = 0; i < physicsData.Length; i++)
            physicsData[i] = new Queue<float>();
            
        for (int i = 0; i < sensorData.Length; i++)
            sensorData[i] = new Queue<float>();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showHUD = !showHUD;
            hudContainer.SetActive(showHUD);
        }
        
        // Individual section toggles
        if (Input.GetKeyDown(KeyCode.Alpha1))
            sections["LegAngles"].ToggleExpanded();
        if (Input.GetKeyDown(KeyCode.Alpha2))
            sections["Physics"].ToggleExpanded();
        if (Input.GetKeyDown(KeyCode.Alpha3))
            sections["Control"].ToggleExpanded();
        if (Input.GetKeyDown(KeyCode.Alpha4))
            sections["Sensors"].ToggleExpanded();
        if (Input.GetKeyDown(KeyCode.Alpha5))
            sections["Performance"].ToggleExpanded();
    }

    private void UpdateHUDData()
    {
        if (hexapodController == null) return;
        
        UpdateLegAngleData();
        UpdatePhysicsData();
        UpdateControlData();
        UpdateSensorData();
        UpdatePerformanceData();
        
        // Update graphs
        UpdateGraphs();
    }

    private void UpdateLegAngleData()
    {
        var section = sections["LegAngles"];
        if (!section.IsExpanded) return;
        
        string angleText = "<color=#00FF00><b>LEG ANGLES</b></color>\n\n";
        
        if (hexapodController.controlMode == AntaresController.ControlMode.NeuralCircuit)
        {
            var state = GetNeuralState();
            if (state != null)
            {
                for (int leg = 0; leg < 6; leg++)
                {
                    Color legColor = legColors[leg];
                    string legColorHex = ColorUtility.ToHtmlStringRGB(legColor);
                    
                    angleText += $"<color=#{legColorHex}><b>Leg {leg + 1}:</b></color> ";
                    angleText += $"Q1: {state.Q1[leg]:F1}° ";
                    angleText += $"Q2: {state.Q2[leg]:F1}° ";
                    angleText += $"Q3: {state.Q3[leg]:F1}°\n";
                    
                    // Store data for graphs
                    StoreAngleData(leg * 3 + 0, state.Q1[leg]);
                    StoreAngleData(leg * 3 + 1, state.Q2[leg]);
                    StoreAngleData(leg * 3 + 2, state.Q3[leg]);
                }
            }
        }
        else
        {
            // For inverse kinematics mode, get angles from ArticulationBodies
            var coxas = GetCoxaTransforms();
            var femurs = GetFemurTransforms();
            var tibias = GetTibiaTransforms();
            
            for (int leg = 0; leg < 6; leg++)
            {
                if (leg < coxas.Length && leg < femurs.Length && leg < tibias.Length)
                {
                    float coxaAngle = GetArticulationAngle(coxas[leg]);
                    float femurAngle = GetArticulationAngle(femurs[leg]);
                    float tibiaAngle = GetArticulationAngle(tibias[leg]);
                    
                    Color legColor = legColors[leg];
                    string legColorHex = ColorUtility.ToHtmlStringRGB(legColor);
                    
                    angleText += $"<color=#{legColorHex}><b>Leg {leg + 1}:</b></color> ";
                    angleText += $"Coxa: {coxaAngle:F1}° ";
                    angleText += $"Femur: {femurAngle:F1}° ";
                    angleText += $"Tibia: {tibiaAngle:F1}°\n";
                    
                    // Store data for graphs
                    StoreAngleData(leg * 3 + 0, coxaAngle);
                    StoreAngleData(leg * 3 + 1, femurAngle);
                    StoreAngleData(leg * 3 + 2, tibiaAngle);
                }
            }
        }
        
        section.UpdateContent(angleText);
    }

    private void StoreAngleData(int index, float value)
    {
        if (index >= 0 && index < legAngleData.Length)
        {
            legAngleData[index].Enqueue(value);
            if (legAngleData[index].Count > maxDataPoints)
                legAngleData[index].Dequeue();
        }
    }

    private void UpdatePhysicsData()
    {
        var section = sections["Physics"];
        if (!section.IsExpanded) return;
        
        Rigidbody rb = hexapodController.GetComponent<Rigidbody>();
        ArticulationBody ab = hexapodController.GetComponent<ArticulationBody>();
        Transform transform = hexapodController.transform;
        
        string physicsText = "<color=#FFD700><b>PHYSICS DATA</b></color>\n\n";
        
        // Position
        Vector3 pos = transform.position;
        physicsText += $"<color=#FF6B6B><b>Position:</b></color>\n";
        physicsText += $"X: {pos.x:F2} Y: {pos.y:F2} Z: {pos.z:F2}\n\n";
        
        // Rotation
        Vector3 rot = transform.eulerAngles;
        physicsText += $"<color=#4ECDC4><b>Rotation:</b></color>\n";
        physicsText += $"X: {rot.x:F1}° Y: {rot.y:F1}° Z: {rot.z:F1}°\n\n";
        
        if (rb != null)
        {
            // Velocity
            Vector3 vel = rb.velocity;
            physicsText += $"<color=#45B7D1><b>Velocity:</b></color>\n";
            physicsText += $"X: {vel.x:F2} Y: {vel.y:F2} Z: {vel.z:F2}\n";
            physicsText += $"Magnitude: {vel.magnitude:F2} m/s\n\n";
            
            // Angular Velocity
            Vector3 angVel = rb.angularVelocity;
            physicsText += $"<color=#96CEB4><b>Angular Velocity:</b></color>\n";
            physicsText += $"X: {angVel.x:F2} Y: {angVel.y:F2} Z: {angVel.z:F2}\n";
        }
        
        if (ab != null)
        {
            physicsText += $"\n<color=#FFEAA7><b>Articulation Body:</b></color>\n";
            physicsText += $"Mass: {ab.mass:F2} kg\n";
            physicsText += $"Use Gravity: {ab.useGravity}\n";
        }
        
        section.UpdateContent(physicsText);
        
        // Store physics data for graphs
        StorePhysicsData(0, pos.x);
        StorePhysicsData(1, pos.y);
        StorePhysicsData(2, pos.z);
        StorePhysicsData(3, rot.x);
        StorePhysicsData(4, rot.y);
        StorePhysicsData(5, rot.z);
        
        if (rb != null)
        {
            StorePhysicsData(6, rb.velocity.x);
            StorePhysicsData(7, rb.velocity.y);
            StorePhysicsData(8, rb.velocity.z);
            StorePhysicsData(9, rb.angularVelocity.x);
            StorePhysicsData(10, rb.angularVelocity.y);
            StorePhysicsData(11, rb.angularVelocity.z);
        }
    }

    private void StorePhysicsData(int index, float value)
    {
        if (index >= 0 && index < physicsData.Length)
        {
            physicsData[index].Enqueue(value);
            if (physicsData[index].Count > maxDataPoints)
                physicsData[index].Dequeue();
        }
    }

    private void UpdateControlData()
    {
        var section = sections["Control"];
        if (!section.IsExpanded) return;
        
        string controlText = "<color=#E17055><b>CONTROL PARAMETERS</b></color>\n\n";
        
        controlText += $"<color=#74B9FF><b>Mode:</b></color> {hexapodController.controlMode}\n\n";
        
        if (hexapodController.controlMode == AntaresController.ControlMode.InverseKinematics)
        {
            controlText += $"<color=#A29BFE><b>Movement Parameters:</b></color>\n";
            controlText += $"Speed (d): {hexapodController.d:F1}\n";
            controlText += $"Amplitude (al): {hexapodController.al:F1}\n";
            controlText += $"Direction (rs): {hexapodController.rs:F2} rad\n";
            controlText += $"Phase (k): {hexapodController.k:F2}\n\n";
            
            controlText += $"<color=#6C5CE7><b>Body Position:</b></color>\n";
            controlText += $"Height (hb): {hexapodController.hb:F1}\n";
            controlText += $"Width (wb): {hexapodController.wb:F1}\n";
        }
        else
        {
            controlText += $"<color=#FD79A8><b>Neural Inputs:</b></color>\n";
            controlText += $"Forward: {hexapodController.go:F2}\n";
            controlText += $"Backward: {hexapodController.bk:F2}\n";
            controlText += $"Left: {hexapodController.left:F2}\n";
            controlText += $"Right: {hexapodController.right:F2}\n";
            controlText += $"Spin L: {hexapodController.spinL:F2}\n";
            controlText += $"Spin R: {hexapodController.spinR:F2}\n";
        }
        
        section.UpdateContent(controlText);
    }

    private void UpdateSensorData()
    {
        var section = sections["Sensors"];
        if (!section.IsExpanded) return;
        
        string sensorText = "<color=#00B894><b>SENSOR DATA</b></color>\n\n";
        
        if (sensors != null && sensors.CurrentScanData != null)
        {
            var scanData = sensors.CurrentScanData;
            sensorText += $"<color=#55A3FF><b>LIDAR Status:</b></color>\n";
            sensorText += $"Active: {sensors.IsScanning}\n";
            sensorText += $"Points: {scanData.Count}\n";
            sensorText += $"Last Scan: {Time.time - sensors.LastScanTime:F1}s ago\n\n";
            
            // Calculate average distances in 8 sectors
            float[] sectorDistances = new float[8];
            int[] sectorCounts = new int[8];
            
            foreach (var point in scanData)
            {
                if (point.hitDetected)
                {
                    int sector = Mathf.FloorToInt((point.angle + 180f) / 45f) % 8;
                    sectorDistances[sector] += point.distance;
                    sectorCounts[sector]++;
                }
            }
            
            sensorText += $"<color=#FDCB6E><b>Sector Distances (m):</b></color>\n";
            string[] directions = {"N", "NE", "E", "SE", "S", "SW", "W", "NW"};
            
            for (int i = 0; i < 8; i++)
            {
                float avgDistance = sectorCounts[i] > 0 ? sectorDistances[i] / sectorCounts[i] : 5f;
                sensorText += $"{directions[i]}: {avgDistance:F2} ";
                
                // Store sensor data for graphs
                StoreSensorData(i, avgDistance);
                
                if ((i + 1) % 4 == 0) sensorText += "\n";
            }
        }
        else if (sensors == null)
        {
            sensorText += "<color=#FFAA00>⚠ No Sensors component found on Antares GameObject</color>\n";
            sensorText += "Sensors component needs to be attached to the same GameObject as AntaresController.";
        }
        else
        {
            sensorText += "<color=#FF7675>No sensor data available</color>";
        }
        
        section.UpdateContent(sensorText);
    }

    private void StoreSensorData(int index, float value)
    {
        if (index >= 0 && index < sensorData.Length)
        {
            sensorData[index].Enqueue(value);
            if (sensorData[index].Count > maxDataPoints)
                sensorData[index].Dequeue();
        }
    }

    private void UpdatePerformanceData()
    {
        var section = sections["Performance"];
        if (!section.IsExpanded) return;
        
        string perfText = "<color=#E84393><b>PERFORMANCE</b></color>\n\n";
        
        perfText += $"<color=#00CEC9><b>Frame Rate:</b></color>\n";
        perfText += $"FPS: {1f / Time.deltaTime:F1}\n";
        perfText += $"Frame Time: {Time.deltaTime * 1000:F1}ms\n\n";
        
        perfText += $"<color=#FDCB6E><b>Simulation:</b></color>\n";
        perfText += $"Time Scale: {Time.timeScale:F2}\n";
        perfText += $"Fixed Delta: {Time.fixedDeltaTime * 1000:F1}ms\n";
        perfText += $"Physics Steps: {Time.fixedDeltaTime:F3}s\n\n";
        
        perfText += $"<color=#6C5CE7><b>Memory:</b></color>\n";
        perfText += $"Used: {System.GC.GetTotalMemory(false) / 1024 / 1024:F1} MB\n";
        
        section.UpdateContent(perfText);
    }

    private void UpdateGraphs()
    {
        // Update angle graphs
        if (sections["LegAngles"].IsExpanded && angleGraphRenderer != null)
        {
            int validLines = 0;
            for (int i = 0; i < legAngleData.Length; i++)
            {
                if (legAngleData[i].Count > 0)
                {
                    int legIndex = i / 3;
                    int jointIndex = i % 3;
                    Color color = Color.Lerp(legColors[legIndex], jointColors[jointIndex], 0.5f);
                    angleGraphRenderer.UpdateGraph(validLines, legAngleData[i].ToArray(), color);
                    validLines++;
                }
            }
        }
        
        // Update physics graphs
        if (sections["Physics"].IsExpanded && physicsGraphRenderer != null)
        {
            Color[] colors = {Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, 
                            Color.white, new Color(1f, 0.5f, 0f), new Color(0.5f, 1f, 0f), 
                            new Color(1f, 0f, 0.5f), new Color(0f, 1f, 0.5f), new Color(0.5f, 0f, 1f)};
            int validLines = 0;
            for (int i = 0; i < physicsData.Length; i++)
            {
                if (physicsData[i].Count > 0)
                {
                    physicsGraphRenderer.UpdateGraph(validLines, physicsData[i].ToArray(), colors[i % colors.Length]);
                    validLines++;
                }
            }
        }
        
        // Update sensor graphs
        if (sections["Sensors"].IsExpanded && sensorGraphRenderer != null)
        {
            int validLines = 0;
            for (int i = 0; i < sensorData.Length; i++)
            {
                if (sensorData[i].Count > 0)
                {
                    Color color = Color.HSVToRGB((float)i / 8f, 0.8f, 1f);
                    sensorGraphRenderer.UpdateGraph(validLines, sensorData[i].ToArray(), color);
                    validLines++;
                }
            }
        }
    }

    // Helper methods
    private HexapodState GetNeuralState()
    {
        // Access neural state from controller using reflection if needed
        var field = typeof(AntaresController).GetField("neuralState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(hexapodController) as HexapodState;
    }

    private Transform[] GetCoxaTransforms()
    {
        var field = typeof(AntaresController).GetField("coxas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(hexapodController) as Transform[];
    }

    private Transform[] GetFemurTransforms()
    {
        var field = typeof(AntaresController).GetField("femurs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(hexapodController) as Transform[];
    }

    private Transform[] GetTibiaTransforms()
    {
        var field = typeof(AntaresController).GetField("tibias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(hexapodController) as Transform[];
    }

    private float GetArticulationAngle(Transform joint)
    {
        if (joint == null) return 0f;
        
        var articulationBody = joint.GetComponent<ArticulationBody>();
        if (articulationBody != null && articulationBody.jointType == ArticulationJointType.RevoluteJoint)
        {
            return articulationBody.jointPosition[0] * Mathf.Rad2Deg;
        }
        
        return joint.localEulerAngles.x;
    }
}

// Expandable Section Component
public class ExpandableSection : MonoBehaviour
{
    private Text titleText;
    private Text contentText;
    private Button toggleButton;
    private GameObject contentPanel;
    private bool isExpanded = true;
    private Vector2 expandedSize;
    private Vector2 collapsedSize;
    private RectTransform rectTransform;
    
    public bool IsExpanded => isExpanded;
    
    public void Initialize(string title, Vector2 size)
    {
        rectTransform = GetComponent<RectTransform>();
        expandedSize = size;
        collapsedSize = new Vector2(size.x, 40);
        
        CreateTitle(title);
        CreateContent();
        CreateToggleButton();
    }
    
    private void CreateTitle(string title)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(transform, false);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = new Vector2(0, 30);
        titleRect.anchoredPosition = new Vector2(0, -15);
        
        titleText = titleObj.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleLeft;
        
        // Add outline
        Outline outline = titleObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);
    }
    
    private void CreateContent()
    {
        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(transform, false);
        
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.sizeDelta = new Vector2(-5, -35);  // Reduced margins for more space
        contentRect.anchoredPosition = new Vector2(0, -2.5f);  // Adjusted position
        
        contentText = contentPanel.AddComponent<Text>();
        contentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        contentText.fontSize = 12;
        contentText.color = Color.white;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.supportRichText = true;
        
        // Add outline
        Outline outline = contentPanel.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);
    }
    
    private void CreateToggleButton()
    {
        GameObject buttonObj = new GameObject("ToggleButton");
        buttonObj.transform.SetParent(transform, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.sizeDelta = new Vector2(25, 25);
        buttonRect.anchoredPosition = new Vector2(-15, -15);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        
        toggleButton = buttonObj.AddComponent<Button>();
        toggleButton.targetGraphic = buttonImage;
        toggleButton.onClick.AddListener(ToggleExpanded);
        
        // Add button text
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        
        Text buttonText = buttonTextObj.AddComponent<Text>();
        buttonText.text = "−";
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 18;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
        buttonTextRect.anchoredPosition = Vector2.zero;
    }
    
    public void ToggleExpanded()
    {
        isExpanded = !isExpanded;
        UpdateLayout();
    }
    
    private void UpdateLayout()
    {
        if (isExpanded)
        {
            rectTransform.sizeDelta = expandedSize;
            contentPanel.SetActive(true);
            toggleButton.GetComponentInChildren<Text>().text = "−";
        }
        else
        {
            rectTransform.sizeDelta = collapsedSize;
            contentPanel.SetActive(false);
            toggleButton.GetComponentInChildren<Text>().text = "+";
        }
    }
    
    public void UpdateContent(string content)
    {
        if (contentText != null)
            contentText.text = content;
    }
}

// Graph Renderer Component
public class GraphRenderer : MonoBehaviour
{
    private RawImage graphImage;
    private Texture2D graphTexture;
    private int graphWidth = 360;
    private int graphHeight = 150;
    private string graphTitle;
    private int maxLines;
    
    public void Initialize(Vector2 size, int maxDataLines, string title)
    {
        graphWidth = (int)size.x;
        graphHeight = (int)size.y;
        maxLines = maxDataLines;
        graphTitle = title;
        
        CreateGraphTexture();
        CreateGraphImage();
    }
    
    private void CreateGraphTexture()
    {
        graphTexture = new Texture2D(graphWidth, graphHeight);
        ClearGraph();
    }
    
    private void CreateGraphImage()
    {
        GameObject imageObj = new GameObject("Graph");
        imageObj.transform.SetParent(transform, false);
        
        RectTransform imageRect = imageObj.AddComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.02f, 0.02f);  // Small margin from edges
        imageRect.anchorMax = new Vector2(0.98f, 0.98f);  // Fill almost entire space
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;
        
        graphImage = imageObj.AddComponent<RawImage>();
        graphImage.texture = graphTexture;
    }
    
    public void UpdateGraph(int lineIndex, float[] data, Color color)
    {
        if (data.Length < 2) return;
        
        // Clear graph background before drawing (only for the first line of each update cycle)
        if (lineIndex == 0)
        {
            ClearGraph();
        }
        
        // Find min/max for scaling
        float min = data.Min();
        float max = data.Max();
        float range = max - min;
        if (range == 0) range = 1;
        
        // Scale data points to fill entire graph width
        float xScale = (float)(graphWidth - 1) / (data.Length - 1);
        
        // Draw line scaled to full width
        for (int i = 1; i < data.Length; i++)
        {
            float y1 = (data[i-1] - min) / range;
            float y2 = (data[i] - min) / range;
            
            // Scale X positions to fill entire width
            int x1 = Mathf.RoundToInt((i - 1) * xScale);
            int x2 = Mathf.RoundToInt(i * xScale);
            
            // Scale Y positions to fill height with small margins
            int scaledY1 = Mathf.RoundToInt(y1 * (graphHeight - 4)) + 2;
            int scaledY2 = Mathf.RoundToInt(y2 * (graphHeight - 4)) + 2;
            
            DrawLine(x1, scaledY1, x2, scaledY2, color);
        }
        
        graphTexture.Apply();
    }
    
    private void DrawLine(int x1, int y1, int x2, int y2, Color color)
    {
        // Clamp coordinates to texture bounds
        x1 = Mathf.Clamp(x1, 0, graphWidth - 1);
        x2 = Mathf.Clamp(x2, 0, graphWidth - 1);
        y1 = Mathf.Clamp(y1, 0, graphHeight - 1);
        y2 = Mathf.Clamp(y2, 0, graphHeight - 1);
        
        // Simple line drawing using Bresenham's algorithm
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            // Draw main pixel
            if (x1 >= 0 && x1 < graphWidth && y1 >= 0 && y1 < graphHeight)
            {
                graphTexture.SetPixel(x1, y1, color);
                
                // Draw thicker line by adding adjacent pixels
                if (y1 + 1 < graphHeight)
                    graphTexture.SetPixel(x1, y1 + 1, color);
                if (x1 + 1 < graphWidth)
                    graphTexture.SetPixel(x1 + 1, y1, color);
            }
            
            if (x1 == x2 && y1 == y2) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }
    
    private void ClearGraph()
    {
        Color[] pixels = new Color[graphWidth * graphHeight];
        Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        graphTexture.SetPixels(pixels);
        graphTexture.Apply();
    }
    
    public void ClearGraphExplicitly()
    {
        ClearGraph();
    }
}
