using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float horizontal;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float jumpingPower = 8f;

    private bool isFacingRight = true;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("WallJumping")]
    private bool isWallSliding;
    private bool isWallJumping;

    private float wallSlidingSpeed = 2f;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;

    [SerializeField] private Vector2 wallJumpingPower = new Vector2(8f, 16f);

    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    [Header("Dashing")]
    private bool canDash = true;
    private bool isDashing;

    [SerializeField] private float dashingPower = 24f;
    [SerializeField] private float dashingTime = 0.5f;
    [SerializeField] private float dashingCooldown = 1f;

    private TrailRenderer tr;

    private Rigidbody2D rb;

    private bool doubleJump;

    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        tr = rb.GetComponent<TrailRenderer>();

        animator = rb.GetComponent<Animator>();
    }

    private void Update()
    {
        if (isDashing)
            return;

        horizontal = Input.GetAxisRaw("Horizontal");

        if (horizontal >= 0.1f ||  horizontal <= -0.1f)
            animator.SetBool("IsRunning", true);
        else
            animator.SetBool("IsRunning", false);

        if (Input.GetButtonDown("Jump"))
        {
            if (IsGrounded())
            {
                Jump();
                doubleJump = true;
            }
            else if (doubleJump && AddAbility.doubleJumpComp)
            {
                doubleJump = false;
                Jump();
            }
        }

        WallSlide();
        WallJump();

        if (!isWallJumping)
            Flip();

        if(Input.GetKeyDown(KeyCode.LeftShift) && canDash && AddAbility.dashComp)
            StartCoroutine(Dash());
    }

    private void FixedUpdate()
    {
        if (isDashing)
            return;

        if (!isWallJumping)
            rb.linearVelocity = new Vector2 (horizontal * speed, rb.linearVelocity.y);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
    }

    private void WallSlide()
    {
        if (IsWalled() &&  !IsGrounded() && horizontal != 0f)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
            isWallSliding = false;
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
            wallJumpingCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.linearVelocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * dashingPower, 0f);
        tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        canDash = true;
        yield return new WaitForSeconds(dashingCooldown);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            animator.SetBool("IsJumping", false);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            animator.SetBool("IsJumping", true);
    }
}
