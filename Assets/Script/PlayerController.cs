using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 input;
    private Vector2 lastMove = Vector2.down; // default facing down

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
    }

    void Update()
    {
        // Get input
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        if (input != Vector2.zero)
        {
            lastMove = input.normalized;
        }

        // Send to Animator
        if (input.magnitude > 0.1f)
        {
            if (input.x != 0) // moving left/right
            {
                animator.SetFloat("InputX", -1); // always tell Animator: "Side = -1"
                animator.SetFloat("InputY", 0);
            }
            else // moving up/down
            {
                animator.SetFloat("InputX", 0);
                animator.SetFloat("InputY", input.y > 0 ? 1 : -1);
            }
        }
        else
        {
            // Idle uses lastMove
            if (Mathf.Abs(lastMove.x) > 0.1f)
            {
                animator.SetFloat("InputX",input.x); // idle side always left
                animator.SetFloat("InputY", 0);
            }
            else
            {
                animator.SetFloat("InputX", 0);
                animator.SetFloat("InputY", lastMove.y > 0 ? 1 : -1);
            }
        }

        animator.SetFloat("Speed", input.magnitude);

        // Flip sprite
        if (input.x < 0 || (input == Vector2.zero && lastMove.x < 0))
            spriteRenderer.flipX = false;
        else if (input.x > 0 || (input == Vector2.zero && lastMove.x > 0))
            spriteRenderer.flipX = true;
    }

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(input.x, 0f, input.y).normalized * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
}
