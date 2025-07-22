#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.code;

[CustomEditor(typeof(RoughTerrainSetup))]
public class RoughTerrainSetupEditor : Editor
{
    private RoughTerrainSetup setupScript;
    
    void OnEnable()
    {
        setupScript = (RoughTerrainSetup)target;
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Environment Setup", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Setup button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Setup Rough Terrain", GUILayout.Height(30)))
        {
            setupScript.SetupRoughTerrainEnvironment();
        }
        
        // Clear button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear Environment", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Environment", 
                "Are you sure you want to clear the rough terrain environment? This cannot be undone.", 
                "Yes", "Cancel"))
            {
                setupScript.ClearEnvironment();
            }
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Reset button
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Reset to Initial Settings", GUILayout.Height(25)))
        {
            setupScript.ResetToInitialSettings();
        }
        
        // Recreate button
        GUI.backgroundColor = Color.magenta;
        if (GUILayout.Button("Recreate Environment", GUILayout.Height(25)))
        {
            setupScript.RecreateEnvironment();
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Setup Tips", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Click 'Setup Rough Terrain' to create the complete testing environment\n" +
            "2. The terrain will be created with procedural Perlin noise\n" +
            "3. Use 'Clear Environment' to remove all created objects\n" +
            "4. You can adjust settings and click 'Reset to Initial Settings' to apply them\n" +
            "5. The HUD will automatically appear for runtime control", 
            MessageType.Info);
        
        // Show preview info
        if (setupScript != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Info", EditorStyles.boldLabel);
            
            float complexity = setupScript.initialRoughness * setupScript.initialAmplitude;
            
            EditorGUILayout.LabelField($"Terrain Size: {setupScript.terrainSize:F1}m x {setupScript.terrainSize:F1}m");
            EditorGUILayout.LabelField($"Resolution: {setupScript.terrainResolution} vertices");
            EditorGUILayout.LabelField($"Complexity: {complexity:F2}");
            
            string difficulty = "Easy";
            if (complexity >= 2f) difficulty = "Extreme";
            else if (complexity >= 1f) difficulty = "Hard";
            else if (complexity >= 0.5f) difficulty = "Medium";
            
            EditorGUILayout.LabelField($"Difficulty: {difficulty}");
        }
    }
}
#endif 