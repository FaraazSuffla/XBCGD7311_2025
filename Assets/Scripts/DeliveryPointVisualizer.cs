using UnityEngine;

public class DeliveryPointVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public bool isActiveDeliveryPoint = false;
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.green;
    public float pulseSpeed = 2f;
    public float bobHeight = 0.5f;
    public float bobSpeed = 1f;

    private Vector3 originalPosition;
    private MeshRenderer indicatorRenderer;
    private Material indicatorMaterial;
    private Light spotLight;

    void Start()
    {
        originalPosition = transform.position;
        indicatorRenderer = GetComponentInChildren<MeshRenderer>();
        
        if (indicatorRenderer != null)
        {
            indicatorMaterial = indicatorRenderer.material;
        }
        
        CreateSpotLight();
        UpdateVisualState();
    }

    void CreateSpotLight()
    {
        GameObject lightObj = new GameObject("DeliverySpotLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.up * 5f;
        lightObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        spotLight = lightObj.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = inactiveColor;
        spotLight.intensity = 2f;
        spotLight.range = 10f;
        spotLight.spotAngle = 45f;
        spotLight.enabled = false;
    }

    void Update()
    {
        if (isActiveDeliveryPoint)
        {
            UpdateActiveEffects();
        }
    }

    void UpdateActiveEffects()
    {
        // Bobbing animation
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = originalPosition + Vector3.up * bobOffset;
        
        // Pulsing light
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        if (spotLight != null)
        {
            spotLight.intensity = Mathf.Lerp(1f, 3f, pulse);
        }
    }

    public void SetAsActiveDeliveryPoint(bool isActive)
    {
        isActiveDeliveryPoint = isActive;
        UpdateVisualState();
        
        if (isActive)
        {
            originalPosition = transform.position;
        }
    }

    void UpdateVisualState()
    {
        Color targetColor = isActiveDeliveryPoint ? activeColor : inactiveColor;
        
        if (indicatorMaterial != null)
        {
            indicatorMaterial.color = targetColor;
        }
        
        if (spotLight != null)
        {
            spotLight.color = targetColor;
            spotLight.enabled = isActiveDeliveryPoint;
        }
    }
}