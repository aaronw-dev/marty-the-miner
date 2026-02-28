using UnityEngine;
using UnityEngine.Input;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerController2D : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float jumpHeight = 2.5f;
    public float timeToApex;
    public float acceleration = 40f;
    public float deceleration = 60f;
    public Transform groundCheck;
    public float groundCheckRadius;
    public LayerMask groundLayer;
    public SpriteRenderer spriteRenderer;
    private Animator anim;
    private Rigidbody2D rb;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float gravity;
    private float jumpVelocity;
    private bool isGrounded;
    private bool jumpPressed, jumpHeld;
    private float horizontalInput;
    private InputActions actions;
    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        gravity = (2 * jumpHeight) / Mathf.Pow(timeToApex, 2);
        jumpVelocity = gravity * timeToApex;
        Debug.Log($"Calculated gravity: {gravity}");
        Debug.Log($"Calculated jump velocity: {jumpVelocity}");
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (anim)
        {
            anim.SetBool("isGrounded", isGrounded);
            anim.SetBool("isWalking", horizontalInput != 0);
        }
        if (spriteRenderer)
            spriteRenderer.flipX = rb.linearVelocityX < 0;
        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
            if (isGrounded)
            {
                if (anim)
                    anim.SetTrigger("jump");
                rb.linearVelocityY = jumpVelocity;
            }
        }

        jumpHeld = Input.GetButton("Jump");
        float speedDiff = (horizontalInput * maxSpeed) - rb.linearVelocityX;
        float accelRate;
        if (horizontalInput != 0)
            accelRate = acceleration;
        else
            accelRate = deceleration;

        rb.linearVelocityX += speedDiff * accelRate * Time.deltaTime;
        rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -maxSpeed, maxSpeed);
        if (!isGrounded)
            rb.linearVelocityY -= gravity * Time.deltaTime;
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}