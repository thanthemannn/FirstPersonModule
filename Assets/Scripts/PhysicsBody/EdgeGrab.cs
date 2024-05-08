using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Than.Input;

namespace Than.Physics3D
{
    public class EdgeGrab : PhysicsBodyModule
    {


        [Header("General")]
        public Brain brain;
        [Tooltip("Transform that is used to represent the head / eye level. Usually the camera")] public Transform head;

        [Tooltip("How far beyond the collider radius to check for safe clamber edges.")] public float edgeCheckDistance = .25f;

        [Tooltip("How far above our head should we begin the edge check.")] public float edgeCheckYOffset = .25f;

        [Tooltip("How long is the edge check line that begins near the objects's head and moves relative downward.")] public float edgeCheckHeight = 1f;
        public float edgeGrabSnapOffset = 0.1f;

        [Tooltip("Make sure we don't grab anything too slanted sideways.")]
        [Range(0, 180)] public float edgeGrabAllowedNormalDegreeVariance_roll = 45;

        [Tooltip("Make sure we don't grab anything too slippery (bent towards us).")]
        [Range(0, 180)] public float edgeGrabAllowedNormalDegreeVariance_pitch = 20;

        [Tooltip("The force applied to push against the edge we are climbing.")]
        public float edgeGrabClingForce = 1.5f;

        public float edgeGrabBoostForce => physicsBody.ascent_gravityScale * 2;

        bool ClamberInputHeld => brain.Jump.held && !brain.Crouch.held;

        public bool active { get; private set; }


        // Update is called once per frame
        void FixedUpdate()
        {
            active = ClamberInputHeld && !physicsBody.isGrounded && EdgeCheck();
            if (active)
            {
                EdgeGrabBoost();
            }
        }


        bool EdgeCheck()
        {
            Vector3 relUp = transform.up;
            Vector3 relFwd = transform.forward;

            float dist = physicsBody.capsuleCollider.radius + edgeCheckDistance;

            Vector3 downStart = head.transform.position + (relUp * edgeCheckYOffset) + (relFwd * dist);
            Vector3 downEnd = downStart - (relUp * edgeCheckHeight);

            bool hitDown = Physics.Linecast(downStart, downEnd, out RaycastHit downHit, physicsBody.layerMask);
            Debug.DrawLine(downStart, downEnd, Color.grey);

            if (hitDown)
            {
                float rollAngleDiff = Mathf.Abs(Vector3.SignedAngle(downHit.normal, transform.right, transform.forward) + 90);
                bool withinRollVariance = rollAngleDiff <= edgeGrabAllowedNormalDegreeVariance_roll;

                float pitchAngleDiff = Mathf.Abs(Vector3.SignedAngle(transform.forward, downHit.normal, transform.up)) - 90;
                bool withinPitchVariance = pitchAngleDiff <= edgeGrabAllowedNormalDegreeVariance_pitch;

                return withinRollVariance && withinPitchVariance;
            }

            return false;
        }

        void EdgeGrabBoost()
        {
            Vector3 force = transform.up * edgeGrabBoostForce;

            if (physicsBody.MoveStep == Vector3.zero)
                force += transform.forward * edgeGrabClingForce;

            physicsBody.velocity = force;

            Debug.DrawRay(physicsBody.rb.position, force, Color.cyan);
        }
    }
}