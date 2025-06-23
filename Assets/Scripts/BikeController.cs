using UnityEngine;
using UnityEngine.InputSystem;

public class BikeController : MonoBehaviour
{
    public float CurrentSpeed => currentSpeed;
    public bool IsGrounded => isGrounded;
    public bool IsWheeling => bikeModel != null && bikeModel.localEulerAngles.x > 5f;
    public bool HasPackage => hasPackage;
    public float SpeedKmh => Mathf.Abs(currentSpeed) * 3.6f;

    private bool hasPackage;

    [Header("Movement Settings")]
    public float maxSpeed = 15f;
    public float acceleration = 8f;
    public float deceleration = 12f;
    public float brakeForce = 20f;
    public float turnSpeed = 80f;
    public float turnSpeedAtSpeed = 1.5f;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;

    [Header("Physics Settings")]
    public float downForce = 100f;
    public float centerOfMassOffset = -0.5f;
    public float traction = 1f;
    public float airDrag = 0.98f;
    public float groundDrag = 0.95f;

    [Header("Advanced Features")]
    public float hopForce = 8f;
    public float wheelieAngle = 20f;
    public float wheelieSpeedThreshold = 0.3f;
    private bool isGrounded = true;
    private float groundCheckDistance = 1.2f;

    [Header("Visual Effects")]
    public float leanAngle = 20f;
    public float leanSpeed = 8f;
    public float bobbingAmplitude = 0.1f;
    public float bobbingFrequency = 2f;
    public ParticleSystem dustParticles;
    public Transform bikeModel;
    private Vector3 originalBikePosition;
    private float bobbingTimer = 0f;

    [Header("Audio")]
    public AudioSource motorSound;
    public AudioSource jumpSound;
    public AudioSource brakeSound;
    public AudioClip packagePickupSound;
    public AudioClip packageDeliverySound;
    public float minPitch = 0.7f;
    public float maxPitch = 1.5f;
    public float motorVolume = 0.5f;

    [Header("Battery Integration")]
    public bool limitSpeedByBattery = true;
    public float minSpeedMultiplier = 0.3f;
    public float movementBatteryMultiplier = 1.2f;

    [Header("Input Settings")]
    public float inputDeadzone = 0.1f;
    public float inputSensitivity = 2f;

    private BatteryManager batteryManager;
    private Rigidbody rb;

    private float verticalInput;
    private float horizontalInput;
    private bool jumpInput;
    private bool brakeInput;

    void Start()
    {
        batteryManager = GetComponent<BatteryManager>();
        rb = GetComponent<Rigidbody>();

        if (bikeModel != null)
            originalBikePosition = bikeModel.localPosition;

        if (rb != null)
            rb.centerOfMass += Vector3.up * centerOfMassOffset;

        if (dustParticles != null) dustParticles.Stop();

        if (motorSound != null)
        {
            motorSound.volume = motorVolume;
            motorSound.loop = true;
        }
    }

    void Update()
    {
        HandleInput();

        if (CanMove())
        {
            HandleMovement();
            HandleTurning();
            HandleSpecialMoves();
            HandleVisualEffects();
            HandleAudioFeedback();
            HandleParticleEffects();
        }
        else
        {
            StopAllEffects();
        }

        UpdateGroundCheck();
    }

    void FixedUpdate()
    {
        if (CanMove())
        {
            ApplyPhysics();
        }
    }

    void HandleInput()
    {
        float rawVertical = Input.GetAxis("Vertical");
        float rawHorizontal = Input.GetAxis("Horizontal");

        verticalInput = Mathf.Abs(rawVertical) > inputDeadzone ? rawVertical * inputSensitivity : 0f;
        horizontalInput = Mathf.Abs(rawHorizontal) > inputDeadzone ? rawHorizontal * inputSensitivity : 0f;

        jumpInput = Input.GetKeyDown(KeyCode.Space);
        brakeInput = Input.GetKey(KeyCode.LeftControl) || (Input.GetKey(KeyCode.S) && verticalInput < 0);
    }

    bool CanMove()
    {
        return batteryManager != null && batteryManager.currentBattery > 0;
    }

    void HandleMovement()
    {
        targetSpeed = brakeInput ? 0f : verticalInput * maxSpeed;

        if (limitSpeedByBattery && batteryManager != null)
        {
            float batteryRatio = batteryManager.currentBattery / batteryManager.maxBattery;
            targetSpeed *= Mathf.Lerp(minSpeedMultiplier, 1f, batteryRatio);
        }

        float accel = brakeInput ? brakeForce :
            (Mathf.Sign(targetSpeed) == Mathf.Sign(currentSpeed) || Mathf.Abs(currentSpeed) < 0.1f) ?
            acceleration : deceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            transform.Translate(0, 0, currentSpeed * Time.deltaTime);

            if (batteryManager != null && Mathf.Abs(verticalInput) > 0.1f)
            {
                batteryManager.currentBattery -= batteryManager.consumptionRate * movementBatteryMultiplier * Time.deltaTime;
            }
        }
    }

    void HandleTurning()
    {
        if (Mathf.Abs(horizontalInput) > 0.1f && Mathf.Abs(currentSpeed) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float turnAmount = horizontalInput * turnSpeed * (1f + speedFactor * turnSpeedAtSpeed) * Time.deltaTime;
            transform.Rotate(0, turnAmount, 0);
        }
    }

    void HandleSpecialMoves()
    {
        if (jumpInput && isGrounded && rb != null)
        {
            rb.AddForce(Vector3.up * hopForce, ForceMode.Impulse);
            if (jumpSound != null) jumpSound.Play();
            isGrounded = false;
        }

        if (bikeModel != null)
        {
            float wheelieTarget = (verticalInput > wheelieSpeedThreshold && isGrounded) ? wheelieAngle * verticalInput : 0f;
            float currentX = bikeModel.localEulerAngles.x;
            if (currentX > 180f) currentX -= 360f;
            float newX = Mathf.LerpAngle(currentX, wheelieTarget, Time.deltaTime * 3f);
            bikeModel.localRotation = Quaternion.Euler(newX, bikeModel.localEulerAngles.y, bikeModel.localEulerAngles.z);
        }
    }

    void HandleVisualEffects()
    {
        float targetLean = -horizontalInput * leanAngle * Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        float currentZ = transform.eulerAngles.z;
        if (currentZ > 180f) currentZ -= 360f;
        float newZ = Mathf.LerpAngle(currentZ, targetLean, leanSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, newZ);

        if (bikeModel != null && Mathf.Abs(currentSpeed) > 0.5f)
        {
            bobbingTimer += Time.deltaTime * bobbingFrequency * Mathf.Abs(currentSpeed);
            float bobOffset = Mathf.Sin(bobbingTimer) * bobbingAmplitude;
            bikeModel.localPosition = originalBikePosition + Vector3.up * bobOffset;
        }
        else if (bikeModel != null)
        {
            bikeModel.localPosition = Vector3.Lerp(bikeModel.localPosition, originalBikePosition, Time.deltaTime * 5f);
        }
    }

    void HandleAudioFeedback()
    {
        if (motorSound != null)
        {
            if (Mathf.Abs(currentSpeed) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            {
                if (!motorSound.isPlaying) motorSound.Play();

                float speedRatio = Mathf.Abs(currentSpeed) / maxSpeed;
                float inputInfluence = Mathf.Abs(verticalInput) * 0.3f;
                float pitchTarget = Mathf.Lerp(minPitch, maxPitch, speedRatio + inputInfluence);

                motorSound.pitch = Mathf.Lerp(motorSound.pitch, pitchTarget, Time.deltaTime * 5f);
                motorSound.volume = Mathf.Lerp(0.3f, motorVolume, speedRatio + inputInfluence);
            }
            else
            {
                motorSound.volume = Mathf.Lerp(motorSound.volume, 0.1f, Time.deltaTime * 3f);
                if (motorSound.volume < 0.15f) motorSound.Stop();
            }
        }

        if (brakeSound != null && brakeInput && Mathf.Abs(currentSpeed) > 1f)
        {
            if (!brakeSound.isPlaying) brakeSound.Play();
        }
        else if (brakeSound != null && brakeSound.isPlaying)
        {
            brakeSound.Stop();
        }
    }

    void HandleParticleEffects()
    {
        if (dustParticles != null)
        {
            bool shouldPlayDust = Mathf.Abs(currentSpeed) > 2f && Mathf.Abs(horizontalInput) > 0.3f && isGrounded;

            if (shouldPlayDust && !dustParticles.isPlaying)
                dustParticles.Play();
            else if (!shouldPlayDust && dustParticles.isPlaying)
                dustParticles.Stop();
        }
    }

    void ApplyPhysics()
    {
        if (rb == null) return;

        if (isGrounded)
            rb.AddForce(-transform.up * downForce * rb.linearVelocity.magnitude);

        float drag = isGrounded ? groundDrag : airDrag;
        rb.linearVelocity *= drag;

        if (rb.linearVelocity.magnitude > maxSpeed * 1.2f)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed * 1.2f;
    }

    void UpdateGroundCheck()
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance))
            isGrounded = hit.collider.CompareTag("Ground");
        else
            isGrounded = false;
    }

    void StopAllEffects()
    {
        if (motorSound != null) motorSound.Stop();
        if (dustParticles != null) dustParticles.Stop();
        if (brakeSound != null) brakeSound.Stop();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Package") && !hasPackage)
        {
            Destroy(other.gameObject);
            hasPackage = true;

            if (packagePickupSound != null)
                AudioSource.PlayClipAtPoint(packagePickupSound, transform.position);
        }

        if (other.CompareTag("DeliveryPoint") && hasPackage)
        {
            DeliveryManager deliveryManager = FindFirstObjectByType<DeliveryManager>();
            if (deliveryManager != null)
            {
                deliveryManager.TryDeliverPackage(other.transform);
                hasPackage = false;

                if (packageDeliverySound != null)
                    AudioSource.PlayClipAtPoint(packageDeliverySound, transform.position);
            }
        }
    }

    void OnDisable()
    {
        StopAllEffects();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0, 0);
    }

    public void ForceStop()
    {
        currentSpeed = 0f;
        targetSpeed = 0f;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}
