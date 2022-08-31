using System.Collections;
using UnityEngine;
using Than.Input;

namespace Than.Physics3D
{
    //*Should execute after manual move input scripts
    [DefaultExecutionOrder(4)]
    [RequireComponent(typeof(PhysicsBody))]
    public class ManualSlide : MonoBehaviour
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

        PhysicsBody pb;
        Vector3 velocity;
        RaycastHit groundCastHitInfo;

        #endregion

        #region Unity Methods

        void Awake()
        {
            pb = GetComponent<PhysicsBody>();
        }

        void OnEnable()
        {
            brain.Crouch.onHeldChange += Slide;
            pb.onGroundStatusChange += GroundStatusChanged;
        }

        void OnDisable()
        {
            brain.Crouch.onHeldChange -= Slide;
            pb.onGroundStatusChange -= GroundStatusChanged;
            Slide(false);
        }

        #endregion

        #region Helper Functions

        //* Start a slide if we hold crouch while landing
        void GroundStatusChanged(bool ground)
        {
            if (ground && brain.Crouch)
                Slide(true);
        }

        bool SlopeCheck() => (pb.GroundCast(out groundCastHitInfo) && PhysicsBody.IsNormalSlidable(groundCastHitInfo.normal, transform.up, minSlopeAngle));

        #endregion

        #region Physics

        void Slide(bool activate)
        {
            if (!pb.isGrounded)
                return;

            //*Some reset activities
            velocity = Vector3.zero;
            StopAllCoroutines();
            if (!activate)
                return;

            //*Get our last movestep as our launch slide velocity
            Vector3 v = pb.LastMoveStep;

            //* Add speed of our slidable slope if relevant
            bool onSlideableSlope = SlopeCheck();
            if (onSlideableSlope)
                v += PhysicsBody.GetSlopeForceFromNormal(groundCastHitInfo.normal, transform.up, speedOnSlope);

            //* Manual slides are permitted if we are moving fast enough OR are on a slope with a strong enough angle
            if (onSlideableSlope || v.magnitude >= minVelocityForSlide)
            {
                velocity = v;
                StartCoroutine(RunSlide());
            }
        }

        IEnumerator RunSlide()
        {
            //*Runs slide as long as our manual movement isn't a stronger opposite force to our current slide velocity
            while (Vector3.Dot(pb.manualMovement, velocity) > -velocity.magnitude)
            {
                yield return null;

                pb.AddForceImpulse(velocity);
                velocity = PhysicsBody.ApplyDrag(velocity, slideDrag);

                //* If we are still on a slope, keep that speed going
                //* We use movetowards here as we may have started the slide in a direction opposite to the slope
                if (SlopeCheck())
                {
                    Vector3 slopeVelocity = PhysicsBody.GetSlopeForceFromNormal(groundCastHitInfo.normal, transform.up, speedOnSlope);
                    velocity = Vector3.MoveTowards(velocity, slopeVelocity, slopeVelocity.magnitude * 2 * Time.deltaTime);
                }
            }

            velocity = Vector3.zero;
        }

        #endregion
    }
}