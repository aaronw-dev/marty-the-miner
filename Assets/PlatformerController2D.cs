using System.Collections;
using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
    public float breathPoints = 100;
    public float currentBreathPoints;
    public float breathLossRate = 5;
    public float breathRecoveryRate = 10;
    public float dashTime = 0.01f;
    public float wallJumpTimeout = 0.5f;
    public float coyoteTime = 0.2f;
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
    public Vector2 dashSpeed;
    public Vector2 wallJumpSpeed;

    public Image breathbar;
    public Animator breathbarAnimator;
    public CinemachineVirtualCameraBase cameraBase;
    public bool enableAnimation = true;
    public bool movementEnabled = true;
    public bool disableMovementOnStart = false;
    public GameObject jumpFXPrefab;
    public GameObject landFXPrefab;
    public GameObject wallLeftFXPrefab;
    public GameObject wallRightFXPrefab;
    public GameObject splashFXPrefab;

    [ReadOnly]
    public bool isJumping;

    [ReadOnly]
    public bool isGrounded;

    [ReadOnly]
    public bool isSubmerged;

    [ReadOnly]
    public bool isDashing;

    [ReadOnly]
    public bool isOnWall;

    [ReadOnly]
    public bool leftSideOnWall;

    [ReadOnly]
    public bool isWallJumping;

    [ReadOnly]
    public bool ignoreLeftWall;

    [ReadOnly]
    public bool ignoreRightWall;

    [ReadOnly]
    public bool canDash;

    [ReadOnly]
    public bool isRecoveringBreath;

    [ReadOnly]
    public bool isDying;
    private float breathPercentage;
    private Animator anim;
    private Rigidbody2D rb;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float gravity;
    private float jumpVelocity;
    private Vector2 inputBuffer;
    private Vector2 lastInputBuffer;
    private PlayerInput playerInput;
    private Vector2 startPosition;
    private CameraConstraintGate lastGate;
    public static PlatformerController2D global;

    private void Awake()
    {
        global = this;
    }

    private void Start()
    {
        startPosition = transform.position;
        playerInput = GetComponent<PlayerInput>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        gravity = (2 * jumpHeight) / Mathf.Pow(timeToApex, 2);
        jumpVelocity = gravity * timeToApex;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        SwitchInputMode("Platforming");
        currentBreathPoints = breathPoints;
        breathPercentage = 1;

        if (disableMovementOnStart)
        {
            DisableMovement();
        }
    }

    public void SwitchInputMode(string modeName)
    {
        playerInput.SwitchCurrentActionMap(modeName);
    }

    public void EnableMovement()
    {
        movementEnabled = true;
    }

    public void DisableMovement()
    {
        movementEnabled = false;
    }

    public void SetCheckpoint(Transform checkpointTransform)
    {
        startPosition = checkpointTransform.position;
    }

    public void SetLastGate(CameraConstraintGate gate)
    {
        lastGate = gate;
    }

    public void platformingInput(CallbackContext ctx)
    {
        if (!movementEnabled) return;

        inputBuffer = ctx.ReadValue<Vector2>();
        if (inputBuffer.magnitude > 0)
            lastInputBuffer = inputBuffer;
    }

    public void platformingJump(CallbackContext ctx)
    {
        if (!movementEnabled) return;

        if (ctx.performed)
        {
            if (isJumping)
            {
                return;
            }
            if (jumpFXPrefab)
            {
                GameObject jumpFX = Instantiate(
                    jumpFXPrefab,
                    groundCheck.position,
                    Quaternion.identity
                );
            }
            if (anim && enableAnimation)
                anim.SetTrigger("jump");
            if ((!isOnWall && isGrounded) || coyoteCounter > 0)
            {
                coyoteCounter = 0;
                rb.linearVelocityY = jumpVelocity;
                isJumping = true;
            }
            else if (isOnWall)
            {
                isWallJumping = true;
                ignoreLeftWall = leftSideOnWall;
                ignoreRightWall = !leftSideOnWall;
                rb.linearVelocity =
                    (wallJumpSpeed * (leftSideOnWall ? Vector2.right : Vector2.left))
                    + wallJumpSpeed.y * Vector2.up;
                canDash = true;
                StartCoroutine(wallJumpTimer());
                isJumping = true;
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
        if (!movementEnabled) return;

        if (ctx.performed && canDash)
        {
            if (anim && enableAnimation)
                anim.SetTrigger("dash");
            rb.linearVelocity =
                dashSpeed
                * new Vector2(Mathf.Sign(lastInputBuffer.x), Mathf.Sign(lastInputBuffer.y));
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
        if (!movementEnabled) return;

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
                isJumping = false;
                if (detectWallLeft && wallLeftFXPrefab)
                {
                    GameObject wallLeftFX = Instantiate(
                        wallLeftFXPrefab,
                        wallCheckLeft.position,
                        Quaternion.identity
                    );
                }
                else if (detectWallRight && wallRightFXPrefab)
                {
                    GameObject wallRightFX = Instantiate(
                        wallRightFXPrefab,
                        wallCheckRight.position,
                        Quaternion.identity
                    );
                }
            }
            if (detectWallLeft || detectWallRight)
                leftSideOnWall = detectWallLeft;

            isOnWall = detectWall;

            if (detectWall)
                canDash = false;
        }
        else
        {
            isOnWall = false;
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
            isJumping = false;

            if (landFXPrefab)
            {
                GameObject landFX = Instantiate(
                    landFXPrefab,
                    groundCheck.position,
                    Quaternion.identity
                );
            }

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
            if (currentBreathPoints > 0)
                currentBreathPoints -= breathLossRate * Time.deltaTime;
            else if (!isDying)
            {
                DieAndReset();
                isDying = true;
            }
        }
        else
        {
            currentBreathPoints += breathRecoveryRate * Time.deltaTime;
            currentBreathPoints = Mathf.Clamp(currentBreathPoints, 0, breathPoints);
        }
        bool recoveringBreathThisFrame = !detectWater && (currentBreathPoints < breathPoints);
        if (!recoveringBreathThisFrame && isRecoveringBreath)
        {
            // breathbarAnimator.Play("close");
            breathbarAnimator.SetBool("isOpen", false);
        }
        isRecoveringBreath = recoveringBreathThisFrame;

        if (breathbar)
        {
            float targetBreathPercentage = currentBreathPoints / breathPoints;
            breathPercentage = Mathf.Lerp(
                breathPercentage,
                targetBreathPercentage,
                Time.deltaTime * 3
            );
            breathbar.fillAmount = Mathf.Round(breathPercentage * 120) / 120;
        }

        if (detectWater && !isSubmerged)
        {
            // breathbarAnimator.Play("open");
            breathbarAnimator.SetBool("isOpen", true);

            if (splashFXPrefab)
            {
                GameObject splashFX = Instantiate(
                    splashFXPrefab,
                    breathCheck.position,
                    Quaternion.identity
                );
            }

            SwitchInputMode("Swimming");
        }
        else if (!detectWater && isSubmerged)
        {
            if (splashFXPrefab)
            {
                GameObject splashFX = Instantiate(
                    splashFXPrefab,
                    breathCheck.position + Vector3.down * 0.4f,
                    Quaternion.identity
                );
            }
            SwitchInputMode("Platforming");
            // currentBreathPoints = breathPoints;
            rb.linearVelocityY = waterExitSpeed;
            ignoreLeftWall = false;
            ignoreRightWall = false;
            canDash = true;
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
        if (isGrounded)
            coyoteCounter = coyoteTime;
        if (!isGrounded && !isSubmerged && !isDashing && !isOnWall)
        {
            coyoteCounter -= Time.deltaTime;
            coyoteCounter = Mathf.Clamp(coyoteCounter, 0, coyoteTime);
            rb.linearVelocityY -= gravity * Time.deltaTime;
        }

        isSubmerged = detectWater;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing)
            StopDash();
    }

    [Button]
    public void DieAndReset()
    {
        StartCoroutine(DieResetCoroutine());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadSceneCoroutine(sceneIndex.ToString()));
    }

    public IEnumerator LoadSceneCoroutine(string sceneName)
    {
        Vector2 playerPosition = Camera.main.WorldToScreenPoint(transform.position);
        yield return CanvasWiper.global.StartCoroutine(
            CanvasWiper.global.wipeScreen(playerPosition, true)
        );

        SceneManager.LoadScene(sceneName);
    }

    public IEnumerator DieResetCoroutine()
    {
        isDying = true;
        inputBuffer = Vector2.zero;
        DisableMovement();
        Vector2 playerPosition = Camera.main.WorldToScreenPoint(transform.position);
        yield return CanvasWiper.global.StartCoroutine(
            CanvasWiper.global.wipeScreen(playerPosition, true)
        );
        cameraBase.Follow = transform;
        transform.position = startPosition;
        gravity = (2 * jumpHeight) / Mathf.Pow(timeToApex, 2);
        jumpVelocity = gravity * timeToApex;

        isJumping = false;
        isDashing = false;
        isOnWall = false;
        isRecoveringBreath = false;
        isSubmerged = false;
        canDash = true;
        SwitchInputMode("Platforming");

        if (anim)
            anim.SetTrigger("land");

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (lastGate != null)
        {
            CameraConstraintManager.global.SetActiveGate(lastGate);
        }
        else
        {
            CameraConstraintManager.global.DisableAllAndSetFirst();
        }

        currentBreathPoints = breathPoints;
        breathPercentage = 1;
        playerPosition = Camera.main.WorldToScreenPoint(transform.position);
        breathbarAnimator.SetBool("isOpen", false);

        yield return new WaitForSeconds(1);
        EnableMovement();
        isDying = false;
        yield return CanvasWiper.global.StartCoroutine(
            CanvasWiper.global.wipeScreen(playerPosition, false)
        );
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
