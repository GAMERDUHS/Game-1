using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Geschwindigkeit des Spielers

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Eingaben lesen
        movement.x = Input.GetAxisRaw("Horizontal"); // A und D oder Pfeiltasten links/rechts
        movement.y = Input.GetAxisRaw("Vertical");   // W und S oder Pfeiltasten hoch/runter
    }

    void FixedUpdate()
    {
        // Physik-basierte Bewegung
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}