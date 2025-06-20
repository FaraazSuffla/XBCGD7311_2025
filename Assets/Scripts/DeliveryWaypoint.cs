using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeliveryWaypoint : MonoBehaviour
{
    [Header("UI References")]
    public Image waypointImage;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI directionText;
    
    [Header("Settings")]
    public float edgeBuffer = 50f;
    public Color onScreenColor = Color.green;
    public Color offScreenColor = Color.red;
    
    private Camera playerCamera;
    private Transform targetDeliveryPoint;
    private Transform playerTransform;
    private RectTransform rectTransform;
    private DeliveryManager deliveryManager;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        playerCamera = Camera.main;
        
        // Find player and delivery manager
        BikeController bike = FindFirstObjectByType<BikeController>();
        if (bike != null) playerTransform = bike.transform;
        
        deliveryManager = FindFirstObjectByType<DeliveryManager>();
        
        // Initially hide the waypoint
        gameObject.SetActive(false);
    }
    
    void Update()
    {
        if (deliveryManager != null && deliveryManager.IsDeliveryActive)
        {
            targetDeliveryPoint = deliveryManager.CurrentDeliveryPoint;
            
            if (targetDeliveryPoint != null && playerTransform != null)
            {
                gameObject.SetActive(true);
                UpdateWaypointPosition();
                UpdateWaypointInfo();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    void UpdateWaypointPosition()
    {
        Vector3 screenPos = playerCamera.WorldToScreenPoint(targetDeliveryPoint.position);
        bool isOnScreen = true;
        
        // Check if target is behind camera
        if (screenPos.z < 0)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
            screenPos.z = 0;
            isOnScreen = false;
        }
        
        // Check if target is outside screen bounds
        if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
        {
            isOnScreen = false;
        }
        
        // Clamp to screen edges
        screenPos.x = Mathf.Clamp(screenPos.x, edgeBuffer, Screen.width - edgeBuffer);
        screenPos.y = Mathf.Clamp(screenPos.y, edgeBuffer, Screen.height - edgeBuffer);
        
        // Set position
        rectTransform.position = screenPos;
        
        // Change color based on visibility
        if (waypointImage != null)
        {
            waypointImage.color = isOnScreen ? onScreenColor : offScreenColor;
        }
    }
    
    void UpdateWaypointInfo()
    {
        if (distanceText != null)
        {
            float distance = Vector3.Distance(playerTransform.position, targetDeliveryPoint.position);
            distanceText.text = distance.ToString("F0") + "m";
        }
        
        if (directionText != null)
        {
            string locationName = targetDeliveryPoint.name.Replace("DeliveryPoint_", "");
            directionText.text = locationName;
        }
    }
}