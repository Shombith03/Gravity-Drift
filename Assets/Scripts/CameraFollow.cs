using UnityEngine;

/// <summary>
/// Follows the player on the X axis only, keeping Y and Z fixed.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("The player transform to follow")]
    public Transform target;

    [Tooltip("Offset ahead of the player on X axis")]
    public float lookAheadX = 3f;

    [Tooltip("Smoothing speed for camera movement")]
    public float smoothSpeed = 8f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        float desiredX = target.position.x + lookAheadX;
        pos.x = Mathf.Lerp(pos.x, desiredX, smoothSpeed * Time.deltaTime);
        transform.position = pos;
    }
}
