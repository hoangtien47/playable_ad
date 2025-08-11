using UnityEngine;

public class BallPickup : MonoBehaviour
{
    [Header("Ball Settings")]
    public LayerMask ballLayerMask = (1 << 9); // Layer 9 where balls are located
    public string ballTag = "ballTag";
    public float maxPickupDistance = 15f;
    
    [Header("Throwing")]
    public float throwForceMultiplier = 15f;
    public float minSwipeSpeed = 150f; // Minimum swipe speed to throw
    public float swipeSensitivity = 1.5f; // How much swipe affects throw direction
    
    private Camera playerCamera;
    private GameObject heldBall;
    private Vector3 initialTouchPosition;
    private Vector3 currentTouchPosition;
    private bool isDragging = false;
    private Vector3 originalScale;
    private Rigidbody heldBallRb;
    private Collider heldBallCollider;
    private float touchStartTime;
    
    void Start()
    {
        // Get camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        
        // Find all balls and log their info
        GameObject[] balls = GameObject.FindGameObjectsWithTag(ballTag);

    }

    void Update()
    {
        HandleInput();
        
        if (heldBall != null && isDragging)
        {
            UpdateHeldBall();
        }
    }
    
    void HandleInput()
    {
        // Handle both mouse (for testing in editor) and touch (for mobile)
        bool inputDown = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        bool inputHeld = Input.GetMouseButton(0) || Input.touchCount > 0;
        bool inputUp = Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);
        
        Vector3 inputPosition = Input.mousePosition;
        if (Input.touchCount > 0)
        {
            inputPosition = Input.GetTouch(0).position;
        }
        
        if (inputDown)
        {
            initialTouchPosition = inputPosition;
            touchStartTime = Time.time;
            
            Debug.Log($"Input detected at position: {inputPosition}");
            
            if (heldBall == null)
            {
                TryPickupBall(inputPosition);
            }
        }
        else if (inputHeld && heldBall != null)
        {
            currentTouchPosition = inputPosition;
            isDragging = true;
        }
        else if (inputUp && heldBall != null)
        {
            if (isDragging)
            {
                // Check if it was a fast swipe
                float swipeTime = Time.time - touchStartTime;
                Vector3 swipeDelta = currentTouchPosition - initialTouchPosition;
                float swipeSpeed = swipeDelta.magnitude / swipeTime;
                
                if (swipeSpeed >= minSwipeSpeed && swipeDelta.y > 0) // Swipe up
                {
                    ThrowBall(swipeDelta, swipeSpeed);
                }
                else
                {
                    ReleaseBall(); // Just drop the ball
                }
            }
            else
            {
                ReleaseBall(); // Just clicked, release gently
            }
        }
    }
    
    void TryPickupBall(Vector3 screenPosition)
    {
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        
        Debug.Log($"Trying to pickup ball at screen position: {screenPosition}");
        Debug.Log($"Ray: {ray.origin} -> {ray.direction}");
        
        // Get all hits along the ray to check for balls even if something is in front
        RaycastHit[] hits = Physics.RaycastAll(ray, maxPickupDistance);
        Debug.Log($"Found {hits.Length} objects along the ray");
        
        // Look through all hits to find a ball
        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"Hit object: {hit.collider.name}, tag: {hit.collider.tag}, layer: {hit.collider.gameObject.layer}");
            
            GameObject hitObject = hit.collider.gameObject;
            
            // Check if it's a ball with the correct tag and on the right layer
            bool hasCorrectTag = hitObject.CompareTag(ballTag);
            bool hasCorrectLayer = hitObject.layer == 9; // Direct layer check since we know it's layer 9
            
            Debug.Log($"Tag check: {hasCorrectTag}, Layer check: {hasCorrectLayer} (layer is {hitObject.layer})");
            
            if (hasCorrectTag && hasCorrectLayer)
            {
                Debug.Log($"Found ball to pickup: {hitObject.name}");
                PickupBall(hitObject);
                return; // Exit after picking up the first ball found
            }
        }
        
        Debug.Log($"No balls with tag '{ballTag}' found in raycast hits");
    }
    
    void PickupBall(GameObject ball)
    {
        heldBall = ball;

        
        // Get components
        heldBallRb = ball.GetComponent<Rigidbody>();
        heldBallCollider = ball.GetComponent<Collider>();
        
        // Disable physics while held
        if (heldBallRb != null)
        {
            heldBallRb.isKinematic = true;
            heldBallRb.linearVelocity = Vector3.zero;
            heldBallRb.angularVelocity = Vector3.zero;
        }        
    }
    
    void UpdateHeldBall()
    {
        if (heldBall == null) return;
        
        // Get the distance from camera to ball to maintain Z position
        float distanceFromCamera = Vector3.Distance(playerCamera.transform.position, heldBall.transform.position);
        
        // Move ball to follow touch position but keep the same distance from camera
        Vector3 worldPosition = playerCamera.ScreenToWorldPoint(new Vector3(currentTouchPosition.x, currentTouchPosition.y, distanceFromCamera));
        
        // Only update X and Y, keep original Z relative to camera
        Vector3 targetPosition = new Vector3(worldPosition.x, worldPosition.y, heldBall.transform.position.z);
        heldBall.transform.position = Vector3.Lerp(heldBall.transform.position, targetPosition, 8f * Time.deltaTime);
    }
    
    void ThrowBall(Vector3 swipeDelta, float swipeSpeed)
    {
        if (heldBall == null || heldBallRb == null) return;
        
        // Use the camera's forward direction as base for throwing
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
        Vector3 cameraUp = playerCamera.transform.up;
        
        // Convert screen swipe to world direction
        float horizontalInput = swipeDelta.x / Screen.width; // -1 to 1
        float verticalInput = swipeDelta.y / Screen.height; // -1 to 1
        
        // Create throw direction based on camera orientation and swipe
        Vector3 throwDirection = cameraForward; // Start with forward direction
        throwDirection += cameraRight * horizontalInput * swipeSensitivity; // Add horizontal component (adjustable sensitivity)
        throwDirection += cameraUp * verticalInput * swipeSensitivity; // Add vertical component (adjustable sensitivity)

        throwDirection = throwDirection.normalized;
        
        // Calculate throw force based on swipe speed
        float throwForce = (swipeSpeed / 100f) * throwForceMultiplier;
        throwForce = Mathf.Clamp(throwForce, 10f, 50f); // Limit throw force
        
        // Ensure minimum upward force for basketball throwing
        if (throwDirection.y < 0.3f)
        {
            throwDirection.y = Mathf.Max(throwDirection.y, 0.3f);
            throwDirection = throwDirection.normalized;
        }
        
        // Restore ball physics
        heldBallRb.isKinematic = false;
        if (heldBallCollider != null)
        {
            heldBallCollider.isTrigger = false;
        }
        
        // Apply throw force
        heldBallRb.linearVelocity = throwDirection * throwForce;
        
        Debug.Log($"Threw ball with force: {throwForce}, direction: {throwDirection}");
        Debug.Log($"Swipe delta: {swipeDelta}, Horizontal: {horizontalInput}, Vertical: {verticalInput}");
        
        // Clear held ball
        heldBall = null;
        isDragging = false;
    }
    
    void ReleaseBall()
    {
        if (heldBall == null) return;
        
        // Gently drop the ball
        if (heldBallRb != null)
        {
            heldBallRb.isKinematic = false;
        }
        
        if (heldBallCollider != null)
        {
            heldBallCollider.isTrigger = false;
        }
        
        
        Debug.Log("Released ball: " + heldBall.name);
        
        // Clear held ball
        heldBall = null;
        isDragging = false;
    }
}
