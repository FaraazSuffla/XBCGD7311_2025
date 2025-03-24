using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BatteryManager : MonoBehaviour
{
    [Header("Battery Settings")]
    public float maxBattery = 100f;
    public float currentBattery;
    public float consumptionRate = 0.5f;
    public float rechargeRate = 5f;
    public bool isCharging = false;

    [Header("UI")]
    public Text batteryText;
    public Image batteryFill;
    public Color fullColor = Color.green;
    public Color emptyColor = Color.red;

    [Header("Events")]
    public UnityEvent onBatteryDepleted;
    public UnityEvent onBatteryFull;

    void Start()
    {
        currentBattery = maxBattery;
        UpdateBatteryUI();
    }

    void Update()
    {
        if (!isCharging)
        {
            if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
            {
                currentBattery -= consumptionRate * Time.deltaTime;
                currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
            }
        }
        else
        {
            currentBattery += rechargeRate * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
            
            if (currentBattery >= maxBattery) onBatteryFull.Invoke();
        }

        UpdateBatteryUI();

        if (currentBattery <= 0) onBatteryDepleted.Invoke();
    }

    void UpdateBatteryUI()
    {
        if (batteryText != null)
            batteryText.text = "Battery: " + currentBattery.ToString("F1") + "%";
        
        if (batteryFill != null)
        {
            float fillAmount = currentBattery / maxBattery;
            batteryFill.fillAmount = fillAmount;
            batteryFill.color = Color.Lerp(emptyColor, fullColor, fillAmount);
        }
    }

    public void StartCharging() => isCharging = true;
    public void StopCharging() => isCharging = false;
}