using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects when the player passes within a threshold distance of an obstacle
/// without colliding. Awards near-miss bonus points and triggers screen shake.
/// Attach to the Player GameObject.
/// </summary>
public class NearMissDetector : MonoBehaviour
{
    [Tooltip("Distance threshold for a near-miss")]
    public float nearMissThreshold = 0.3f;

    private HashSet<int> scoredObstacles = new HashSet<int>();
    private Collider2D playerCollider;

    private void Start()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        CheckNearMisses();
    }

    private void CheckNearMisses()
    {
        // Find all obstacle colliders within near-miss range
        float checkRadius = nearMissThreshold + 1.5f; // search radius includes obstacle half-size
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, checkRadius);

        for (int i = 0; i < nearby.Length; i++)
        {
            Collider2D col = nearby[i];
            if (col == playerCollider) continue;
            if (col.isTrigger) continue; // skip crystal triggers

            Obstacle obstacle = col.GetComponentInParent<Obstacle>();
            if (obstacle == null || !obstacle.IsActive) continue;

            int obstacleId = obstacle.GetInstanceID();
            if (scoredObstacles.Contains(obstacleId)) continue;

            float distance = GetClosestDistance(col);
            if (distance > 0f && distance <= nearMissThreshold)
            {
                scoredObstacles.Add(obstacleId);
                GameManager.Instance.RegisterNearMiss();
                ScreenShake.Instance?.ShakeNearMiss();
                AudioManager.Instance?.PlayNearMissThud();
            }
        }

        // Clean up scored obstacles that are no longer active
        if (scoredObstacles.Count > 50)
        {
            scoredObstacles.Clear();
        }
    }

    private float GetClosestDistance(Collider2D otherCollider)
    {
        Vector2 playerPos = transform.position;
        Vector2 closestPoint = otherCollider.ClosestPoint(playerPos);
        Vector2 playerClosest = playerCollider.ClosestPoint(closestPoint);
        return Vector2.Distance(playerClosest, closestPoint);
    }
}
