using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Geschwindigkeit des Spielers

    private Rigidbody2D rb;
    private Vector2 movement;
    private bool facingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Eingaben lesen
        movement.x = Input.GetAxisRaw("Horizontal"); // A und D oder Pfeiltasten links/rechts
        movement.y = Input.GetAxisRaw("Vertical");   // W und S oder Pfeiltasten hoch/runter

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
        // Physik-basierte Bewegung
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
    }
}
