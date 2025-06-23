using UnityEngine;

public class ChargingStation : MonoBehaviour
{
    public float chargeRange = 3f;
    public BatteryManager batteryManager; // Assign in Inspector

    void Update()
    {
        if (batteryManager == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, Camera.main.transform.position);

        if (distanceToPlayer <= chargeRange)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("🔋 Charging started!");
                batteryManager.StartCharging();
            }
        }
        else
        {
            batteryManager.StopCharging();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chargeRange);
    }
}