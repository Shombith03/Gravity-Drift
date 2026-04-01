using UnityEngine;

/// <summary>
/// Base class for all obstacle types in Gravity Drift.
/// Handles horizontal movement and object pool lifecycle.
/// </summary>
public abstract class Obstacle : MonoBehaviour
{
    public bool IsActive { get; private set; }

    private float despawnX;

    public virtual void Activate(Vector2 position, float despawnBehindPlayer)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        despawnX = despawnBehindPlayer;
        IsActive = true;
        gameObject.SetActive(true);
    }

    public virtual void Deactivate()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (!IsActive) return;

        float speed = ObstacleSpawner.GameSpeed;
        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

        if (transform.position.x < despawnX)
        {
            Deactivate();
        }
    }
}
