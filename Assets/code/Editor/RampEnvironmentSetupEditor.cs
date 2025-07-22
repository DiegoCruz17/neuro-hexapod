#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.code;

[CustomEditor(typeof(RampEnvironmentSetup))]
public class RampEnvironmentSetupEditor : Editor
{
    private RampEnvironmentSetup setupScript;
    
    void OnEnable()
    {
        setupScript = (RampEnvironmentSetup)target;
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
        if (GUILayout.Button("Setup Ramp Environment", GUILayout.Height(30)))
        {
            setupScript.SetupRampEnvironment();
        }
        
        // Clear button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear Environment", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Environment", 
                "Are you sure you want to clear the ramp environment? This cannot be undone.", 
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
            "1. Click 'Setup Ramp Environment' to create the complete testing environment\n" +
            "2. The environment will be created as child objects in the scene\n" +
            "3. Use 'Clear Environment' to remove all created objects\n" +
            "4. You can adjust settings and click 'Reset to Initial Settings' to apply them", 
            MessageType.Info);
        
        // Show preview info
        if (setupScript != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Info", EditorStyles.boldLabel);
            
            float angle = Mathf.Atan2(setupScript.initialHeightDifference, setupScript.initialRampLength) * Mathf.Rad2Deg;
            float steepness = setupScript.initialHeightDifference / setupScript.initialRampLength;
            
            EditorGUILayout.LabelField($"Ramp Angle: {angle:F1}°");
            EditorGUILayout.LabelField($"Steepness: {steepness:F2}");
            
            string difficulty = "Easy";
            if (angle >= 30f) difficulty = "Hard";
            else if (angle >= 15f) difficulty = "Medium";
            
            EditorGUILayout.LabelField($"Difficulty: {difficulty}");
        }
    }
}
#endif 