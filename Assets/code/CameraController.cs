using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hexapod Reference")]
    public Transform hexapodTarget; // Drag your hexapod robot here in the inspector
    
    [Header("Camera Settings")]
    public float followDistance = 15f;
    public float followHeight = 5f;
    public float followSpeed = 3f;
    public float rotationSpeed = 100f;
    
    [Header("View Settings")]
    public float perspectiveFOV = 80f;
    public float isometricSize = 5f;
    
    [Header("Isometric View Angles")]
    public Vector3 isometricRotation = new Vector3(45f, 45f, 0f);
    public Vector3 perspectiveRotation = new Vector3(20f, 0f, 0f);
    
    private Camera cam;
    private bool isIsometricView = false;
    private Vector3 offset;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    // Input handling
    private bool upArrowPressed = false;
    private bool downArrowPressed = false;
    private bool leftArrowPressed = false;
    private bool rightArrowPressed = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (cam == null)
        {
            Debug.LogError("CameraController: No Camera component found!");
            return;
        }
        
        if (hexapodTarget == null)
        {
            // Try to find the hexapod automatically
            GameObject hexapodObj = GameObject.Find("hexapod");
            if (hexapodObj != null)
            {
                hexapodTarget = hexapodObj.transform;
                Debug.Log("CameraController: Automatically found hexapod target");
            }
            else
            {
                Debug.LogWarning("CameraController: No hexapod target assigned and couldn't find 'hexapod' GameObject. Please assign the hexapod reference in the inspector.");
            }
        }
        
        // Initialize camera settings
        SetPerspectiveView();
        
        // Calculate initial offset
        if (hexapodTarget != null)
        {
            offset = new Vector3(0, followHeight, -followDistance);
        }
    }

    void Update()
    {
        HandleInput();
        FollowTarget();
    }
    
    void HandleInput()
    {
        // Check for arrow key presses (only trigger once per press)
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ToggleView();
        }
        
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ToggleView();
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            RotateAroundTarget(-90f);
        }
        
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            RotateAroundTarget(90f);
        }
    }
    
    void FollowTarget()
    {
        if (hexapodTarget == null) return;
        
        // Calculate target position based on current view mode
        if (isIsometricView)
        {
            // For isometric view, position camera at a fixed angle
            Vector3 direction = Quaternion.Euler(isometricRotation) * Vector3.back;
            targetPosition = hexapodTarget.position + direction * followDistance + Vector3.up * followHeight;
            targetRotation = Quaternion.LookRotation(hexapodTarget.position - targetPosition);
        }
        else
        {
            float hexapodYRotation = hexapodTarget.eulerAngles.y;
            Quaternion yOnlyRotation = Quaternion.Euler(0, hexapodYRotation, 0);
            
            Vector3 desiredPosition = hexapodTarget.position + yOnlyRotation * offset;
            targetPosition = desiredPosition;
            targetRotation = Quaternion.LookRotation(hexapodTarget.position - targetPosition);
        }
        
        // Smoothly move to target position and rotation
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
    }
    
    void ToggleView()
    {
        isIsometricView = !isIsometricView;
        
        if (isIsometricView)
        {
            SetIsometricView();
            Debug.Log("Camera: Switched to Isometric View");
        }
        else
        {
            SetPerspectiveView();
            Debug.Log("Camera: Switched to Perspective View");
        }
    }
    
    void SetPerspectiveView()
    {
        cam.orthographic = false;
        cam.fieldOfView = perspectiveFOV;
        isIsometricView = false;
    }
    
    void SetIsometricView()
    {
        cam.orthographic = true;
        cam.orthographicSize = isometricSize;
        isIsometricView = true;
    }
    
    void RotateAroundTarget(float angle)
    {
        if (hexapodTarget == null) return;
        
        // Rotate the offset around the Y axis
        offset = Quaternion.Euler(0, angle, 0) * offset;
        
        Debug.Log($"Camera: Rotated {angle} degrees around target");
    }
    
    // Public methods for external control
    public void SetTarget(Transform target)
    {
        hexapodTarget = target;
    }
    
    public void SetFollowDistance(float distance)
    {
        followDistance = distance;
        offset = offset.normalized * distance;
        offset.y = followHeight;
    }
    
    public void SetFollowHeight(float height)
    {
        followHeight = height;
        offset.y = height;
    }
    
    // Gizmos for debugging in Scene view
    void OnDrawGizmosSelected()
    {
        if (hexapodTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hexapodTarget.position, 0.5f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, hexapodTarget.position);
        }
    }
}
