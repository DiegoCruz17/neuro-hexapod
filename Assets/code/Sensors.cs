using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sensors : MonoBehaviour
{
    [Header("LIDAR Configuration")]
    [SerializeField] private float lidarRange = 5f;           // Maximum range of LIDAR
    [SerializeField] private int numberOfRays = 180;           // Number of rays for 360-degree scan
    [SerializeField] private float scanAngle = 360f;           // Scan angle in degrees
    [SerializeField] private float scanFrequency = 0.5f;       // Scans per second
    [SerializeField] private LayerMask obstacleLayer = -1;     // Layer mask for obstacles
    
    [Header("Visualization")]
    [SerializeField] private bool showRays = true;             // Show raycast lines in scene view
    [SerializeField] private bool showHitPoints = true;        // Show hit points as spheres
    [SerializeField] private Color rayColor = Color.green;     // Color of raycast lines
    [SerializeField] private Color hitPointColor = Color.red;  // Color of hit point spheres
    [SerializeField] private float hitPointSize = 0.1f;        // Size of hit point spheres
    
    [Header("Data Output")]
    [SerializeField] private bool logScanData = false;         // Log scan data to console
    [SerializeField] private bool saveToFile = false;          // Save scan data to file
    
    // LIDAR data structures
    private List<LidarPoint> currentScanData;
    private float lastScanTime;
    private bool isScanning = false;
    
    // Visualization objects
    private List<GameObject> hitPointSpheres;
    
    // Public properties for external access
    public List<LidarPoint> CurrentScanData => currentScanData;
    public bool IsScanning => isScanning;
    public float LastScanTime => lastScanTime;
    
    // LIDAR point data structure
    [System.Serializable]
    public struct LidarPoint
    {
        public float angle;        // Angle in degrees
        public float distance;     // Distance to obstacle
        public Vector3 hitPoint;   // World position of hit point
        public bool hitDetected;   // Whether a hit was detected
        public string hitObject;   // Name of hit object
        
        public LidarPoint(float angle, float distance, Vector3 hitPoint, bool hitDetected, string hitObject)
        {
            this.angle = angle;
            this.distance = distance;
            this.hitPoint = hitPoint;
            this.hitDetected = hitDetected;
            this.hitObject = hitObject;
        }
    }
    
    void Start()
    {
        InitializeLidar();
    }
    
    void Update()
    {
        // Perform LIDAR scan at specified frequency
        if (Time.time - lastScanTime >= 1f / scanFrequency)
        {
            PerformLidarScan();
            lastScanTime = Time.time;
        }
        
        // Update visualization
        if (showRays)
        {
            DrawLidarRays();
        }
    }
    
    private void InitializeLidar()
    {
        currentScanData = new List<LidarPoint>();
        hitPointSpheres = new List<GameObject>();
        
        // Validate parameters
        numberOfRays = Mathf.Max(1, numberOfRays);
        scanAngle = Mathf.Clamp(scanAngle, 1f, 360f);
        lidarRange = Mathf.Max(0.1f, lidarRange);
        scanFrequency = Mathf.Max(0.1f, scanFrequency);
        
        Debug.Log($"LIDAR initialized: {numberOfRays} rays, {scanAngle}° scan angle, {lidarRange}m range, {scanFrequency}Hz frequency");
    }
    
    private void PerformLidarScan()
    {
        isScanning = true;
        currentScanData.Clear();
        
        // Clear previous hit point spheres
        ClearHitPointSpheres();
        
        float angleStep = scanAngle / numberOfRays;
        Vector3 sensorPosition = transform.position;
        
        for (int i = 0; i < numberOfRays; i++)
        {
            float currentAngle = i * angleStep - scanAngle / 2f;
            Vector3 rayDirection = GetRayDirection(currentAngle);
            
            // Perform raycast from current Y position
            RaycastHit hit;
            bool hitDetected = Physics.Raycast(sensorPosition, rayDirection, out hit, lidarRange, obstacleLayer);
            
            float distance = hitDetected ? hit.distance : lidarRange;
            Vector3 hitPoint = hitDetected ? hit.point : sensorPosition + rayDirection * lidarRange;
            string hitObject = hitDetected ? hit.collider.name : "No Hit";
            
            // Create LIDAR point
            LidarPoint point = new LidarPoint(currentAngle, distance, hitPoint, hitDetected, hitObject);
            currentScanData.Add(point);
            
            // Create visualization sphere for hit point
            if (showHitPoints && hitDetected)
            {
                CreateHitPointSphere(hitPoint);
            }
        }
        
        isScanning = false;
        
        // Log or save data if enabled
        if (logScanData)
        {
            LogScanData();
        }
        
        if (saveToFile)
        {
            SaveScanDataToFile();
        }
    }
    
    private Vector3 GetRayDirection(float angle)
    {
        // Convert angle to radians and get direction vector
        // Using Y axis as the rotation axis (scanning in X-Z plane)
        float angleRad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
    }
    
    private void DrawLidarRays()
    {
        if (currentScanData == null || currentScanData.Count == 0) return;
        
        Vector3 sensorPosition = transform.position;
        Debug.Log($"Sensor position: {sensorPosition}");
        
        
        foreach (var point in currentScanData)
        {
            Vector3 rayDirection = GetRayDirection(point.angle);
            Color rayColorToUse = point.hitDetected ? Color.red : rayColor;
            
            Debug.DrawRay(sensorPosition, rayDirection * point.distance, rayColorToUse, 1f / scanFrequency);
        }
    }
    
    private void CreateHitPointSphere(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * hitPointSize;
        
        // Set material color
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = hitPointColor;
            renderer.material = material;
        }
        
        // Remove collider to avoid interference
        Destroy(sphere.GetComponent<Collider>());
        
        hitPointSpheres.Add(sphere);
    }
    
    private void ClearHitPointSpheres()
    {
        foreach (var sphere in hitPointSpheres)
        {
            if (sphere != null)
            {
                Destroy(sphere);
            }
        }
        hitPointSpheres.Clear();
    }
    
    private void LogScanData()
    {
        Debug.Log($"LIDAR Scan at {Time.time:F2}s - {currentScanData.Count} points:");
        foreach (var point in currentScanData)
        {
            if (point.hitDetected)
            {
                Debug.Log($"  Angle: {point.angle:F1}°, Distance: {point.distance:F2}m, Object: {point.hitObject}");
            }
        }
    }
    
    private void SaveScanDataToFile()
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"lidar_scan_{timestamp}.csv";
        string filepath = Application.persistentDataPath + "/" + filename;
        
        try
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filepath))
            {
                writer.WriteLine("Angle,Distance,HitDetected,HitObject,X,Y,Z");
                foreach (var point in currentScanData)
                {
                    writer.WriteLine($"{point.angle},{point.distance},{point.hitDetected},{point.hitObject},{point.hitPoint.x},{point.hitPoint.y},{point.hitPoint.z}");
                }
            }
            Debug.Log($"LIDAR scan data saved to: {filepath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save LIDAR data: {e.Message}");
        }
    }
    
    // Public methods for external control
    public void StartContinuousScanning()
    {
        isScanning = true;
    }
    
    public void StopContinuousScanning()
    {
        isScanning = false;
    }
    
    public void PerformSingleScan()
    {
        PerformLidarScan();
    }
    
    public List<LidarPoint> GetScanData()
    {
        return new List<LidarPoint>(currentScanData);
    }
    
    public float GetDistanceAtAngle(float angle)
    {
        if (currentScanData == null || currentScanData.Count == 0) return lidarRange;
        
        // Find the closest angle in our scan data
        float closestAngle = float.MaxValue;
        LidarPoint closestPoint = currentScanData[0];
        
        foreach (var point in currentScanData)
        {
            float angleDiff = Mathf.Abs(point.angle - angle);
            if (angleDiff < closestAngle)
            {
                closestAngle = angleDiff;
                closestPoint = point;
            }
        }
        
        return closestPoint.distance;
    }
    
    public bool IsObstacleInDirection(float angle, float maxDistance)
    {
        float distance = GetDistanceAtAngle(angle);
        return distance < maxDistance;
    }
    
    public Vector3 GetClosestObstacleDirection()
    {
        if (currentScanData == null || currentScanData.Count == 0) return Vector3.zero;
        
        float minDistance = float.MaxValue;
        Vector3 closestDirection = Vector3.zero;
        
        foreach (var point in currentScanData)
        {
            if (point.hitDetected && point.distance < minDistance)
            {
                minDistance = point.distance;
                closestDirection = GetRayDirection(point.angle);
            }
        }
        
        return closestDirection;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw LIDAR range circle in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lidarRange);
        
        // Draw scan angle arc
        if (scanAngle < 360f)
        {
            Gizmos.color = Color.cyan;
            int segments = 20;
            float angleStep = scanAngle / segments;
            Vector3 startPos = transform.position;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep - scanAngle / 2f;
                float angle2 = (i + 1) * angleStep - scanAngle / 2f;
                
                Vector3 pos1 = transform.position + GetRayDirection(angle1) * lidarRange;
                Vector3 pos2 = transform.position + GetRayDirection(angle2) * lidarRange;
                
                Gizmos.DrawLine(pos1, pos2);
            }
        }
    }
}
