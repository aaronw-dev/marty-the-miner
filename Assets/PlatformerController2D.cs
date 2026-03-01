using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerController2D : MonoBehaviour
{
    public float maxPlatformSpeed = 10f;
    public float maxSwimSpeed = 6f;
    public float jumpHeight = 2.5f;
    public float timeToApex;
    public float airControlFactor = 0.2f;
    public float acceleration = 40f;
    public float deceleration = 60f;
    public float airDeceleration = 5f;
    public float swimmingAcceleration = 10f;
    public float swimmingDeceleration = 5f;
    public float dashRecoveryTime = 0.2f;
    public float waterExitSpeed = 8f;
    public Transform groundCheck;
    public float groundCheckRadius;
    public Transform breathCheck;
    public float breathCheckRadius;
    public Transform wallCheckRight,
        wallCheckLeft;
    public Vector2 wallCheckSize;
    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public LayerMask wallLayer;
    public SpriteRenderer spriteRenderer;
    public bool enableAnimation = true;
    public Vector2 dashSpeed;
    public Vector2 wallJumpSpeed;
    public float breathPoints = 100;
    public float currentBreathPoints;
    public float breathLossRate = 5;
    public float breathRecoveryRate = 10;
    public float dashTime = 0.01f;
    private Animator anim;
    private Rigidbody2D rb;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float gravity;
    private float jumpVelocity;
    public bool isGrounded;
    public bool isSubmerged;
    public bool isDashing;
    public bool isOnWall;
    public bool leftSideOnWall;
    public bool isWallJumping;
    public float wallJumpTimeout = 0.5f;
    private Vector2 inputBuffer;
    private Vector2 lastInputBuffer;
    private PlayerInput playerInput;
    private bool canDash;
    public bool ignoreLeftWall;
    public bool ignoreRightWall;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        gravity = (2 * jumpHeight) / Mathf.Pow(timeToApex, 2);
        jumpVelocity = gravity * timeToApex;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        SwitchInputMode("Platforming");
        currentBreathPoints = breathPoints;
        // Debug.Log($"Calculated gravity: {gravity}");
        // Debug.Log($"Calculated jump velocity: {jumpVelocity}");
    }

    public void SwitchInputMode(string modeName)
    {
        playerInput.SwitchCurrentActionMap(modeName);
    }

    public void platformingInput(CallbackContext ctx)
    {
        inputBuffer = ctx.ReadValue<Vector2>();
        if (inputBuffer.magnitude > 0)
            lastInputBuffer = inputBuffer;
    }

    public void platformingJump(CallbackContext ctx)
    {
        if (ctx.performed)
        {
            if (anim && enableAnimation)
                anim.SetTrigger("jump");
            if (!isOnWall && isGrounded)
            {
                rb.linearVelocityY = jumpVelocity;
            }
            else if (isOnWall)
            {
                isWallJumping = true;
                ignoreLeftWall = leftSideOnWall;
                ignoreRightWall = !leftSideOnWall;
                rb.linearVelocity =
                    (wallJumpSpeed * (leftSideOnWall ? Vector2.right : Vector2.left))
                    + wallJumpSpeed.y * Vector2.up;
                StartCoroutine(wallJumpTimer());
            }
        }
    }

    public IEnumerator wallJumpTimer()
    {
        yield return new WaitForSeconds(wallJumpTimeout);
        isWallJumping = false;
    }

    public void platformingDash(CallbackContext ctx)
    {
        if (ctx.performed && canDash)
        {
            if (anim && enableAnimation)
                anim.SetTrigger("dash");
            rb.linearVelocity =
                dashSpeed * new Vector2(Mathf.Sign(lastInputBuffer.x), Mathf.Sign(lastInputBuffer.y));
            StartCoroutine(DashStart());
        }
    }

    public void StopDash()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;
        dashRecovery();
    }

    public IEnumerator DashStart()
    {
        canDash = false;
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        StopDash();
    }

    public void swimmingInput(CallbackContext ctx)
    {
        inputBuffer = ctx.ReadValue<Vector2>();
    }

    public IEnumerator dashRecovery()
    {
        yield return new WaitForSeconds(dashRecoveryTime);
        canDash = true;
    }

    private void Update()
    {
        bool detectGround = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
        if (!isWallJumping && !isGrounded)
        {
            bool detectWallLeft =
                Physics2D.OverlapBox(wallCheckLeft.position, wallCheckSize, 0, wallLayer)
                && !ignoreLeftWall;
            bool detectWallRight =
                Physics2D.OverlapBox(wallCheckRight.position, wallCheckSize, 0, wallLayer)
                && !ignoreRightWall;
            bool detectWall = detectWallLeft || detectWallRight;
            if (!isOnWall && detectWall)
            {
                rb.linearVelocity = Vector2.zero;
                rb.linearVelocityX = detectWallLeft ? -10 : 10;
            }
            if (detectWallLeft || detectWallRight)
                leftSideOnWall = detectWallLeft;

            isOnWall = detectWall;
        }
        else
        {
            isOnWall = false;
        }
        if (isOnWall)
        {
            canDash = false;
        }
        if (anim)
            anim.SetBool("isWall", isOnWall);
        if (!isGrounded && detectGround && !isSubmerged)
        {
            if (anim)
                anim.SetTrigger("land");

            ignoreLeftWall = false;
            ignoreRightWall = false;
            isDashing = false;
            StartCoroutine(dashRecovery());
        }
        isGrounded = detectGround;
        bool detectWater = Physics2D.OverlapCircle(
            breathCheck.position,
            breathCheckRadius,
            waterLayer
        );
        if (detectWater)
        {
            currentBreathPoints -= breathLossRate * Time.deltaTime;
        }
        else
        {
            currentBreathPoints += breathRecoveryRate * Time.deltaTime;
        }
        if (detectWater && !isSubmerged)
        {
            SwitchInputMode("Swimming");
        }
        else if (!detectWater && isSubmerged)
        {
            SwitchInputMode("Platforming");
            currentBreathPoints = breathPoints;
            rb.linearVelocityY = waterExitSpeed;
            Debug.Log(rb.linearVelocity);
        }
        if (anim && enableAnimation)
        {
            anim.SetBool("isGrounded", isGrounded);
            anim.SetBool("isWalking", inputBuffer.x != 0);
        }
        if (spriteRenderer && inputBuffer.x != 0)
            spriteRenderer.flipX = inputBuffer.x < 0;
        float maxSpeed = isSubmerged ? maxSwimSpeed : maxPlatformSpeed;
        float speedDiff = (inputBuffer.x * maxSpeed) - rb.linearVelocityX;
        float accelRate;
        if (inputBuffer.x != 0)
        {
            if (isGrounded && !isSubmerged)
                accelRate = acceleration;
            else if (isSubmerged)
                accelRate = swimmingAcceleration;
            else
                accelRate = acceleration * airControlFactor;
        }
        else
        {
            if (isGrounded && !isSubmerged)
                accelRate = deceleration;
            else if (isSubmerged)
                accelRate = swimmingDeceleration;
            else
                accelRate = airDeceleration;
        }

        if (!isSubmerged && !isDashing)
        {
            rb.linearVelocityX += speedDiff * accelRate * Time.deltaTime;
            rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -maxSpeed, maxSpeed);
        }
        else if (isSubmerged)
        {
            float speedDiffY = (inputBuffer.y * maxSpeed) - rb.linearVelocityY;
            rb.linearVelocityX += speedDiff * accelRate * Time.deltaTime;
            rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -maxSpeed, maxSpeed);
            rb.linearVelocityY += speedDiffY * accelRate * Time.deltaTime;
        }
        if (!isGrounded && !isSubmerged && !isDashing && !isOnWall)
            rb.linearVelocityY -= gravity * Time.deltaTime;

        isSubmerged = detectWater;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing)
            StopDash();
    }

    private void OnDrawGizmos()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (breathCheck)
        {
            Gizmos.color = Color.skyBlue;
            Gizmos.DrawWireSphere(breathCheck.position, breathCheckRadius);
        }
        Gizmos.color = Color.green;
        if (wallCheckLeft)
        {
            Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckSize);
        }
        if (wallCheckRight)
        {
            Gizmos.DrawWireCube(wallCheckRight.position, wallCheckSize);
        }
    }
}
