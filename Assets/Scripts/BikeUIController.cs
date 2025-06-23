using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BikeUIController : MonoBehaviour
{
    [Header("Battery Display")]
    public Image batteryFill;
    public Gradient batteryGradient;
    public TextMeshProUGUI batteryPercentText;
    public GameObject lowBatteryWarning;

    [Header("Speed Display")]
    public TextMeshProUGUI speedText;
    public RectTransform speedNeedle;
    public float minNeedleAngle = 90f;
    public float maxNeedleAngle = -90f;

    // Turbo UI removed

    [Header("Status Effects")]
    public GameObject wheelieIndicator;
    public TextMeshProUGUI airTimeText;
    private float airTime = 0f;

    private BikeController bikeController;
    private BatteryManager batteryManager;

    void Start()
    {
        bikeController = FindFirstObjectByType<BikeController>();
        batteryManager = FindFirstObjectByType<BatteryManager>();

        if (lowBatteryWarning) lowBatteryWarning.SetActive(false);
        if (wheelieIndicator) wheelieIndicator.SetActive(false);
        if (airTimeText) airTimeText.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateBatteryUI();
        UpdateSpeedUI();
        UpdateStatusEffects();
    }

    void UpdateBatteryUI()
    {
        if (batteryManager && batteryFill)
        {
            float fillAmount = batteryManager.currentBattery / batteryManager.maxBattery;
            batteryFill.fillAmount = fillAmount;
            batteryFill.color = batteryGradient.Evaluate(fillAmount);

            if (batteryPercentText)
                batteryPercentText.text = Mathf.RoundToInt(fillAmount * 100) + "%";

            if (lowBatteryWarning)
                lowBatteryWarning.SetActive(fillAmount < 0.2f);
        }
    }

    void UpdateSpeedUI()
    {
        if (!bikeController || !speedText || !speedNeedle) return;

        float speedRatio = Mathf.Abs(bikeController.CurrentSpeed) / bikeController.maxSpeed;
        speedText.text = Mathf.RoundToInt(bikeController.SpeedKmh).ToString() + " km/h";

        float needleAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, speedRatio);
        speedNeedle.rotation = Quaternion.Euler(0, 0, needleAngle);
    }

    void UpdateStatusEffects()
    {
        if (!bikeController) return;

        if (wheelieIndicator)
        {
            bool isWheeling = Input.GetAxis("Vertical") > 0.8f && bikeController.IsGrounded;
            wheelieIndicator.SetActive(isWheeling);
        }

        if (airTimeText)
        {
            if (!bikeController.IsGrounded)
            {
                airTime += Time.deltaTime;
                airTimeText.gameObject.SetActive(true);
                airTimeText.text = "AIR: " + airTime.ToString("F1") + "s";
            }
            else
            {
                if (airTime > 1f)
                {
                    Debug.Log("Big air! " + airTime.ToString("F1") + " seconds");
                }
                airTime = 0f;
                airTimeText.gameObject.SetActive(false);
            }
        }
    }
}
