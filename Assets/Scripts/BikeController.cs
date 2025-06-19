using UnityEngine;
using UnityEngine.InputSystem;

public class BikeController : MonoBehaviour
{
    // Public properties for other scripts to access
    public float CurrentSpeed => currentSpeed;
    public float TurboTimeRemaining => turboTimeRemaining;
    public bool IsGrounded => isGrounded;
    public bool IsWheeling => bikeModel != null && bikeModel.localEulerAngles.x > 5f;
    public bool HasPackage => hasPackage;
    public float SpeedKmh => Mathf.Abs(currentSpeed) * 3.6f; // Convert m/s to km/h

    private bool hasPackage;

    [Header("Movement Settings")]
    public float maxSpeed = 15f;
    public float acceleration = 8f;
    public float deceleration = 12f;
    public float brakeForce = 20f;
    public float turnSpeed = 80f;
    public float turnSpeedAtSpeed = 1.5f; // Multiplier for turning at high speed
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;

    [Header("Physics Settings")]
    public float downForce = 100f; // Keep bike grounded
    public float centerOfMassOffset = -0.5f; // Lower center of mass for stability
    public float traction = 1f; // Grip on surfaces
    public float airDrag = 0.98f; // Air resistance
    public float groundDrag = 0.95f; // Ground friction

    [Header("Advanced Features")]
    public float hopForce = 8f;
    public float turboBoost = 2f;
    public float turboDuration = 3f;
    public float turboCooldown = 8f;
    public float wheelieAngle = 20f;
    public float wheelieSpeedThreshold = 0.3f;
    private float turboTimeRemaining = 0f;
    private float turboCooldownRemaining = 0f;
    private bool isGrounded = true;
    private float groundCheckDistance = 1.2f;

    [Header("Visual Effects")]
    public float leanAngle = 20f;
    public float leanSpeed = 8f;
    public float bobbingAmplitude = 0.1f;
    public float bobbingFrequency = 2f;
    public ParticleSystem dustParticles;
    public ParticleSystem turboParticles;
    public Transform bikeModel;
    private Vector3 originalBikePosition;
    private float bobbingTimer = 0f;

    [Header("Audio")]
    public AudioSource motorSound;
    public AudioSource jumpSound;
    public AudioSource turboSound;
    public AudioSource brakeSound;
    public AudioClip packagePickupSound;
    public AudioClip packageDeliverySound;
    public float minPitch = 0.7f;
    public float maxPitch = 1.5f;
    public float motorVolume = 0.5f;

    [Header("Battery Integration")]
    public bool limitSpeedByBattery = true;
    public float minSpeedMultiplier = 0.3f;
    public float turboBatteryCost = 25f;
    public float movementBatteryMultiplier = 1.2f; // Extra battery drain during movement

    [Header("Input Settings")]
    public float inputDeadzone = 0.1f;
    public float inputSensitivity = 2f;

    // Components
    private BatteryManager batteryManager;
    private Rigidbody rb;

    // Input values
    private float verticalInput;
    private float horizontalInput;
    private bool jumpInput;
    private bool turboInput;
    private bool brakeInput;

    void Start()
    {
        batteryManager = GetComponent<BatteryManager>();
        rb = GetComponent<Rigidbody>();

        // Store original bike model position for bobbing effect
        if (bikeModel != null)
            originalBikePosition = bikeModel.localPosition;

        // Adjust center of mass for better stability
        if (rb != null)
            rb.centerOfMass += Vector3.up * centerOfMassOffset;

        // Initialize particle systems
        if (dustParticles != null) dustParticles.Stop();
        if (turboParticles != null) turboParticles.Stop();

        // Initialize audio
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
            // Stop all effects when battery is dead
            StopAllEffects();
        }

        UpdateGroundCheck();
        UpdateCooldowns();
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
        // Get input with deadzone
        float rawVertical = Input.GetAxis("Vertical");
        float rawHorizontal = Input.GetAxis("Horizontal");

        verticalInput = Mathf.Abs(rawVertical) > inputDeadzone ? rawVertical * inputSensitivity : 0f;
        horizontalInput = Mathf.Abs(rawHorizontal) > inputDeadzone ? rawHorizontal * inputSensitivity : 0f;

        jumpInput = Input.GetKeyDown(KeyCode.Space);
        turboInput = Input.GetKeyDown(KeyCode.LeftShift);
        brakeInput = Input.GetKey(KeyCode.LeftControl) || (Input.GetKey(KeyCode.S) && verticalInput < 0);
    }

    bool CanMove()
    {
        return batteryManager != null && batteryManager.currentBattery > 0;
    }

    void HandleMovement()
    {
        // Calculate target speed based on input
        if (brakeInput)
        {
            targetSpeed = 0f;
        }
        else
        {
            targetSpeed = verticalInput * maxSpeed;
        }

        // Apply turbo boost
        if (turboTimeRemaining > 0)
        {
            targetSpeed *= turboBoost;
        }

        // Apply battery speed limitation
        if (limitSpeedByBattery && batteryManager != null)
        {
            float batteryRatio = batteryManager.currentBattery / batteryManager.maxBattery;
            float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, batteryRatio);
            targetSpeed *= speedMultiplier;
        }

        // Smooth acceleration/deceleration
        float accelerationRate = brakeInput ? brakeForce :
                                (Mathf.Sign(targetSpeed) == Mathf.Sign(currentSpeed) || Mathf.Abs(currentSpeed) < 0.1f) ?
                                acceleration : deceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);

        // Apply movement
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            transform.Translate(0, 0, currentSpeed * Time.deltaTime);

            // Extra battery drain when moving
            if (batteryManager != null && Mathf.Abs(verticalInput) > 0.1f)
            {
                float extraDrain = batteryManager.consumptionRate * movementBatteryMultiplier * Time.deltaTime;
                batteryManager.currentBattery -= extraDrain;
            }
        }
    }

    void HandleTurning()
    {
        if (Mathf.Abs(horizontalInput) > 0.1f && Mathf.Abs(currentSpeed) > 0.1f)
        {
            // Speed-dependent turning
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float turnMultiplier = 1f + (speedFactor * turnSpeedAtSpeed);

            float turnAmount = horizontalInput * turnSpeed * turnMultiplier * Time.deltaTime;
            transform.Rotate(0, turnAmount, 0);
        }
    }

    void HandleSpecialMoves()
    {
        // Jump/Hop
        if (jumpInput && isGrounded && rb != null)
        {
            rb.AddForce(Vector3.up * hopForce, ForceMode.Impulse);
            if (jumpSound != null) jumpSound.Play();
            isGrounded = false;
        }

        // Turbo Boost
        if (turboInput && turboCooldownRemaining <= 0 && batteryManager != null &&
            batteryManager.currentBattery >= turboBatteryCost)
        {
            ActivateTurbo();
        }

        // Wheelie effect
        if (bikeModel != null)
        {
            float wheelieTarget = 0f;
            if (verticalInput > wheelieSpeedThreshold && isGrounded)
            {
                wheelieTarget = wheelieAngle * verticalInput;
            }

            float currentX = bikeModel.localEulerAngles.x;
            if (currentX > 180f) currentX -= 360f; // Normalize angle

            float newX = Mathf.LerpAngle(currentX, wheelieTarget, Time.deltaTime * 3f);
            bikeModel.localRotation = Quaternion.Euler(newX, bikeModel.localEulerAngles.y, bikeModel.localEulerAngles.z);
        }
    }

    void ActivateTurbo()
    {
        turboTimeRemaining = turboDuration;
        turboCooldownRemaining = turboCooldown;
        batteryManager.currentBattery -= turboBatteryCost;

        if (turboSound != null) turboSound.Play();
        if (turboParticles != null) turboParticles.Play();
    }

    void HandleVisualEffects()
    {
        // Leaning during turns
        float targetLeanAngle = -horizontalInput * leanAngle * Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        Vector3 currentEuler = transform.eulerAngles;
        float newZ = Mathf.LerpAngle(currentEuler.z > 180f ? currentEuler.z - 360f : currentEuler.z,
                                    targetLeanAngle, leanSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, newZ);

        // Bobbing effect when moving
        if (bikeModel != null && Mathf.Abs(currentSpeed) > 0.5f)
        {
            bobbingTimer += Time.deltaTime * bobbingFrequency * Mathf.Abs(currentSpeed);
            float bobOffset = Mathf.Sin(bobbingTimer) * bobbingAmplitude * Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
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

        // Brake sound
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
        // Dust particles when turning at speed
        if (dustParticles != null)
        {
            bool shouldPlayDust = Mathf.Abs(currentSpeed) > 2f && Mathf.Abs(horizontalInput) > 0.3f && isGrounded;

            if (shouldPlayDust && !dustParticles.isPlaying)
            {
                dustParticles.Play();
            }
            else if (!shouldPlayDust && dustParticles.isPlaying)
            {
                dustParticles.Stop();
            }

            if (dustParticles.isPlaying)
            {
                var emission = dustParticles.emission;
                float intensity = Mathf.Abs(currentSpeed) / maxSpeed * Mathf.Abs(horizontalInput);
                emission.rateOverTime = Mathf.Lerp(5f, 50f, intensity);
            }
        }

        // Turbo particles
        if (turboParticles != null)
        {
            if (turboTimeRemaining > 0 && !turboParticles.isPlaying)
            {
                turboParticles.Play();
            }
            else if (turboTimeRemaining <= 0 && turboParticles.isPlaying)
            {
                turboParticles.Stop();
            }
        }
    }

    void ApplyPhysics()
    {
        if (rb == null) return;

        // Apply downforce for stability
        if (isGrounded)
        {
            rb.AddForce(-transform.up * downForce * rb.linearVelocity.magnitude);
        }

        // Apply drag
        float dragValue = isGrounded ? groundDrag : airDrag;
        rb.linearVelocity *= dragValue;

        // Limit maximum velocity
        if (rb.linearVelocity.magnitude > maxSpeed * 1.2f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed * 1.2f;
        }
    }

    void UpdateGroundCheck()
    {
        // More accurate ground checking
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance))
        {
            if (!isGrounded && hit.collider.CompareTag("Ground"))
            {
                isGrounded = true;
                // Landing effect could be added here
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    void UpdateCooldowns()
    {
        if (turboTimeRemaining > 0)
        {
            turboTimeRemaining -= Time.deltaTime;
        }

        if (turboCooldownRemaining > 0)
        {
            turboCooldownRemaining -= Time.deltaTime;
        }
    }

    void StopAllEffects()
    {
        if (motorSound != null) motorSound.Stop();
        if (dustParticles != null) dustParticles.Stop();
        if (turboParticles != null) turboParticles.Stop();
        if (brakeSound != null) brakeSound.Stop();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Pick up package
        if (other.CompareTag("Package") && !hasPackage)
        {
            Destroy(other.gameObject);
            hasPackage = true;

            if (packagePickupSound != null && motorSound != null)
                AudioSource.PlayClipAtPoint(packagePickupSound, transform.position);

            Debug.Log("Package picked up!");
        }

        // Deliver package
        if (other.CompareTag("DeliveryPoint") && hasPackage)
        {
            DeliveryManager deliveryManager = FindFirstObjectByType<DeliveryManager>();
            if (deliveryManager != null)
            {
                deliveryManager.TryDeliverPackage(other.transform);
                hasPackage = false;

                if (packageDeliverySound != null && motorSound != null)
                    AudioSource.PlayClipAtPoint(packageDeliverySound, transform.position);
            }
        }
    }

    void OnDisable()
    {
        StopAllEffects();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0, 0);
    }

    // Public methods for external scripts
    public bool CanUseTurbo()
    {
        return turboCooldownRemaining <= 0 && batteryManager != null &&
               batteryManager.currentBattery >= turboBatteryCost;
    }

    public float GetTurboCooldownProgress()
    {
        return 1f - (turboCooldownRemaining / turboCooldown);
    }

    public void ForceStop()
    {
        currentSpeed = 0f;
        targetSpeed = 0f;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}