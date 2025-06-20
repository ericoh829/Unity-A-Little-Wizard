using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;        // Reference to the player object
    public Vector3 offset;          // Offset for the camera position
    public float smoothTime = 0.2f; // Higher values = more lag
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        Vector3 desiredPosition = player.position + offset;
        // SmoothDamp calculates a smooth transition over time, creating a lagging/momentum effect
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
