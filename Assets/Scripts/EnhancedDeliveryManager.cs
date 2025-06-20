using UnityEngine;
using TMPro;

public class EnhancedDeliveryManager : MonoBehaviour
{
    [Header("Delivery Settings")]
    public GameObject packagePrefab;
    public Transform[] deliveryPoints;
    public float baseReward = 50f;
    public float timeLimitPerDelivery = 120f;
    public float timeBonusMultiplier = 2f;
    public float packageSpawnHeight = 2f;

    [Header("UI")]
    public TextMeshProUGUI deliveryInstructions;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI statsText;

    [Header("Audio")]
    public AudioClip deliveryCompleteSound;
    public AudioClip deliveryFailSound;
    public AudioSource audioSource;

    private GameObject currentPackage;
    private Transform currentDeliveryPoint;
    private float currentTimeLeft;
    private bool isDeliveryActive;
    private float currentReward;
    private int deliveriesCompleted = 0;
    private float totalEarnings = 0f;
    private BikeController playerBike;

    void Start()
    {
        InitializeSystem();
    }

    void InitializeSystem()
    {
        playerBike = FindFirstObjectByType<BikeController>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (packagePrefab == null || deliveryPoints.Length == 0)
        {
            Debug.LogError("DeliveryManager: Missing package prefab or delivery points!");
            return;
        }
        
        StartNewDelivery();
    }

    void Update()
    {
        if (isDeliveryActive)
        {
            UpdateTimer();
            UpdateUI();
        }
    }

    public void StartNewDelivery()
    {
        if (currentPackage != null)
            DestroyImmediate(currentPackage);

        Vector3 spawnPos = playerBike != null ? 
            playerBike.transform.position + Vector3.up * packageSpawnHeight : 
            transform.position;
        
        currentPackage = Instantiate(packagePrefab, spawnPos, Quaternion.identity);
        currentPackage.tag = "Package";
        
        currentDeliveryPoint = deliveryPoints[Random.Range(0, deliveryPoints.Length)];
        
        if (deliveryInstructions != null)
            deliveryInstructions.text = "Deliver to: " + currentDeliveryPoint.name.Replace("DeliveryPoint_", "");

        currentTimeLeft = timeLimitPerDelivery;
        currentReward = baseReward;
        isDeliveryActive = true;
    }

    void UpdateTimer()
    {
        currentTimeLeft -= Time.deltaTime;
        
        if (currentTimeLeft <= 0)
            FailDelivery();
    }

    void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimeLeft / 60);
            int seconds = Mathf.FloorToInt(currentTimeLeft % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
            
            timerText.color = currentTimeLeft <= 30f ? Color.red : 
                             currentTimeLeft <= 60f ? Color.yellow : Color.white;
        }
        
        if (rewardText != null)
        {
            float bonus = currentTimeLeft * timeBonusMultiplier;
            rewardText.text = $"Reward: ${currentReward + bonus:F0}";
        }
        
        if (statsText != null)
        {
            statsText.text = $"Completed: {deliveriesCompleted} | Total: ${totalEarnings:F0}";
        }
    }

    public void CompleteDelivery()
    {
        if (!isDeliveryActive) return;

        float timeBonus = currentTimeLeft * timeBonusMultiplier;
        currentReward += timeBonus;
        totalEarnings += currentReward;
        deliveriesCompleted++;

        if (audioSource != null && deliveryCompleteSound != null)
            audioSource.PlayOneShot(deliveryCompleteSound);

        if (rewardText != null)
        {
            rewardText.text = $"Complete! +${currentReward:F0}";
            rewardText.color = Color.green;
        }

        if (currentPackage != null)
            Destroy(currentPackage);

        isDeliveryActive = false;
        Invoke(nameof(StartNewDelivery), 3f);
    }

    void FailDelivery()
    {
        if (!isDeliveryActive) return;

        if (audioSource != null && deliveryFailSound != null)
            audioSource.PlayOneShot(deliveryFailSound);

        if (rewardText != null)
        {
            rewardText.text = "Failed - Too Slow!";
            rewardText.color = Color.red;
        }

        if (currentPackage != null)
            Destroy(currentPackage);

        isDeliveryActive = false;
        Invoke(nameof(StartNewDelivery), 3f);
    }

    public void TryDeliverPackage(Transform deliveryPoint)
    {
        if (isDeliveryActive && deliveryPoint == currentDeliveryPoint)
        {
            if (playerBike != null && playerBike.HasPackage)
            {
                CompleteDelivery();
            }
        }
    }

    public bool IsDeliveryActive => isDeliveryActive;
    public Transform CurrentDeliveryPoint => currentDeliveryPoint;
}