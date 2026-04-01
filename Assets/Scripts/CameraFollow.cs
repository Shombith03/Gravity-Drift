using UnityEngine;

/// <summary>
/// Follows the player on the X axis only, keeping Y and Z fixed.
/// Applies screen shake offset when active.
/// Lerps orthographic size (FOV equivalent) from 5 to 5.42 over first 60 seconds.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("The player transform to follow")]
    public Transform target;

    [Tooltip("Offset ahead of the player on X axis")]
    public float lookAheadX = 3f;

    [Tooltip("Smoothing speed for camera movement")]
    public float smoothSpeed = 8f;

    [Header("FOV Lerp (Orthographic Size)")]
    [Tooltip("Starting orthographic size (FOV ~60)")]
    public float startOrthoSize = 5f;
    [Tooltip("Target orthographic size (FOV ~65)")]
    public float endOrthoSize = 5.42f;
    [Tooltip("Time in seconds to reach target size")]
    public float fovLerpDuration = 60f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.orthographicSize = startOrthoSize;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Follow player X
        Vector3 pos = transform.position;
        float desiredX = target.position.x + lookAheadX;
        pos.x = Mathf.Lerp(pos.x, desiredX, smoothSpeed * Time.deltaTime);
        transform.position = pos;

        // Apply screen shake offset
        if (ScreenShake.Instance != null)
        {
            transform.position += ScreenShake.Instance.ShakeOffset;
        }

        // FOV lerp over time
        if (cam != null)
        {
            float t = Mathf.Clamp01(ObstacleSpawner.ElapsedTime / fovLerpDuration);
            cam.orthographicSize = Mathf.Lerp(startOrthoSize, endOrthoSize, t);
        }
    }
}
