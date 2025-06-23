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
    // Removed turboFOVBoost
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

        desiredPosition += shakeOffset;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothSpeed * Time.deltaTime);

        HandleCameraRotation();

        UpdateFieldOfView();
    }

    void HandleInput()
    {
        HandleMouseLook();

        if (Input.GetKeyDown(resetCameraKey))
        {
            ResetCamera();
        }

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

        if (Mathf.Abs(mouseX) > 0.1f || Mathf.Abs(mouseY) > 0.1f)
        {
            manualRotationInput.x += mouseX;
            manualRotationInput.y += mouseY;

            manualRotationInput.x = Mathf.Clamp(manualRotationInput.x, -maxHorizontalAngle, maxHorizontalAngle);
            manualRotationInput.y = Mathf.Clamp(manualRotationInput.y, -maxVerticalAngle, maxVerticalAngle);

            manualControlTimer = 2f;
            isManualControl = true;
        }

        if (manualControlTimer > 0)
        {
            manualControlTimer -= Time.deltaTime;
        }
        else if (isManualControl)
        {
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
            float speedRatio = Mathf.Clamp01(bikeController.SpeedKmh / 50f);

            float targetDistance = Mathf.Lerp(distance, speedCameraDistance, speedRatio);
            float targetHeight = Mathf.Lerp(height, speedCameraHeight, speedRatio);

            currentDistance = Mathf.Lerp(currentDistance, targetDistance, speedTransitionSpeed * Time.deltaTime);
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, speedTransitionSpeed * Time.deltaTime);

            targetFOV = baseFOV + (speedRatio * speedFOVIncrease);

            // Removed turbo FOV boost
        }
        else
        {
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
            Quaternion manualRotation = Quaternion.Euler(manualRotationInput.y, target.eulerAngles.y + manualRotationInput.x, 0);
            direction = manualRotation * Vector3.back;
        }
        else if (followBikeRotation)
        {
            direction = -target.forward;
        }
        else
        {
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

        if (Physics.Raycast(target.position, direction.normalized, out hit, distanceToTarget, obstacleMask))
        {
            float adjustedDistance = hit.distance - collisionOffset;
            adjustedDistance = Mathf.Clamp(adjustedDistance, minDistance, maxDistance);
            desiredPosition = target.position + (direction.normalized * adjustedDistance);
        }

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
            Vector3 manualLookDirection = target.position - transform.position;
            if (manualLookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(manualLookDirection);
            }
            return;
        }

        Vector3 lookAtPoint = target.position;
        if (bikeController != null)
        {
            Vector3 targetVelocityVector = target.forward * (bikeController.CurrentSpeed * lookAheadDistance * 0.5f);
            lookAtPoint += targetVelocityVector;
        }

        Vector3 autoLookDirection = lookAtPoint - transform.position;
        if (autoLookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(autoLookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }

    void UpdateCameraEffects()
    {
        if (bikeController != null)
        {
            float speedRatio = bikeController.SpeedKmh / 50f;
            float baseShake = engineShakeIntensity * speedRatio;

            if (!bikeController.IsGrounded && targetVelocity > 0.1f)
            {
                currentShakeIntensity = Mathf.Max(currentShakeIntensity, landingShakeForce);
            }

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

    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    void OnDisable()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
