using System.Collections;
using UnityEngine;
using Than.Input;

namespace Than.Physics3D
{
    [RequireComponent(typeof(PhysicsBody))]
    public class Jump : MonoBehaviour
    {
        #region Inspector Fields

        [Header("General")]
        public Brain brain;

        [Space(10)]
        [Tooltip("Each array value represents the force of that jump.\nThe first jump is usually from the ground, the remaining jumps are performed in the air.")]
        public float[] jumps = new float[] { 10 };

        [Tooltip("Prevents jump spam glitches.")]
        public float jumpCooldown = .05f;

        [Header("Custom Gravity")]
        [Tooltip("The gravity while the body is descending (And jump is held).")]
        public float descent_jumpHeld_gravityScale = 3;

        [Tooltip("The gravity while the body is ascending (And jump is held).")]
        public float ascent_jumpHeld_gravityScale = 1.5f;

        #endregion

        #region Other Properties and Variables

        int current_jumps;
        PhysicsBody pb;
        float lastJumpTime = 0;
        Vector3 lastJumpForce = Vector3.zero;
        RaycastHit jumpGroundCast;

        #endregion

        #region Control Methods

        void JumpPressed()
        {
            StartCoroutine(HoldGravity());
            AttemptJump();
        }

        public void AttemptJump()
        {
            if (current_jumps < jumps.Length)
            {
                PerformJump(jumps[current_jumps]);
                current_jumps++;
            }
        }

        public void ReplenishJumps()
        {
            //*Give the very first jump back only if we are on the ground
            current_jumps = pb.isGrounded ? 0 : 1;
        }

        #endregion

        #region Unity Methods

        void Awake()
        {
            pb = GetComponent<PhysicsBody>();
            current_jumps = jumps.Length;
        }

        void OnEnable()
        {
            brain.Jump.onPress += JumpPressed;
        }

        void OnDisable()
        {
            StopAllCoroutines();
            brain.Jump.onPress -= JumpPressed;
        }

        void FixedUpdate()
        {
            if (pb.isGrounded && Time.fixedTime > lastJumpTime + pb.groundCoyoteTime + jumpCooldown) //* Artificial cooldown using coyote time to avoid potential double jumps while the ground is still buffered
            {
                current_jumps = 0;
            }
        }

        #endregion

        #region Physics

        void PerformJump(float force)
        {
            Vector3 relVel = pb.RelativeVelocity;
            if (relVel.y < 0)
                pb.RelativeVelocity = new Vector3(relVel.x, 0, relVel.z);

            Vector3 forceDir = transform.up;

            //* If we are on the ground and sliding, make our jump direction the normal of the surface we are sliding on
            if (pb.GroundCast(out jumpGroundCast) && pb.IsNormalSlidable(jumpGroundCast.normal))
                forceDir = jumpGroundCast.normal;

            lastJumpForce = forceDir * force;
            pb.AddForce(lastJumpForce);

            lastJumpTime = Time.fixedTime;
        }

        IEnumerator HoldGravity()
        {
            while (brain.Jump)
            {
                pb.SetGravityThisFrame(descent_jumpHeld_gravityScale, ascent_jumpHeld_gravityScale);
                yield return null;
            }
        }

        #endregion
    }
}