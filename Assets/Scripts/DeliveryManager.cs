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
    private DeliveryPointVisualizer[] allVisualizers;

    // Public properties for other scripts to access
    public bool IsDeliveryActive => isDeliveryActive;
    public Transform CurrentDeliveryPoint => currentDeliveryPoint;
    public float TimeRemaining => currentTimeLeft;

    void Start()
    {
        InitializeVisualizers();
        StartNewDelivery();
    }

    void InitializeVisualizers()
    {
        // Get all delivery point visualizers
        allVisualizers = new DeliveryPointVisualizer[deliveryPoints.Length];
        for (int i = 0; i < deliveryPoints.Length; i++)
        {
            if (deliveryPoints[i] != null)
            {
                allVisualizers[i] = deliveryPoints[i].GetComponent<DeliveryPointVisualizer>();
            }
        }
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
        // Clean up previous package
        if (currentPackage != null)
        {
            Destroy(currentPackage);
        }

        // Deactivate all delivery points
        DeactivateAllDeliveryPoints();

        // Spawn package at a random point (or bike's location)
        BikeController playerBike = FindFirstObjectByType<BikeController>();
        Vector3 spawnPosition = transform.position;
        if (playerBike != null)
        {
            spawnPosition = playerBike.transform.position + Vector3.up * 2f;
        }
        
        currentPackage = Instantiate(packagePrefab, spawnPosition, Quaternion.identity);
        
        // Choose a random delivery point
        int randomIndex = Random.Range(0, deliveryPoints.Length);
        currentDeliveryPoint = deliveryPoints[randomIndex];
        
        // Activate the selected delivery point
        if (allVisualizers[randomIndex] != null)
        {
            allVisualizers[randomIndex].SetAsActiveDeliveryPoint(true);
        }
        
        // Update UI
        if (deliveryInstructions != null)
        {
            string locationName = currentDeliveryPoint.name.Replace("DeliveryPoint_", "");
            deliveryInstructions.text = "Deliver to: " + locationName;
        }

        // Reset timer and reward
        currentTimeLeft = timeLimitPerDelivery;
        currentReward = baseReward;
        isDeliveryActive = true;

        Debug.Log($"New delivery started: {currentDeliveryPoint.name}");
    }

    void DeactivateAllDeliveryPoints()
    {
        foreach (var visualizer in allVisualizers)
        {
            if (visualizer != null)
            {
                visualizer.SetAsActiveDeliveryPoint(false);
            }
        }
    }

    void UpdateTimer()
    {
        currentTimeLeft -= Time.deltaTime;
        
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimeLeft / 60);
            int seconds = Mathf.FloorToInt(currentTimeLeft % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }

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
        if (rewardText != null)
        {
            rewardText.text = "Reward: $" + currentReward.ToString("F0");
            rewardText.color = Color.green;
        }

        // Deactivate current delivery point
        DeactivateAllDeliveryPoints();

        // Clean up
        if (currentPackage != null)
        {
            Destroy(currentPackage);
        }

        Debug.Log($"Delivery completed! Reward: ${currentReward:F0}");

        // Start next delivery after a delay
        Invoke("StartNewDelivery", 2f);
        isDeliveryActive = false;
    }

    void FailDelivery()
    {
        Debug.Log("Delivery failed! Too slow.");
        
        // Update UI
        if (rewardText != null)
        {
            rewardText.text = "Delivery Failed!";
            rewardText.color = Color.red;
        }

        // Deactivate delivery points
        DeactivateAllDeliveryPoints();

        // Clean up
        if (currentPackage != null)
        {
            Destroy(currentPackage);
        }

        Invoke("StartNewDelivery", 2f);
        isDeliveryActive = false;
    }

    // Call this when the bike collides with the delivery point
    public void TryDeliverPackage(Transform deliveryPoint)
    {
        if (isDeliveryActive && deliveryPoint == currentDeliveryPoint)
        {
            // Check if player has package
            BikeController playerBike = FindFirstObjectByType<BikeController>();
            if (playerBike != null && playerBike.HasPackage)
            {
                CompleteDelivery();
            }
            else
            {
                Debug.Log("Cannot deliver - no package picked up!");
            }
        }
    }
}