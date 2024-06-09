using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 5f; // Player's speed

    private Rigidbody2D rb;
    private Vector2 movement;
    private bool facingRight = true;
    public Vector3 spawnOffset = new Vector3(0, 1, 0); // Initial offset

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Read input
        movement.x = Input.GetAxisRaw("Horizontal"); // A and D or arrow keys left/right
        movement.y = Input.GetAxisRaw("Vertical");   // W and S or arrow keys up/down

        // Check direction and flip sprite if necessary
        if (movement.x > 0 && !facingRight)
        {
            Flip();
        }
        else if (movement.x < 0 && facingRight)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        // Physics-based movement
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    void Flip()
    {
        // Toggle the value of facingRight
        facingRight = !facingRight;

        // Multiply the player's x local scale by -1 to flip the sprite
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;

        // Flip the offset by multiplying the x value by -1
        spawnOffset.x *= -1;

        // If there is a currently spawned prefab, update its position
        if (Uimanager.Instance != null && Uimanager.Instance.CurrentSpawnedPrefab != null)
        {
            Uimanager.Instance.CurrentSpawnedPrefab.transform.position = transform.position + spawnOffset;
        }
    }
}
