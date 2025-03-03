using UnityEngine;
using UnityEngine.UI;

public class BatteryManager : MonoBehaviour
{
    public float maxBattery = 100f; // Maximum battery capacity
    public float currentBattery; // Current battery level
    public float consumptionRate = 0.5f; // Battery consumption per second
    public float rechargeRate = 5f; // Battery recharge per second
    public bool isCharging = false; // Is the bike charging?

    public Text batteryText; // UI Text to display battery level

    void Start()
    {
        currentBattery = maxBattery; // Start with a full battery
        UpdateBatteryUI();
    }

    void Update()
    {
        if (!isCharging)
        {
            // Consume battery while moving
            if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
            {
                currentBattery -= consumptionRate * Time.deltaTime;
                currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery); // Ensure battery doesn't go below 0
            }
        }
        else
        {
            // Recharge the battery
            currentBattery += rechargeRate * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery); // Ensure battery doesn't exceed max
        }

        // Update the battery UI
        UpdateBatteryUI();

        // Check if the battery is empty
        if (currentBattery <= 0)
        {
            Debug.Log("Battery is dead!");
            // Disable bike movement or trigger a game over
        }
    }

    void UpdateBatteryUI()
    {
        if (batteryText != null)
        {
            batteryText.text = "Battery: " + currentBattery.ToString("F1") + "%";
        }
    }

    public void StartCharging()
    {
        isCharging = true;
    }

    public void StopCharging()
    {
        isCharging = false;
    }
}