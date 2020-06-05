﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb = null;
    [SerializeField]
    private Collider coll = null;
    /// <summary>
    /// The object to reference when moving "forward," "left," "backward," or "right."
    /// </summary>
    [SerializeField]
    private GameObject orienter = null;

    [Header("Movement Settings")]
    [Tooltip("The max speed the player can move via input.")]
    [SerializeField] private float maxMoveVelocity = 3.5f;
    [SerializeField] private float moveSpeed = 1;

    [Header("Wall Ride Settings")]
    [SerializeField] [Range(0, 2)] private float wallRideTime = 1;
    private float wallRideTimer = 0;

    [Header("Jump Settings")]
    [SerializeField] private float jumpPower = 5;
    [SerializeField] [Range(1.001f, 4)] private float fallGravityMultiplier = 2f;
    [SerializeField] [Range(0, 0.5f)] private float coyoteTime = 0.1f;
    [SerializeField] [Range(0, 0.5f)] private float jumpCooldownTime = 0.1f;
    [SerializeField] [Range(0, 0.5f)] private float jumpBufferTime = 0.1f;
    private bool canJump;
    private bool onJumpCooldown;
    private float secondsOffGround;
    private bool jumpBuffered;

    //--- Main Functions --- //

    void FixedUpdate()
    {
        //Set up a movement vector.
        Vector3 inputDir = Vector3.zero;

        //If the WASD keys are held, add the corresponding direction to the move direction.
        if (Input.GetKey(KeyCode.W))
        {
            inputDir += orienter.transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            inputDir += -orienter.transform.right;
        }
        if (Input.GetKey(KeyCode.S))
        {
            inputDir += -orienter.transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            inputDir += orienter.transform.right;
        }

        if (inputDir != Vector3.zero)
        {
            //Now that we've gotten all the directions of the user's input, normalize it and move with it.
            inputDir.Normalize();
            inputDir = GetWallRideDirection(inputDir);
            MoveWithDirection(inputDir);
        }
    }

    private void Update()
    {
        SetJumpAllowance();

        if (IsGrounded()) { wallRideTimer = 0; }

        //If the user presses space and is allowed to jump, make the player jump.
        StartCoroutine(CheckForBufferedJump(jumpBufferTime));
        if (!onJumpCooldown && jumpBuffered && canJump)
        {
            //Immediately cancel wall rides by adding to the timer.
            wallRideTimer += wallRideTimer;

            //There is no longer a jump buffered because we're about to do that jump.
            jumpBuffered = false;

            //Start a cooldown on jumping, so the player can't jump again immediately.
            //This mitigates problems with the leeway on IsGrounded(), which is otherwise necessary.
            StartCoroutine(SetJumpCooldown(jumpCooldownTime));

            rb.velocity = new Vector3(rb.velocity.x, jumpPower, rb.velocity.z);

            //Also expedite the rolling progress during jumps by adding some torque.
            //This cross product gets the axis of angular velocity (perpendicular to velocity)
            rb.AddTorque(Vector3.Cross(Vector3.up, rb.velocity));

            //Take note that the player just jumped. This is reset upon being grounded, and ensures the player
            //can't jump twice.
            canJump = false;
        }

        //If the player isn't holding space after they jump, and they haven't hit the peak of their jump yet,
        //Increase gravity to allow for a short hop
        if (!Input.GetKey(KeyCode.Space) && rb.velocity.y > 0)
        {
            //We subtract fallGrav by 1 because gravity is already added once per frame; to make fallGrav
            //accurate, we need to subtract it by 1. fallGrav can't be 1 due to its range property, so no zeroes
            rb.velocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1) * Time.deltaTime;
        }
        //Otherwise, if the player has reached the peak of their jump, increase gravity to make the jump feel
        //weightier
        else if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1) * Time.deltaTime;
        }
    }

    //--- Helper Functions ---//

    /// <summary>
    /// Applies velocity in the given direction, diregarding the y component of velocity.
    /// </summary>
    /// <param name="moveDir">The direction to move in.</param>
    private void MoveWithDirection(Vector3 moveDir)
    {
        //First, get the current velocity and discard the y component.
        Vector3 velocityXZ = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        //Now add movespeed to it, and then clamp it to make sure it doesn't exceed maxMoveVelocity.
        Vector3 newVel = velocityXZ + moveDir * moveSpeed * Time.deltaTime;
        newVel = Vector3.ClampMagnitude(newVel, maxMoveVelocity);

        //Finally, now that we've set and clamped the new velocity, apply it.
        //If newVel has a y component, we're wall riding, so gravity doesn't matter and we can apply directly.
        if (newVel.y > 0)
        {
            rb.velocity = new Vector3(newVel.x, newVel.y, newVel.z);
        }
        else
        {
            rb.velocity = new Vector3(newVel.x, rb.velocity.y, newVel.z);
        }
    }

    private Vector3 GetWallRideDirection(Vector3 moveDir)
    {
        //If we haven't exceeded the max wall ride time,
        if (wallRideTimer < wallRideTime)
        {
            //Check if there is something in the direction the player wants to move.
            RaycastHit hit;
            bool castSuccess = Physics.SphereCast
            (
                //same as IsGrounded(), but out is not discarded and direction is different
                transform.position + -moveDir * 0.025f,
                coll.bounds.extents.y,
                moveDir,
                out hit,
                0.05f
            );

            //If the cast hit something, and the player is moving toward (or directly along) that wall,
            if (castSuccess && Vector3.Dot(hit.normal, moveDir) <= 0)
            {
                rb.useGravity = false;

                //Add to the wall ride timer.
                wallRideTimer += Time.deltaTime;

                //Get the component of moveDir that is toward the hit and subtract it from moveDir so we can
                //manipulate it independently.
                Vector3 towardHit = Vector3.Project(moveDir, hit.normal);
                moveDir -= towardHit;

                //Make the component toward the hit face upward instead, then add it back to moveDir.
                towardHit = Vector3.up * towardHit.magnitude;
                moveDir += towardHit;
            }
            else
            {
                rb.useGravity = true;
            }
        }
        else
        {
            rb.useGravity = true;
        }

        return moveDir;
    }

    /// <summary>
    /// Checks to see if there is a collider directly below the player.
    /// </summary>
    /// <returns>Whether there is a collider directly below the player or not.</returns>
    private bool IsGrounded()
    {
        //Inspired / adapted from http://answers.unity.com/answers/196395/view.html
        //Cast a sphere with the same radius as the player downward to see if there's something underneath.
        return Physics.SphereCast
        (
            //Start just a little bit above the player, so if the player is very slightly in the ground
            //as rigidbodies sometimes are, then the ground can still be detected.
            //This ignores ceilings too, because SphereCast ignores colliders it starts inside of!
            transform.position + Vector3.up * 0.025f,
            coll.bounds.extents.y,
            Vector3.down,
            out _,
            0.05f
        );
    }

    /// <summary>
    /// Set whether the player can jump or not, allowing some leeway after leaving ground.
    /// </summary>
    private void SetJumpAllowance()
    {
        //If the player is grounded,
        if (IsGrounded())
        {
            //Reset the coyote time counter, and make sure the player can jump.
            //They hit the ground, so they're not jumping anymore.
            secondsOffGround = 0;
            canJump = true;
        }
        //If the player is not grounded,
        else
        {
            //Add to the number of frames the player has been off the ground.
            secondsOffGround += Time.deltaTime;

            //If the frame counter is greater than the leeway allowed, the player can't jump anymore.
            //The greater the frame leeway, the more time the player has to jump after leaving the ground.
            if (secondsOffGround > coyoteTime)
            {
                canJump = false;
            }
        }
    }

    /// <summary>
    /// Enables a jump cooldown for length seconds, to ensure the player can't jump twice.
    /// </summary>
    /// <param name="length">How long to wait before allowing the player to jump again.</param>
    private IEnumerator SetJumpCooldown(float length)
    {
        onJumpCooldown = true;
        yield return new WaitForSeconds(length);
        onJumpCooldown = false;
    }

    /// <summary>
    /// Check if the player has pressed the jump button, and if so, reflect that in a bool.
    /// This bool is reset after bufferTime has passed.
    /// </summary>
    /// <param name="bufferTime">How long to "hold on to" the player's input.</param>
    private IEnumerator CheckForBufferedJump(float bufferTime)
    {
        if (!jumpBuffered && Input.GetKeyDown(KeyCode.Space))
        {
            jumpBuffered = true;
            yield return new WaitForSeconds(bufferTime);
            jumpBuffered = false;
        }
    }
}
