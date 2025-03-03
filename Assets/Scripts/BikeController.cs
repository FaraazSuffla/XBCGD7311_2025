using UnityEngine;

public class BikeController : MonoBehaviour
{
    public float speed = 10f;
    public float turnSpeed = 50f;
    private BatteryManager batteryManager;

    void Start()
    {
        batteryManager = GetComponent<BatteryManager>();
    }

    void Update()
    {
        if (batteryManager != null && batteryManager.currentBattery > 0)
        {
            // Move forward/backward
            float move = Input.GetAxis("Vertical") * speed * Time.deltaTime;
            transform.Translate(0, 0, move);

            // Rotate left/right
            float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;
            transform.Rotate(0, turn, 0);
        }
        else
        {
            Debug.Log("Bike is out of battery!");
        }
    }
}