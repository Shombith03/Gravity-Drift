using UnityEngine;

/// <summary>
/// Creates a simple tunnel with two EdgeCollider2D walls (floor and ceiling) 5 units apart.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class TunnelSetup : MonoBehaviour
{
    [Tooltip("Half-height of the tunnel (distance from center to each wall)")]
    public float tunnelHalfHeight = 2.5f;

    [Tooltip("Length of the tunnel segment")]
    public float tunnelLength = 200f;

    private void Awake()
    {
        CreateWall("Floor", -tunnelHalfHeight);
        CreateWall("Ceiling", tunnelHalfHeight);
    }

    private void CreateWall(string wallName, float yPosition)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform);
        wall.transform.localPosition = new Vector3(0f, yPosition, 0f);
        wall.layer = gameObject.layer;

        EdgeCollider2D edge = wall.AddComponent<EdgeCollider2D>();
        edge.points = new Vector2[]
        {
            new Vector2(-tunnelLength * 0.5f, 0f),
            new Vector2(tunnelLength * 0.5f, 0f)
        };
    }
}
