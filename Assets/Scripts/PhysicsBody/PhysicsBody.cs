#if DEVELOPMENT_BUILD || UNITY_EDITOR
#define ALLOW_DEVMODE
#endif

using System.Collections.Generic;
using UnityEngine;


namespace Than.Physics3D
{
    [DefaultExecutionOrder(-8)]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PhysicsBody : MonoBehaviour
    {
        public Vector3 gravityDirection = Vector3.down;
        public Vector3 up;

        #region Public Properties and Events

        public Transform PlatformParent
        {
            get { return _platformParent; }

            set
            {
                if (value == _platformParent)
                    return;

                _platformParent = value;

                if (_platformParent)
                {
                    platformParent_velocity = Vector3.zero;
                    platformParent_rotationDelta = Quaternion.identity;
                    platformParent_lastPosition = _platformParent.position;
                    platformParent_lastRotation = _platformParent.rotation;
                }

            }
        }

        public Vector3 platformParent_velocity { get; private set; }
        public Quaternion platformParent_rotationDelta { get; private set; }

        public Transform sphereTest;
        public float sphereMultiplier = 1;
        //public CharacterController characterController { get; private set; }

        public float slopeLimit = 30;//=> characterController.slopeLimit;

        public Rigidbody rb { get; private set; }
        public CapsuleCollider capsuleCollider { get; private set; }
        public LayerMask layerMask { get; private set; }

        public bool isGroundedRaw => GroundCast(out cached_groundCastHitInfo);//Physics.Raycast(transform.position, -transform.up, 1.1f, layerMask);//characterController.isGrounded;
        public bool isGrounded => buffer_isGrounded;
        bool buffer_isGrounded;

        public Vector3 velocity { get { return rb.velocity; } set { rb.velocity = value; } }
        public Vector3 RelativeVelocity { get { return transform.InverseTransformDirection(rb.velocity); } set { rb.velocity = transform.InverseTransformDirection(value); } }
        Vector3 slideVelocity;
        Vector3 slideVelocity_lastFixedUpdate;
        public Vector3 LastControlledMovement => controlledMovement_lastFixedUpdate;
        Vector3 controlledMovement;
        Vector3 controlledMovement_lastFixedUpdate;
        Vector3 unrestrictedMovement;
        Vector3 unrestrictedMovement_lastFixedUpdate;
        public float current_gravityScale { get; private set; }


        public Vector3 MoveStep { get { return fixedUpdate_moveStep / Time.fixedDeltaTime; } }
        Vector3 fixedUpdate_moveStep;

        public float Current_GroundSpeedLimit => buffer_groundSpeedLimit;
        public float Current_AirSpeedLimit => Mathf.Min(buffer_airSpeedLimit, Mathf.Max(buffer_groundSpeedLimit, LastControlledMovement.magnitude));
        float buffer_groundSpeedLimit = Mathf.Infinity;
        float buffer_airSpeedLimit = Mathf.Infinity;

        public Collider[] attachedColliders { get; private set; }
        public int attachedCollider_len { get; private set; }

        public System.Action<bool> onGroundStatusChange;

        #endregion

        #region Inspector Fields

        [Header("General")]
        [Tooltip("The linear drag coefficient. 0 means no damping.")]
        [Min(0)] public float drag = 1;

        [Tooltip("Should the body rotate along it's relative Y with the platform it stands on")]
        public bool rotateWithParentPlatform = false;

        [Header("Gravity")]
        [Tooltip("The gravity while the body is descending.")]
        public float descent_gravityScale = 4;
        float buffer_descent_gravityScale;

        [Tooltip("The gravity while the body is ascending.")]
        public float ascent_gravityScale = 3;
        float buffer_ascent_gravityScale;

        [Header("Slope Physics")]
        [Tooltip("The max velocity when sliding down a max slope at full speed.")]
        public float slideSpeed = 20;

        [Tooltip("Applies a limit to the manualMovement property while the body is sliding.")]
        public float manualMoveLimitWhileSliding = 5;

        [Tooltip("The linear drag coefficient. 0 means no damping. This applies as a separate drag to slide velocity only.")]
        [Min(0)] public float slideDrag = 5;

        [Header("Ground Checking")]
        [Tooltip("How long is the body actually off the ground before we declare that they have left. Allows for a few more frames of extra ground control for the player.")]
        [Min(0)] public float groundCoyoteTime = .2f;
        float current_groundCoyoteTime = 0;

        [Tooltip("For collecting data on the surface the body is standing on.")]
        [Space(10)][SerializeField][Min(0)] float groundCastDistance = .2f;
        [Tooltip("Determines the frequency of ground casts within a slim radius around the body. The lower the value the higher the definition of surrounding casts.")]
        [SerializeField][Range(1, 360)] float groundDegreeCastStep = 22.5f;

        #endregion

        #region Other Properties and Varibles

        //*Ground cast properties
        Vector3 GroundCastCenter => transform.TransformPoint(capsuleCollider.center - Vector3.up * (capsuleCollider.height * .5f - GroundCastRadius));// * (capsuleCollider.height * .5f - capsuleCollider.radius); //transform.position + characterController.center;
        //float CastDistance => capsuleCollider.height * .5f + groundCastDistance;//characterController.height * .5f + groundCastDistance;
        float GroundCastRadius => capsuleCollider.radius * .666666f;//transform.forward * characterController.radius * .5f;

        //*Ground cast cached data
        [HideInInspector] public RaycastHit cached_groundCastHitInfo;
        bool cached_groundCastHitResult;
        float cached_groundcastTime = -1;

        //*Velocity Limiting
        RaycastHit velocityCast_allocation;


        //*Platform parenting
        Transform _platformParent;
        Vector3 platformParent_lastPosition;
        Quaternion platformParent_lastRotation;

        //* Physics freeze overrides
        // List<MonoBehaviour> movementFreezeOverride;
        // List<MonoBehaviour> gravityFreezeOverride;
        // List<MonoBehaviour> velocityFreezeOverride;

        #endregion





        #region Control Methods

        /// <summary>
        /// Applies a manual movement value that will be added to the object instantly. Move forces are cleared after every frame and can be limited by other scripts.
        /// </summary>
        public void Move(Vector3 movement)
        {
            controlledMovement += movement;
        }

        /// <summary>
        /// Applies a manual movement value that will be added to the object instantly. Impulse forces are cleared after use every frame and are NOT limited by other scripts.
        /// </summary>
        public void MoveUnrestricted(Vector3 force)
        {
            unrestrictedMovement += force;
        }

        /// <summary>
        /// Applies a force that will be added to the objects total velocity, which degrades over time due to drag.
        /// </summary>
        public void AddForce(Vector3 force)
        {
            //rb.AddForce(force, ForceMode.VelocityChange);
            velocity += force;
        }

        //* Aerial movement should carry the momentum we've taken with us from the ground, but also be limited to whatever airSpeedLimit we might set for it
        public void ApplyMoveSpeedLimit_Ground(float groundSpeedLimit)
        {
            buffer_groundSpeedLimit = Mathf.Min(groundSpeedLimit, buffer_groundSpeedLimit);
        }
        public void ApplyMoveSpeedLimit_Air(float airSpeedLimit)
        {
            buffer_airSpeedLimit = Mathf.Min(airSpeedLimit, buffer_airSpeedLimit);
        }

        public void SetGravityThisFrame(float descent, float ascent)
        {
            buffer_descent_gravityScale = descent;
            buffer_ascent_gravityScale = ascent;
            buffer_clearGravityThisFrame = false;
        }

        #endregion

        #region Helper Functions

        public bool GroundCast(out RaycastHit hitInfo)
        {
            //* We cheat this a bit, if a raycast has already been run this frame, just reuse it instead of casting again
            if (cached_groundcastTime == Time.fixedTime)
            {
                hitInfo = cached_groundCastHitInfo;
                return cached_groundCastHitResult;
            }

            //* Ignore if we are at a strong y velocity upwards
            if (RelativeVelocity.y > stickToGroundForce)
            {
                hitInfo = default(RaycastHit);
                cached_groundCastHitInfo = hitInfo;
                cached_groundCastHitResult = false;
                return false;
            }

            cached_groundcastTime = Time.fixedTime;
            // if (!isGroundedRaw)
            // {
            //     hitInfo = default(RaycastHit);
            //     cached_groundCastHitInfo = hitInfo;
            //     cached_groundCastHitResult = false;
            //     return false;
            // }

            Vector3 pos = GroundCastCenter;
            //float dist = CastDistance;

            //*Try a simple linecast from the center first
            Vector3 down = -transform.up;
            // if (Physics.Raycast(pos, down, out hitInfo, dist, layerMask))
            // {
            //     cached_groundCastHitResult = true;
            //     cached_groundCastHitInfo = hitInfo;
            //     return true;
            // }


            if (Physics.SphereCast(pos, groundCastDistance, down, out hitInfo, groundCastDistance, layerMask))
            {
                //!Removing the below condition fixed slide movement... why was it here again?
                // if (IsNormalSlidable(hitInfo.normal))
                // {
                //     hitInfo = default(RaycastHit);
                //     cached_groundCastHitInfo = hitInfo;
                //     cached_groundCastHitResult = false;
                //     return false;
                // }
                cached_groundCastHitResult = true;
                cached_groundCastHitInfo = hitInfo;
                return true;
            }

            //* If the above didn't return a hit, check around the center just a little bit
            // float push = DegreeCastPush;
            // for (float deg = 0; deg < 360; deg += groundDegreeCastStep)
            // {
            //     Vector2 degVector2 = UMath.DegreeToVector2(deg);
            //     Vector3 degreePos = transform.TransformDirection(new Vector3(degVector2.x, 0, degVector2.y)) * push;
            //     if (Physics.Raycast(pos + degreePos, down, out hitInfo, dist, layerMask))
            //     {
            //         cached_groundCastHitResult = true;
            //         cached_groundCastHitInfo = hitInfo;
            //         return true;
            //     }
            // }

            cached_groundCastHitResult = false;
            cached_groundCastHitInfo = hitInfo;
            return false;
        }

        public bool IsNormalSlidable(Vector3 normal) => IsNormalSlidable(normal, transform.up, slopeLimit);
        public static bool IsNormalSlidable(Vector3 normal, Vector3 up, float minAngle)
        {
            float slopeAngle = Vector3.Angle(normal, up);
            return slopeAngle > minAngle;
        }

        //!TODO: change referenced function calls to be within fixedUpdate()
        public static Vector3 ApplyDrag(Vector3 velocityVector, float drag, float deltaTime)
        {
            velocityVector = velocityVector * (1 - deltaTime * drag);
            if (velocityVector.sqrMagnitude < .01f)
                velocityVector = Vector3.zero;

            return velocityVector;
        }

        public static Vector3 GetSlopeForceFromNormal(Vector3 normal, Vector3 up, float baseSpeed)
        {
            return (up - normal * Vector3.Dot(up, normal)) * -baseSpeed;
        }

        #endregion

        #region Unity Methods

        void Awake()
        {
            buffer_descent_gravityScale = descent_gravityScale;
            buffer_ascent_gravityScale = ascent_gravityScale;
            //characterController = GetComponent<CharacterController>();
            rb = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            layerMask = gameObject.layer.GetLayerMaskFromCollisionMatrix();

            attachedColliders = rb.GetComponentsInChildren<Collider>();//characterController.GetComponentsInChildren<Collider>();
            attachedCollider_len = attachedColliders.Length;
        }

        //Vector3 rbMoveStep;
        // void LateUpdate()
        // {
        //     bool groundCast = GroundCast(out cached_groundCastHitInfo);

        //     //* Clamp manual movement to our determined limits
        //     lastManualMovement_UnclampedLimit = manualMovement;
        //     manualMovement = Vector3.ClampMagnitude(manualMovement, isGrounded ? Current_GroundSpeedLimit : Current_AirSpeedLimit);

        //     velocity += UpdateGravityCalculation();

        //     //* Ensures that we don't apply too much velocity against an object to break through it or cause other glitches
        //     //if (characterController.Cast(velocity.normalized, out velocityCast_allocation, velocity.magnitude * Time.deltaTime, layerMask))
        //     //if (capsuleCollider.Cast(velocity.normalized, out velocityCast_allocation, velocity.magnitude * Time.deltaTime, layerMask))
        //     if (rb.SweepTest(velocity.normalized, out velocityCast_allocation, velocity.magnitude * Time.deltaTime))
        //     {
        //         velocity = Vector3.ClampMagnitude(velocity, velocityCast_allocation.distance);
        //     }

        //     UpdateSlideVelocity(groundCast);

        //     //*Add all of this frames forces together to get our moveStep
        //     buffer_moveStep = velocity + slideVelocity + manualMovement + impulseForces;

        //     //* Allows us to tweak this step a bit more within the function without affecting our public property LastMoveStep
        //     rbMoveStep = buffer_moveStep;

        //     //*Avoid bumpy movement when running / moving down slopes
        //     //*Needs to be well on ground (with groundCast), not moving upwards, not sliding, and on some sort of slope
        //     Vector3 localMoveStep = transform.InverseTransformDirection(rbMoveStep);
        //     //Debug.Log(rbMoveStep + " | " + localMoveStep + " | " + transform.TransformDirection(localMoveStep));
        //     if (groundCast && localMoveStep.y <= 0 && slideVelocity.sqrMagnitude < 1 && cached_groundCastHitInfo.normal != transform.up)
        //     {
        //         // localMoveStep.y = -buffer_moveStep.magnitude;
        //         // rbMoveStep = transform.TransformDirection(localMoveStep);
        //     }
        //     //TODO this is breaking our sphere movement ^^^
        //     //internalMovement.y = -buffer_moveStep.magnitude;

        //     //*Apply character movement and drag
        //     //characterController.Move(internalMovement * Time.deltaTime);
        //     //rb.MovePosition(rb.position + internalMovement * Time.deltaTime);
        //     velocity = ApplyDrag(velocity, drag);

        //     //*Saves our last movebuffer for various uses
        //     if (isGrounded)
        //         lastGroundManualMovement = manualMovement;
        //     else
        //         lastAirManualMovement = manualMovement;
        //     lastManualMovement = manualMovement;

        //     //* Reset temporary values / forces
        //     manualMovement = impulseForces = Vector3.zero;
        //     buffer_airSpeedLimit = buffer_groundSpeedLimit = Mathf.Infinity;
        //     GroundBufferUpdate();
        // }

        public float stickToGroundForce = 2;
        public const float gravitationalConstant = 0.0001f;

        public float collisionDetectionEpsilon = .1f;



        void FixedUpdate()
        {
            bool groundCast = GroundCast(out cached_groundCastHitInfo);

            GravityScaleUpdate();

            if (groundCast)
            {
                rb.AddForce(-cached_groundCastHitInfo.normal * stickToGroundForce, ForceMode.VelocityChange);//-transform.up * stickToGroundForce, ForceMode.VelocityChange);
            }

            UpdatePlatformParent(groundCast);

            //*Gravity
            Vector3 acceleration = GravityDirection * -Physics.gravity.y * current_gravityScale;
            rb.AddForce(acceleration, ForceMode.Acceleration);
            //*Align to gravity
            rb.MoveRotation(Quaternion.FromToRotation(transform.up, -GravityDirection) * rb.rotation);

            Vector3 slideVel = UpdateSlideVelocity(groundCast, Time.fixedDeltaTime);

            //*Movement forces
            fixedUpdate_moveStep = controlledMovement + unrestrictedMovement + slideVel;// + (slideVelocity * Time.fixedDeltaTime);

            //* Collision detection using our capsule
            Vector3 moveStepDir = fixedUpdate_moveStep.normalized;
            Vector3 pos = rb.position + capsuleCollider.center;
            Vector3 offset = capsuleCollider.transform.up * (capsuleCollider.height * .5f - capsuleCollider.radius);
            //* Recede the cast capsule by epsilon and increase the check distance by epsilon to equal it out. This allows for collision detection slightly "inside the skin" of our collider
            if (Physics.CapsuleCast(pos - offset, pos + offset, capsuleCollider.radius - collisionDetectionEpsilon, moveStepDir, out RaycastHit hitInfo, fixedUpdate_moveStep.magnitude + collisionDetectionEpsilon))
            {
                //*Shorten our movement step to account for the collision
                fixedUpdate_moveStep = moveStepDir * (hitInfo.distance - collisionDetectionEpsilon);//(Vector3.Dot(hitInfo.point - rb.position, moveStepDir) - capsuleCollider.radius);
            }

            rb.MovePosition(rb.position + fixedUpdate_moveStep);

            GroundBufferUpdate(Time.fixedDeltaTime);

            buffer_airSpeedLimit = buffer_groundSpeedLimit = Mathf.Infinity;

            controlledMovement_lastFixedUpdate = controlledMovement / Time.fixedDeltaTime;
            unrestrictedMovement_lastFixedUpdate = unrestrictedMovement / Time.fixedDeltaTime;
            slideVelocity_lastFixedUpdate = slideVelocity;

            controlledMovement = Vector3.zero;
            unrestrictedMovement = Vector3.zero;

            up = transform.up;
        }


        #endregion

        #region Physics Updates

        Vector3 GravityDirection
        {
            get
            {
                if (sphereTest)
                {
                    Vector3 directionTowardsSphere = (transform.position - sphereTest.position).normalized * sphereMultiplier;

                    return directionTowardsSphere;
                }
                else
                {
                    return gravityDirection;
                }

            }
        }

        bool buffer_clearGravityThisFrame = true;
        void GravityScaleUpdate()
        {
            current_gravityScale = 0;
            if (!isGroundedRaw)
                current_gravityScale = RelativeVelocity.y > 0 ? buffer_ascent_gravityScale : buffer_descent_gravityScale;


            if (buffer_clearGravityThisFrame)
            {
                //*Reset gravity buffers
                buffer_descent_gravityScale = descent_gravityScale;
                buffer_ascent_gravityScale = ascent_gravityScale;
            }
            buffer_clearGravityThisFrame = true;
        }

        // Vector3 UpdateGravityCalculation()
        // {
        //     Vector3 gravity = Vector3.zero;
        //     current_gravityScale = 0;
        //     if (!isGroundedRaw)
        //     {
        //         //* Apply gravity. Gravity is multiplied by deltaTime twice (once here, once when used in velocity with our final MovePosition())
        //         //* This is because gravity should be applied as an acceleration (ms^-2)
        //         current_gravityScale = transform.InverseTransformPoint(velocity).y > 0 ? buffer_ascent_gravityScale : buffer_descent_gravityScale;
        //         gravity = GravityDirection * Physics.gravity.y * current_gravityScale * Time.deltaTime;
        //     }

        //     //*Reset gravity buffers
        //     buffer_descent_gravityScale = descent_gravityScale;
        //     buffer_ascent_gravityScale = ascent_gravityScale;

        //     return gravity;
        // }

        void UpdatePlatformParent(bool groundCastHit)
        {
            if (groundCastHit && !cached_groundCastHitInfo.rigidbody)
            {
                PlatformParent = cached_groundCastHitInfo.transform;

                platformParent_velocity = PlatformParent.position - platformParent_lastPosition;

                platformParent_rotationDelta = platformParent_lastRotation.Difference(PlatformParent.rotation);

                if (platformParent_rotationDelta != Quaternion.identity)
                {
                    rb.position = rb.position.RotateAroundPivot(platformParent_lastPosition, platformParent_rotationDelta);

                    //*Rotate our camera only on our local up axis
                    platformParent_rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
                    transform.Rotate(transform.up, angle * Vector3.Dot(axis, transform.up), Space.World);
                }


                platformParent_lastPosition = PlatformParent.position;
                platformParent_lastRotation = PlatformParent.rotation;
            }
            else
            {
                PlatformParent = null;
                platformParent_rotationDelta = Quaternion.identity;
            }

            rb.position += platformParent_velocity;
        }


        Vector3 UpdateSlideVelocity(bool groundCastHit, float deltaTime)
        {
            bool onSlope = groundCastHit && IsNormalSlidable(cached_groundCastHitInfo.normal);
            float drag = slideDrag;

            //* Keep us at the peak of our slide while we are still on the slope
            if (onSlope)
                slideVelocity = GetSlopeForceFromNormal(cached_groundCastHitInfo.normal, transform.up, slideSpeed);

            //* Limits our move speed while we are sliding and attempting to move against it
            if (Vector3.Dot(controlledMovement, slideVelocity) < -slideVelocity.magnitude * .5f)
            {
                controlledMovement = Vector3.ClampMagnitude(controlledMovement, manualMoveLimitWhileSliding);
            }
            //* Increase drag if we are moving after the slide
            else if (!onSlope && groundCastHit)
            {
                drag *= 2;
            }

            slideVelocity = ApplyDrag(slideVelocity, drag, deltaTime);
            return slideVelocity * deltaTime;
        }

        void GroundBufferUpdate(float deltaTime)
        {
            bool groundedThisFrame = isGroundedRaw;
            if (groundedThisFrame != buffer_isGrounded)
            {
                //* Gives us a bit of coyote time here if we just left the ground, allowing the player to jump for a fraction longer
                if (groundedThisFrame || current_groundCoyoteTime >= groundCoyoteTime)
                {
                    buffer_isGrounded = groundedThisFrame;
                    onGroundStatusChange?.Invoke(groundedThisFrame);
                    current_groundCoyoteTime = 0;
                }
                else
                    current_groundCoyoteTime += deltaTime;
            }
            else
                current_groundCoyoteTime = 0;
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        [Header("Gizmos")]
        [SerializeField] float gizmoCurvepointTimeGap = .2f;
        float lastGizmoCurvepointTime = 0;
        List<Vector3> gizmoCurvepoints = new List<Vector3>();
        bool gizmos_clearWhenLeaveGround = false;
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            if (isGrounded)
            {
                lastGizmoCurvepointTime = 0;
                gizmos_clearWhenLeaveGround = true;
            }
            else
            {
                if (gizmos_clearWhenLeaveGround)
                {
                    gizmos_clearWhenLeaveGround = false;
                    gizmoCurvepoints.Clear();
                }

                float nextTime = lastGizmoCurvepointTime + gizmoCurvepointTimeGap;
                if (Time.time > nextTime)
                {
                    lastGizmoCurvepointTime = nextTime;
                    gizmoCurvepoints.Add(transform.position);
                }
            }

            Gizmos.color = Color.blue;
            for (int p = gizmoCurvepoints.Count - 1; p > 0; p--)
            {
                Gizmos.DrawLine(gizmoCurvepoints[p], gizmoCurvepoints[p - 1]);
            }

            if (velocityCast_allocation.distance > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(velocityCast_allocation.point, .1f);
            }


            Vector3 pos = GroundCastCenter;
            //float dist = CastDistance;
            float radius = GroundCastRadius;
            Vector3 down = -transform.up;

            // + down * dist
            Gizmos.color = isGroundedRaw ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(GroundCastCenter, radius);
            Gizmos.DrawWireSphere(GroundCastCenter + down * groundCastDistance, radius);
            // Debug.DrawRay(pos, down * dist, isGroundedRaw ? Color.yellow : Color.red);
            // for (float deg = 0; deg < 360; deg += groundDegreeCastStep)
            // {
            //     Vector2 degVector2 = UMath.DegreeToVector2(deg);
            //     Vector3 degreePos = transform.TransformDirection(new Vector3(degVector2.x, 0, degVector2.y)) * push;
            //     Debug.DrawRay(pos + degreePos, down * dist, isGroundedRaw ? Color.yellow : Color.red);
            // }
        }
#endif

        #endregion
    }
}