using UnityEngine;
using UnityEngine.InputSystem;

public class BikeController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public float turnSpeed = 50f;
    public float acceleration = 5f;
    public float deceleration = 8f;
    private float currentSpeed = 0f;

    [Header("Visual Effects")]
    public float leanAngle = 15f;
    public float leanSpeed = 5f;
    public ParticleSystem dustParticles;

    [Header("Audio")]
    public AudioSource motorSound;
    public float minPitch = 0.8f;
    public float maxPitch = 1.3f;

    [Header("Battery Limitation")]
    public bool limitSpeedByBattery = true;
    public float minSpeedMultiplier = 0.5f;

    private BatteryManager batteryManager;
    private Rigidbody rb;

    void Start()
    {
        batteryManager = GetComponent<BatteryManager>();
        rb = GetComponent<Rigidbody>();
        
        if (dustParticles != null) dustParticles.Stop();
    }

    void Update()
    {
        if (batteryManager != null && batteryManager.currentBattery > 0)
        {
            HandleMovement();
            HandleLean();
            HandleAudioFeedback();
            HandleParticles();
        }
        else
        {
            if (motorSound != null) motorSound.Stop();
            if (dustParticles != null) dustParticles.Stop();
        }
    }

    void HandleMovement()
    {
        float targetSpeed = Input.GetAxis("Vertical") * speed;
        
        // Apply battery speed limitation
        if (limitSpeedByBattery)
        {
            float batteryEffect = Mathf.Lerp(minSpeedMultiplier, 1f, 
                                           batteryManager.currentBattery / batteryManager.maxBattery);
            targetSpeed *= batteryEffect;
        }

        // Smooth acceleration/deceleration
        if (Mathf.Abs(targetSpeed) > 0.1f)
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);

        // Apply movement
        transform.Translate(0, 0, currentSpeed * Time.deltaTime);

        // Turning (faster turns at higher speed)
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;
        float turnMultiplier = 1 + Mathf.Abs(currentSpeed / speed) * 0.5f;
        transform.Rotate(0, turn * turnMultiplier, 0);
    }

    void HandleLean()
    {
        float targetLeanAngle = -Input.GetAxis("Horizontal") * leanAngle;
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, targetLeanAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, leanSpeed * Time.deltaTime);
    }

    void HandleAudioFeedback()
    {
        if (motorSound != null)
        {
            float speedRatio = Mathf.Abs(currentSpeed / speed);
            motorSound.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
            
            if (!motorSound.isPlaying) motorSound.Play();
        }
    }

    void HandleParticles()
    {
        if (dustParticles != null)
        {
            if (Mathf.Abs(currentSpeed) > 0.5f && Mathf.Abs(Input.GetAxis("Horizontal")) > 0.2f)
            {
                if (!dustParticles.isPlaying) dustParticles.Play();
                var emission = dustParticles.emission;
                emission.rateOverTime = Mathf.Lerp(5f, 30f, Mathf.Abs(currentSpeed / speed));
            }
            else
            {
                if (dustParticles.isPlaying) dustParticles.Stop();
            }
        }
    }

    void OnDisable()
    {
        // Reset rumble if using gamepad
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0, 0);
    }
}