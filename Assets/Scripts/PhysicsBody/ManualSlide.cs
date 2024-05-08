using System.Collections;
using UnityEngine;
using Than.Input;

namespace Than.Physics3D
{
    //*Should execute after manual move input scripts
    [DefaultExecutionOrder(4)]
    public class ManualSlide : PhysicsBodyModule
    {
        #region Inspector Fields

        public Brain brain;

        [Tooltip("The linear drag coefficient. 0 means no damping. This applies as a separate drag to slide velocity only.")]
        [Min(0)] public float slideDrag = 2;

        [Tooltip("The minimum movement velocity needed to start a manual slide (if not on a slope).")]
        [Min(0)] public float minVelocityForSlide = 12f;

        [Tooltip("The minimum slope needed to start a manual slide (If not moving at min velocity).")]
        [Range(0, 360)] public float minSlopeAngle = 20;

        [Tooltip("The max velocity when sliding down a max slope at full speed.")]
        public float speedOnSlope = 20;

        #endregion

        #region Other Variables and Properties

        Vector3 velocity;
        RaycastHit groundCastHitInfo;

        #endregion

        #region Unity Methods
        void OnEnable()
        {
            brain.Crouch.onHeldChange += SlidePressedAction;
            physicsBody.onGroundStatusChange += GroundStatusChanged;
        }

        void OnDisable()
        {
            brain.Crouch.onHeldChange -= SlidePressedAction;
            physicsBody.onGroundStatusChange -= GroundStatusChanged;
            ResetSlide();
        }

        #endregion

        #region Helper Functions

        //* Start a slide if we hold crouch while landing
        void GroundStatusChanged(bool ground)
        {
            if (ground && brain.Crouch)
            {
                Vector3 v = physicsBody.LastControlledMovement;
                // if (v.sqrMagnitude < float.Epsilon)
                //     v = transform.forward;

                v = physicsBody.LastControlledMovement.normalized * physicsBody.velocity.magnitude;


                Slide(v, float.Epsilon);
            }
        }

        bool SlopeCheck() => (physicsBody.GroundCast(out groundCastHitInfo) && PhysicsBody.IsNormalSlidable(groundCastHitInfo.normal, transform.up, minSlopeAngle));

        #endregion

        #region Physics

        void SlidePressedAction(bool activate)
        {
            if (!physicsBody.isGrounded)
                return;

            ResetSlide();

            if (!activate)
                return;

            //*Get our last movestep as our launch slide velocity
            Vector3 v = physicsBody.MoveStep;
            Slide(v, minVelocityForSlide);
        }

        void Slide(Vector3 startingVelocity, float minVelocityRequired)
        {
            Vector3 v = startingVelocity;

            //* Add speed of our slidable slope if relevant
            bool onSlideableSlope = SlopeCheck();
            if (onSlideableSlope)
                v += PhysicsBody.GetSlopeForceFromNormal(groundCastHitInfo.normal, transform.up, speedOnSlope);


            //* Manual slides are permitted if we are moving fast enough OR are on a slope with a strong enough angle
            if (onSlideableSlope || v.magnitude > minVelocityRequired)
            {
                velocity = v;
            }
        }

        void ResetSlide()
        {
            //*Some reset activities
            velocity = Vector3.zero;
            StopAllCoroutines();
        }

        void FixedUpdate()
        {
            if (velocity != Vector3.zero)
            {
                SlidePhysics(Time.fixedDeltaTime);
            }
        }

        void SlidePhysics(float deltaTime)
        {
            //*Runs slide as long as our manual movement isn't a stronger opposite force to our current slide velocity
            if (Vector3.Dot(physicsBody.LastControlledMovement, velocity) > -velocity.magnitude)
            {
                physicsBody.MoveUnrestricted(velocity * deltaTime);
                velocity = PhysicsBody.ApplyDrag(velocity, slideDrag, deltaTime);

                //* If we are still on a slope, keep that speed going
                //* We use movetowards here as we may have started the slide in a direction opposite to the slope
                if (SlopeCheck())
                {
                    Vector3 slopeVelocity = PhysicsBody.GetSlopeForceFromNormal(groundCastHitInfo.normal, transform.up, speedOnSlope);
                    velocity = Vector3.MoveTowards(velocity, slopeVelocity, slopeVelocity.magnitude * 2 * deltaTime);
                }
            }
            else
            {
                velocity = Vector3.zero;
            }


        }

        #endregion
    }
}