using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 2f, -5f);
    public float distance = 5f;
    public float height = 2f;
    public float smoothSpeed = 5f;
    public float rotationSmoothSpeed = 10f;

    [Header("Mouse Look Controls")]
    public bool enableMouseLook = true;
    public float mouseXSensitivity = 2f;
    public float mouseYSensitivity = 2f;
    public bool invertY = false;
    public float maxVerticalAngle = 80f;
    public float maxHorizontalAngle = 120f;
    public float manualControlDecay = 3f;
    public KeyCode resetCameraKey = KeyCode.R;

    [Header("Speed-Based Camera")]
    public float speedCameraDistance = 8f;
    public float speedCameraHeight = 3f;
    public float speedTransitionSpeed = 2f;
    public float baseFOV = 60f;
    public float speedFOVIncrease = 15f;
    public float turboFOVBoost = 10f;
    public float fovTransitionSpeed = 3f;

    [Header("Camera Shake")]
    public float engineShakeIntensity = 0.05f;
    public float landingShakeForce = 1f;
    public float shakeDecay = 5f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleMask;
    public float minDistance = 1f;
    public float maxDistance = 10f;
    public float collisionOffset = 0.5f;
    public float wallAvoidanceForce = 2f;

    [Header("Advanced Settings")]
    public float lookAheadDistance = 2f;
    public float verticalAngleLimit = 30f;
    public float horizontalAngleLimit = 45f;
    public bool followBikeRotation = true;

    // Mouse Look Variables
    private Vector2 manualRotationInput;
    private float manualControlTimer;
    private bool isManualControl;

    // Speed & Effect Variables
    private float currentDistance;
    private float currentHeight;
    private float targetFOV;
    private Camera cam;
    private BikeController bikeController;
    private Vector3 shakeOffset;
    private float currentShakeIntensity;
    private Vector3 lastTargetPosition;
    private float targetVelocity;

    // Movement Variables
    private Vector3 currentVelocity;

    void Start()
    {
        InitializeCamera();
    }

    void InitializeCamera()
    {
        currentDistance = distance;
        currentHeight = height;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        targetFOV = baseFOV;
        if (cam != null) cam.fieldOfView = baseFOV;

        if (target != null)
        {
            bikeController = target.GetComponent<BikeController>();
            lastTargetPosition = target.position;
        }

        // Lock cursor for mouse look
        if (enableMouseLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        HandleInput();
        UpdateCameraEffects();
    }

    void LateUpdate()
    {
        if (target == null) return;

        CalculateSpeedBasedAdjustments();

        Vector3 desiredPosition = CalculateDesiredPosition();
        desiredPosition = HandleCameraCollision(desiredPosition);

        // Apply camera shake
        desiredPosition += shakeOffset;

        // Smoothly move the camera
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothSpeed * Time.deltaTime);

        // Handle rotation
        HandleCameraRotation();

        // Update FOV
        UpdateFieldOfView();
    }

    void HandleInput()
    {
        HandleMouseLook();

        // Reset camera to auto-follow
        if (Input.GetKeyDown(resetCameraKey))
        {
            ResetCamera();
        }

        // Toggle cursor lock/unlock
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            ToggleCursorLock();
        }
    }

    void HandleMouseLook()
    {
        if (!enableMouseLook || Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseYSensitivity * (invertY ? 1f : -1f);

        // Check if mouse is being moved
        if (Mathf.Abs(mouseX) > 0.1f || Mathf.Abs(mouseY) > 0.1f)
        {
            manualRotationInput.x += mouseX;
            manualRotationInput.y += mouseY;

            // Clamp the manual rotation
            manualRotationInput.x = Mathf.Clamp(manualRotationInput.x, -maxHorizontalAngle, maxHorizontalAngle);
            manualRotationInput.y = Mathf.Clamp(manualRotationInput.y, -maxVerticalAngle, maxVerticalAngle);

            manualControlTimer = 2f; // Reset decay timer
            isManualControl = true;
        }

        // Decay manual control over time
        if (manualControlTimer > 0)
        {
            manualControlTimer -= Time.deltaTime;
        }
        else if (isManualControl)
        {
            // Gradually return to auto follow
            manualRotationInput = Vector2.Lerp(manualRotationInput, Vector2.zero, manualControlDecay * Time.deltaTime);

            if (manualRotationInput.magnitude < 0.1f)
            {
                manualRotationInput = Vector2.zero;
                isManualControl = false;
            }
        }
    }

    void ResetCamera()
    {
        manualRotationInput = Vector2.zero;
        manualControlTimer = 0f;
        isManualControl = false;
    }

    void CalculateSpeedBasedAdjustments()
    {
        if (bikeController != null)
        {
            // Normalize speed to a 0-1 ratio (based on 50 km/h max for camera effects)
            float speedRatio = Mathf.Clamp01(bikeController.SpeedKmh / 50f);

            // Adjust distance and height based on speed
            float targetDistance = Mathf.Lerp(distance, speedCameraDistance, speedRatio);
            float targetHeight = Mathf.Lerp(height, speedCameraHeight, speedRatio);

            currentDistance = Mathf.Lerp(currentDistance, targetDistance, speedTransitionSpeed * Time.deltaTime);
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, speedTransitionSpeed * Time.deltaTime);

            // FOV adjustments - wider view at higher speeds
            targetFOV = baseFOV + (speedRatio * speedFOVIncrease);

            // Extra FOV and distance during turbo
            if (bikeController.TurboTimeRemaining > 0)
            {
                targetFOV += turboFOVBoost;
                currentDistance *= 1.2f; // Pull back during turbo
            }
        }
        else
        {
            // Fallback when no bike controller is found
            currentDistance = distance;
            currentHeight = height;
            targetFOV = baseFOV;
        }
    }

    Vector3 CalculateDesiredPosition()
    {
        if (target == null) return transform.position;

        Vector3 direction;

        if (isManualControl)
        {
            // Use manual rotation input relative to bike's orientation
            Quaternion manualRotation = Quaternion.Euler(manualRotationInput.y, target.eulerAngles.y + manualRotationInput.x, 0);
            direction = manualRotation * Vector3.back;
        }
        else if (followBikeRotation)
        {
            // Follow bike's rotation automatically
            direction = -target.forward;
        }
        else
        {
            // Maintain world-relative position with angle limits
            Vector3 currentDirection = (transform.position - target.position).normalized;
            if (currentDirection == Vector3.zero) currentDirection = Vector3.back;

            float currentAngle = Vector3.SignedAngle(Vector3.forward, currentDirection, Vector3.up);
            currentAngle = Mathf.Clamp(currentAngle, -horizontalAngleLimit, horizontalAngleLimit);

            float verticalAngle = Vector3.SignedAngle(Vector3.forward, currentDirection, Vector3.right);
            verticalAngle = Mathf.Clamp(verticalAngle, -verticalAngleLimit, verticalAngleLimit);

            Quaternion rotation = Quaternion.Euler(verticalAngle, currentAngle, 0);
            direction = rotation * Vector3.forward * -1f;
        }

        Vector3 basePosition = target.position + (direction * currentDistance);
        basePosition += Vector3.up * currentHeight;

        return basePosition;
    }

    Vector3 HandleCameraCollision(Vector3 desiredPosition)
    {
        RaycastHit hit;
        Vector3 direction = desiredPosition - target.position;
        float distanceToTarget = direction.magnitude;

        // Main collision check
        if (Physics.Raycast(target.position, direction.normalized, out hit, distanceToTarget, obstacleMask))
        {
            float adjustedDistance = hit.distance - collisionOffset;
            adjustedDistance = Mathf.Clamp(adjustedDistance, minDistance, maxDistance);
            desiredPosition = target.position + (direction.normalized * adjustedDistance);
        }

        // Wall avoidance - check sides to prevent camera getting stuck
        Vector3 right = transform.right;
        if (Physics.Raycast(desiredPosition, right, 2f, obstacleMask))
        {
            desiredPosition -= right * wallAvoidanceForce;
        }
        if (Physics.Raycast(desiredPosition, -right, 2f, obstacleMask))
        {
            desiredPosition += right * wallAvoidanceForce;
        }

        return desiredPosition;
    }

    void HandleCameraRotation()
    {
        if (isManualControl)
        {
            // During manual control, always look at the target
            Vector3 manualLookDirection = target.position - transform.position;
            if (manualLookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(manualLookDirection);
            }
            return;
        }

        // Auto follow - look at target with slight look-ahead
        Vector3 lookAtPoint = target.position;
        if (bikeController != null)
        {
            // Add look-ahead based on bike's speed and direction
            Vector3 targetVelocityVector = target.forward * (bikeController.CurrentSpeed * lookAheadDistance * 0.5f);
            lookAtPoint += targetVelocityVector;
        }

        // Smoothly rotate camera to look at target
        Vector3 autoLookDirection = lookAtPoint - transform.position;
        if (autoLookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(autoLookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }

    void UpdateCameraEffects()
    {
        // Camera shake effects
        if (bikeController != null)
        {
            // Engine shake based on speed
            float speedRatio = bikeController.SpeedKmh / 50f;
            float baseShake = engineShakeIntensity * speedRatio;

            // Extra shake when landing from jumps
            if (!bikeController.IsGrounded && targetVelocity > 0.1f)
            {
                currentShakeIntensity = Mathf.Max(currentShakeIntensity, landingShakeForce);
            }

            // Apply shake gradually
            currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, baseShake, shakeDecay * Time.deltaTime);

            if (currentShakeIntensity > 0.001f)
            {
                shakeOffset = new Vector3(
                    Random.Range(-currentShakeIntensity, currentShakeIntensity),
                    Random.Range(-currentShakeIntensity, currentShakeIntensity),
                    0
                );
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        // Update target velocity for effects
        if (target != null)
        {
            Vector3 currentTargetPosition = target.position;
            targetVelocity = (currentTargetPosition - lastTargetPosition).magnitude / Time.deltaTime;
            lastTargetPosition = currentTargetPosition;
        }
    }

    void UpdateFieldOfView()
    {
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }
    }

    void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Helper method to adjust camera distance (can be called from other scripts)
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    void OnDisable()
    {
        // Unlock cursor when camera is disabled
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}