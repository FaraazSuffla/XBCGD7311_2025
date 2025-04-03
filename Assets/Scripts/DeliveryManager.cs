using UnityEngine;
using TMPro;

public class DeliveryManager : MonoBehaviour
{
    [Header("Delivery Settings")]
    public GameObject packagePrefab;
    public Transform[] deliveryPoints; // Assign in Inspector
    public float baseReward = 50f;
    public float timeLimitPerDelivery = 120f;
    public float timeBonusMultiplier = 2f;

    [Header("UI")]
    public TextMeshProUGUI deliveryInstructions;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI rewardText;

    private GameObject currentPackage;
    private Transform currentDeliveryPoint;
    private float currentTimeLeft;
    private bool isDeliveryActive;
    private float currentReward;

    void Start()
    {
        StartNewDelivery();
    }

    void Update()
    {
        if (isDeliveryActive)
        {
            UpdateTimer();
        }
    }

    public void StartNewDelivery()
    {
        // Spawn package at a random point (or bike's location)
        currentPackage = Instantiate(packagePrefab, transform.position, Quaternion.identity);
        
        // Choose a random delivery point
        currentDeliveryPoint = deliveryPoints[Random.Range(0, deliveryPoints.Length)];
        deliveryInstructions.text = "Deliver to: " + currentDeliveryPoint.name;

        // Reset timer and reward
        currentTimeLeft = timeLimitPerDelivery;
        currentReward = baseReward;
        isDeliveryActive = true;
    }

    void UpdateTimer()
    {
        currentTimeLeft -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.RoundToInt(currentTimeLeft).ToString() + "s";

        if (currentTimeLeft <= 0)
        {
            FailDelivery();
        }
    }

    public void CompleteDelivery()
    {
        // Calculate bonus for fast delivery
        float timeBonus = currentTimeLeft * timeBonusMultiplier;
        currentReward += timeBonus;

        // Update UI
        rewardText.text = "Reward: $" + currentReward.ToString("F0");
        Destroy(currentPackage);

        // Start next delivery after a delay
        Invoke("StartNewDelivery", 2f);
        isDeliveryActive = false;
    }

    void FailDelivery()
    {
        Debug.Log("Delivery failed! Too slow.");
        Destroy(currentPackage);
        Invoke("StartNewDelivery", 2f);
        isDeliveryActive = false;
    }

    // Call this when the bike collides with the delivery point
    public void TryDeliverPackage(Transform deliveryPoint)
    {
        if (isDeliveryActive && deliveryPoint == currentDeliveryPoint)
        {
            CompleteDelivery();
        }
    }
}