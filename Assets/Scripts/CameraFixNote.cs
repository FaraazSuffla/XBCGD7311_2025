using UnityEngine;

// This is a fixed version of the HandleCameraRotation method
// Copy this method into your CameraFollow script to fix the compilation error

/*
Replace the HandleCameraRotation method with this:

void HandleCameraRotation()
{
    if (isManualControl)
    {
        // During manual control, always look at the target
        Vector3 manualLookDirection = target.position - transform.position;
        if (manualLookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(manualLookDirection);
        }
        return;
    }

    // Auto follow - look at target with slight look-ahead
    Vector3 lookAtPoint = target.position;
    if (bikeController != null)
    {
        // Add look-ahead based on bike's speed and direction
        Vector3 targetVelocityVector = target.forward * (bikeController.CurrentSpeed * lookAheadDistance * 0.5f);
        lookAtPoint += targetVelocityVector;
    }
    
    // Smoothly rotate camera to look at target
    Vector3 autoLookDirection = lookAtPoint - transform.position;
    if (autoLookDirection != Vector3.zero)
    {
        Quaternion targetRotation = Quaternion.LookRotation(autoLookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
*/

public class CameraFixNote : MonoBehaviour
{
    // This script contains the fix instructions above
}