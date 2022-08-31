#if DEVELOPMENT_BUILD || UNITY_EDITOR
#define ALLOW_DEVMODE
#endif

using System;
using UnityEngine;
using Than.Input;

namespace Than.Physics3D
{
    [DefaultExecutionOrder(2)]
    [RequireComponent(typeof(PhysicsBody))]
    public class Movement : MonoBehaviour
    {
        #region  Public Properties and Events
        public PhysicsBody pb { get; private set; }

        public bool sprinting { get; private set; } = false;
        public System.Action<bool> onSprintStateChanged;
        public float SprintRemainingPercent => 1 - current_sprintTime / max_staminaTime;
        public bool IsSprintInfinite => max_staminaTime == Mathf.Infinity;
        public float current_sprintTime { get; private set; } = 0;

        public enum SprintState { none, sprint, cooldown, recharge }
        public SprintState current_sprintState { get; private set; }

        #endregion

        #region Inspector Fields

        [Header("General")]
        public Brain brain;
        [Tooltip("Projects our 2D movement to a 3D vector based of the relation with the body and this transform. Usually a 3rd person camera would fit here.")]
        public Transform projectionRelativeTransform;

        [Header("Movement")]
        [Tooltip("Max speed while moving normally.")]
        public float moveSpeed = 8f;

        [Tooltip("How fast to get to current input from last input.")]
        public float move_accelerationRate = 12f;

        [Tooltip("How fast to get from last input to neutral.")]
        public float move_deccelerationRate = 10f;

        [Tooltip("Higher value: More air control.\nLower value: More realistic.")]
        public float air_accelerationRate = 5f;

        [Tooltip("Higher value: More air control.\nLower value: Less air drag.")]
        public float air_deccelerationRate = 5f;

        [Header("Sprint")]
        [Tooltip("If true, body will always sprint when able, unless the sprint button is held.")]
        public bool invert_sprintButton = false;

        [Space(10)]
        [Tooltip("Max speed while sprinting.")]
        public float sprintSpeed = 12f;

        [Tooltip("The rate which we interpolate between our regular moveSpeed and our sprintSpeed")]
        public float sprint_accelerationRate = 5f;

        [Space(10)]
        [Tooltip("Should you be able to begin a sprint while airborne?")]
        public bool canStartSprintInAir = true;

        [Tooltip("Our input axis must be stronger than this magnitude for sprint input to be considered")]
        [Range(0, 1)] public float minSprintInputMagnitude = .5f;

        [Tooltip("If our input is outside of the range, then we should not sprint (relative to body forward).")]
        [Range(0, 180)] public float allowedSprintArc = 180;

        [Header("Stamina")]
        [Tooltip("How long the body can sprint for before needing to recharge stamina. [0 - Infinity]")]
        [Min(0)] public float max_staminaTime = Mathf.Infinity;

        [Tooltip("How fast stamina can recharge.\n(eg. Value of 2 is 2x as fast as sprint consumption)")]
        public float rate_staminaRecharge = 2;

        [Tooltip("How long after sprinting should it take for it to start recharging stamina.")]
        public float cooldown_staminaRecharge = .5f;

        #endregion

        #region Other Properties and Variables

        float currentCooldown_sprintRecharge = 0;
        Vector2 inputBuffer;
        float speedTimeBuffer;
        RaycastHit groundHitInfo;

        #endregion

        #region Helper Functions

        public SprintState GetSprintState()
        {
            if (sprinting)
                return SprintState.sprint;
            else if (currentCooldown_sprintRecharge < cooldown_staminaRecharge)
                return SprintState.cooldown;
            else if (current_sprintTime > 0)
                return SprintState.recharge;
            else
                return SprintState.none;
        }

        #endregion

        #region Unity Methods

        void Awake()
        {
            pb = GetComponent<PhysicsBody>();
        }

        void OnEnable()
        {
            speedTimeBuffer = 0;
            inputBuffer = Vector2.zero;
        }

        void Update()
        {
            //* Input acceleration / deceleration when we are in air / on ground
            float rate;
            if (brain.Move.held)
                rate = pb.isGrounded ? move_accelerationRate : air_accelerationRate;
            else
                rate = pb.isGrounded ? move_deccelerationRate : air_deccelerationRate;

            inputBuffer = Vector2.MoveTowards(inputBuffer, brain.Move, Time.deltaTime * rate);

            //*Update our sprint status and associated values/events
            SprintLogicUpdate();

            //*If we have no input, the rest of this function does not need to be performed
            if (inputBuffer == Vector2.zero)
                return;

            //*Base our current speed based on an interpolation between move and sprint speed values
            speedTimeBuffer = Mathf.Clamp01(speedTimeBuffer + (sprinting.ToSign() * sprint_accelerationRate * Time.deltaTime));
            float current_speed = Mathf.Lerp(moveSpeed, sprintSpeed, speedTimeBuffer);

            //*Adjust our movement to compensate for any slopes we may be on
            Vector3 normal = transform.up;
            // if (pb.GroundCast(out groundHitInfo))
            //     normal = groundHitInfo.normal;

            //*Translate our 2D input to a 3D direction based off of our body relative to our projectionRelativeTransform (which is usually the camera)
            Transform transformSource = projectionRelativeTransform ? projectionRelativeTransform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(transformSource.forward, normal).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transformSource.right, normal).normalized;

            //*Apply our movement values
            Vector3 moveDirection = forward * inputBuffer.y + right * inputBuffer.x;
            if (sprinting)
                moveDirection = moveDirection.normalized;

            //*Move in direction * speed (velocity)
            pb.Move(moveDirection * current_speed);
        }

        #endregion

        #region Physics Updates 

        void SprintLogicUpdate()
        {
            bool buffer_sprint = sprinting;
            sprinting = invert_sprintButton ^ brain.Sprint;

            if (inputBuffer == Vector2.zero) //* No movement = no sprint
                sprinting = false;
            else if (!buffer_sprint && !canStartSprintInAir && !pb.isGrounded) //* Started sprinting in the air, and whether that's allowed or not
                sprinting = false;
            else if (current_sprintTime >= max_staminaTime) //* Our sprint is maxed out
                sprinting = false;
            else if (Mathf.Abs(Vector2.SignedAngle(Vector2.up, inputBuffer)) > allowedSprintArc) //* Don't allow us to sprint outside of the allowed facing direction arc
                sprinting = false;
            else if (pb.isGrounded ? (pb.Current_GroundSpeedLimit < sprintSpeed) : (pb.Current_AirSpeedLimit < sprintSpeed)) //* If our speed limit in the physicsbody is lower than our actual speed, we shouldn't sprint
                sprinting = false;
            else if (inputBuffer.magnitude < minSprintInputMagnitude) //* If our input is too weak, player probably shouldn't sprint
                sprinting = false;

            if (sprinting) //* Sprinting
            {
                currentCooldown_sprintRecharge = 0;
                current_sprintTime += Time.deltaTime;
            }
            else if (currentCooldown_sprintRecharge < cooldown_staminaRecharge) //* Sprint recharge is in cooldown
            {
                currentCooldown_sprintRecharge += Time.deltaTime;
            }
            else //* Sprint is recharging
            {
                current_sprintTime -= Time.deltaTime * rate_staminaRecharge;
            }
            //*Time value
            current_sprintTime = Mathf.Clamp(current_sprintTime, 0, max_staminaTime);

            //*Perform sprint state change events
            current_sprintState = GetSprintState();
            if (buffer_sprint != sprinting)
            {
                buffer_sprint = sprinting;
                onSprintStateChanged?.Invoke(sprinting);
            }
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR

        [Header("Gizmos")]
        [SerializeField] float gizmos_projectionMultiplier = 3;
        private void OnDrawGizmos()
        {
            if (projectionRelativeTransform)
            {
                Vector3 normal = transform.up;
                Vector3 forward = Vector3.ProjectOnPlane(projectionRelativeTransform.forward, normal).normalized;
                Vector3 pf = transform.position + forward * gizmos_projectionMultiplier;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, pf);
                Gizmos.DrawSphere(pf, .2f);

                Vector3 right = Vector3.ProjectOnPlane(projectionRelativeTransform.right, normal).normalized;
                Vector3 pr = transform.position + right * gizmos_projectionMultiplier;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, pr);
                Gizmos.DrawSphere(pr, .2f);
            }

        }

#endif

        #endregion
    }
}