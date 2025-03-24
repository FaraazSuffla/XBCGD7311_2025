using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);
    public float smoothSpeed = 5f;
    public float lookAheadDistance = 2f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleMask;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    void LateUpdate()
    {
        if (target != null)
        {
            // Calculate base position with offset
            Vector3 targetPosition = target.position + offset;
            
            // Add look-ahead based on bike's forward direction
            Vector3 lookAhead = target.forward * lookAheadDistance;
            targetPosition += lookAhead;

            // Handle camera collision
            HandleCameraCollision(ref targetPosition);

            // Smoothly move the camera
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
            
            // Smoothly look at target with slight look-ahead
            Vector3 lookAtPoint = target.position + (lookAhead * 0.5f);
            transform.LookAt(lookAtPoint);
        }
    }

    void HandleCameraCollision(ref Vector3 targetPos)
    {
        RaycastHit hit;
        Vector3 direction = targetPos - target.position;
        
        if (Physics.Raycast(target.position, direction.normalized, out hit, direction.magnitude, obstacleMask))
        {
            targetPos = hit.point - (direction.normalized * 0.5f);
        }
    }
}