using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class BatteryManager : MonoBehaviour
{
    [Header("Battery Settings")]
    public float maxBattery = 100f;
    public float currentBattery;
    public float consumptionRate = 0.5f;
    public float rechargeRate = 5f;
    public bool isCharging = false;

    [Header("UI References")]
    public Image batteryFill;
    public Gradient batteryGradient;
    public TextMeshProUGUI batteryPercentageText;
    public GameObject lowBatteryWarning;
    public float warningFlashInterval = 0.5f;
    private float flashTimer = 0f;

    [Header("Events")]
    public UnityEvent onBatteryDepleted;
    public UnityEvent onBatteryLow;       // <20%
    public UnityEvent onBatteryCritical;  // <5%
    public UnityEvent onBatteryFull;
    public UnityEvent onBatteryChanged;

    [Header("Charging Station Detection")]
    public Transform chargingStationPosition; // Where the player should stand to charge
    public float chargeActivationRange = 3f;   // How close to activate charging
    private bool isInChargingZone = false;

    void Start()
    {
        currentBattery = maxBattery;
        UpdateBatteryUI();
    }

    void Update()
    {
        HandleChargingInput();

        if (!isCharging)
        {
            // Consume battery when moving
            if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
            {
                currentBattery -= consumptionRate * Time.deltaTime;
                currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
                onBatteryChanged.Invoke();
            }
        }
        else
        {
            // Recharge battery
            currentBattery += rechargeRate * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
            onBatteryChanged.Invoke();
        }

        UpdateBatteryUI();
        CheckBatteryEvents();
    }

    void HandleChargingInput()
    {
        if (chargingStationPosition == null)
        {
            Debug.LogWarning("🔋 Charging station position not assigned!");
            return;
        }

        float distanceToStation = Vector3.Distance(transform.position, chargingStationPosition.position);

        if (distanceToStation <= chargeActivationRange)
        {
            if (!isInChargingZone)
            {
                Debug.Log("⚡ Entered charging zone.");
                isInChargingZone = true;
            }

            if (Input.GetKeyDown(KeyCode.E)) // Change or remove input if you auto-start
            {
                StartCharging();
            }
        }
        else
        {
            if (isInChargingZone)
            {
                Debug.Log("🛑 Exited charging zone.");
                isInChargingZone = false;
                StopCharging();
            }
        }
    }

    void UpdateBatteryUI()
    {
        float batteryPercent = currentBattery / maxBattery;

        if (batteryFill != null)
        {
            batteryFill.fillAmount = batteryPercent;
            batteryFill.color = batteryGradient.Evaluate(batteryPercent);
        }

        if (batteryPercentageText != null)
        {
            batteryPercentageText.text = Mathf.RoundToInt(batteryPercent * 100) + "%";
        }

        if (lowBatteryWarning != null)
        {
            if (batteryPercent < 0.2f && !isCharging)
            {
                flashTimer += Time.deltaTime;
                if (flashTimer >= warningFlashInterval)
                {
                    lowBatteryWarning.SetActive(!lowBatteryWarning.activeSelf);
                    flashTimer = 0f;
                }
            }
            else
            {
                lowBatteryWarning.SetActive(false);
                flashTimer = 0f;
            }
        }
    }

    void CheckBatteryEvents()
    {
        float batteryPercent = currentBattery / maxBattery;

        if (currentBattery <= 0)
        {
            onBatteryDepleted.Invoke();
        }
        else if (batteryPercent < 0.05f)
        {
            onBatteryCritical.Invoke();
        }
        else if (batteryPercent < 0.2f)
        {
            onBatteryLow.Invoke();
        }

        if (currentBattery >= maxBattery)
        {
            onBatteryFull.Invoke();
        }
    }

    public void StartCharging()
    {
        isCharging = true;
        Debug.Log("🔋 Charging started.");
    }

    public void StopCharging()
    {
        isCharging = false;
        Debug.Log("🛑 Charging stopped.");
    }

    public void SetBattery(float amount)
    {
        currentBattery = Mathf.Clamp(amount, 0, maxBattery);
        UpdateBatteryUI();
    }
}