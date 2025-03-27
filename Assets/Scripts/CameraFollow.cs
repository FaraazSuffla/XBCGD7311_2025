using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 2f, -5f); // Adjusted for better third-person view
    public float distance = 5f; // Default follow distance
    public float height = 2f; // Camera height relative to target
    public float smoothSpeed = 5f;
    public float rotationSmoothSpeed = 10f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleMask;
    public float minDistance = 1f;
    public float maxDistance = 10f;
    public float collisionOffset = 0.5f; // Push camera this much away from obstacles

    [Header("Advanced Settings")]
    public float lookAheadDistance = 2f;
    public float verticalAngleLimit = 30f; // Max up/down angle
    public float horizontalAngleLimit = 45f; // Max left/right angle
    public bool followBikeRotation = true; // Should camera rotate with bike?

    private Vector3 currentVelocity;
    private float currentDistance;

    void Start()
    {
        currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate desired position behind the bike
        Vector3 desiredPosition = CalculateDesiredPosition();
        
        // Handle camera collision
        desiredPosition = HandleCameraCollision(desiredPosition);

        // Smoothly move the camera
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothSpeed * Time.deltaTime);

        // Calculate look direction with slight look-ahead
        Vector3 lookAtPoint = target.position + (target.forward * lookAheadDistance * 0.5f);
        
        // Smoothly rotate camera to look at target
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    Vector3 CalculateDesiredPosition()
    {
        // Base position behind the bike
        Vector3 basePosition = target.position - (target.forward * distance);
        
        // Add height offset
        basePosition += Vector3.up * height;
        
        // If following bike rotation, use bike's forward direction
        if (followBikeRotation)
        {
            return basePosition;
        }
        
        // Otherwise maintain world-relative position with angle limits
        Vector3 direction = (basePosition - target.position).normalized;
        float currentAngle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
        currentAngle = Mathf.Clamp(currentAngle, -horizontalAngleLimit, horizontalAngleLimit);
        
        float verticalAngle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.right);
        verticalAngle = Mathf.Clamp(verticalAngle, -verticalAngleLimit, verticalAngleLimit);
        
        Quaternion rotation = Quaternion.Euler(verticalAngle, currentAngle, 0);
        return target.position + (rotation * Vector3.forward * -distance) + Vector3.up * height;
    }

    Vector3 HandleCameraCollision(Vector3 desiredPosition)
    {
        RaycastHit hit;
        Vector3 direction = desiredPosition - target.position;
        float distanceToTarget = direction.magnitude;

        if (Physics.Raycast(target.position, direction.normalized, out hit, distanceToTarget, obstacleMask))
        {
            // If we hit something, adjust the camera position
            float adjustedDistance = hit.distance - collisionOffset;
            adjustedDistance = Mathf.Clamp(adjustedDistance, minDistance, maxDistance);
            
            // Recalculate position with adjusted distance
            desiredPosition = target.position + (direction.normalized * adjustedDistance);
        }
        else
        {
            // No collision - use full distance
            currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * smoothSpeed);
            desiredPosition = target.position + (direction.normalized * currentDistance);
        }

        return desiredPosition;
    }

    // Helper method to adjust camera distance (could be called from other scripts)
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }
}