using UnityEngine;

public class SmoothCameraFollow2D : MonoBehaviour
{
    public Transform target;        // The target for the camera to follow
    public float smoothSpeed = 0.125f; // The speed of the smooth transition
    public Vector3 offset;          // The offset from the target position

    void FixedUpdate()
    {
        if (target != null)
        {
            // Desired position with offset
            Vector3 desiredPosition = target.position + offset;

            // Smoothly interpolate between the current position and the desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // Apply the smoothed position to the camera
            transform.position = smoothedPosition;

            // Ensure the camera stays at the same z-axis position (for 2D perspective)
            transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
        }
    }
}
