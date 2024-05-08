using UnityEngine;
using Than.Input;

namespace Than.Physics3D
{
    [DefaultExecutionOrder(0)]
    public class Crouch : PhysicsBodyModule
    {
        #region Public Properties and Events

        public bool crouched { get; private set; } = false;

        #endregion

        #region Inspector Fields

        public Brain brain;

        [Space(10)]
        public bool allowCrouchInAir = true;
        public bool stayCrouchedWhenLeavingGround = true;

        [Space(10)]
        [Tooltip("Manual move speed limit for crouch ground movement")] public float grounded_speedLimit = 10f / 3f;
        [Tooltip("Manual move speed limit for crouch air movement.")] public float aerial_speedLimit = Mathf.Infinity;

        [Space(10)]
        [Tooltip("Transform that is animated when crouching. Usually the camera")] public Transform head;
        [Tooltip("Animation performed when crouching / un-crouching.")] public AnimationCurve head_crouchCurve = AnimationCurve.Linear(0, 0, .1f, .5f);

        [Space(10)]
        [Tooltip("Change the main collider's height when crouched.")] public float mainColliderCrouchedHeight = 1.1f;
        [Tooltip("Capsule colliders that will be resized when crouching.")] public CrouchCollider[] affectedColliders;

        #endregion

        #region Other Variables and Properties

        float default_headY;
        int len_affectedColliders;
        CrouchCollider mainCrouchCollider;
        Collider[] headSpaceObstructions = new Collider[10];

        #endregion

        #region Control Methods

        private void StartCrouch() => StartCrouch(true);
        private void StartCrouch(bool playAnimation)
        {
            if (crouched)
                return;

            if (!allowCrouchInAir && !physicsBody.isGrounded)
                return;

            crouched = true;
            mainCrouchCollider.SetCrouch(crouched);
            for (int i = 0; i < len_affectedColliders; i++)
                affectedColliders[i].SetCrouch(crouched);

            StopAllCoroutines();

            if (playAnimation)
                StartCoroutine(head_crouchCurve.Animate(SetHeadHeightOffset));
            else
                SetHeadHeightOffset(head_crouchCurve.LastValue());
        }

        private void StopCrouch() => StopCrouch(true);
        private void StopCrouch(bool playAnimation)
        {
            if (!crouched)
                return;

            if (CheckHeadspaceObstructed())
                return;

            crouched = false;
            mainCrouchCollider.SetCrouch(crouched);
            for (int i = 0; i < len_affectedColliders; i++)
                affectedColliders[i].SetCrouch(crouched);

            StopAllCoroutines();

            if (playAnimation)
                StartCoroutine(head_crouchCurve.AnimateReverse(SetHeadHeightOffset));
            else
                SetHeadHeightOffset(0);
        }

        #endregion

        #region Unity Methods

        void Start()
        {
            mainCrouchCollider = new CrouchCollider(physicsBody.capsuleCollider);
            mainCrouchCollider.crouchedHeight = mainColliderCrouchedHeight;
            len_affectedColliders = affectedColliders.Length;
            for (int i = 0; i < len_affectedColliders; i++)
                affectedColliders[i].Setup();

            default_headY = head.transform.localPosition.y;
        }

        void OnEnable()
        {
            brain.Crouch.onPress += StartCrouch;
            physicsBody.onGroundStatusChange += GroundStatusChanged;
        }

        void OnDisable()
        {
            brain.Crouch.onPress -= StartCrouch;
            physicsBody.onGroundStatusChange -= GroundStatusChanged;
            StopAllCoroutines();
            StopCrouch(false);
        }

        void Update()
        {
            if (crouched)
            {
                physicsBody.ApplyMoveSpeedLimit_Ground(grounded_speedLimit);
                physicsBody.ApplyMoveSpeedLimit_Air(aerial_speedLimit);

                if (!brain.Crouch)
                    StopCrouch();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && !crouched && mainCrouchCollider != null)
            {
                mainCrouchCollider.crouchedHeight = mainColliderCrouchedHeight;
                mainCrouchCollider.Setup();
            }
        }

#endif

        #endregion

        #region Helper Functions

        private void GroundStatusChanged(bool grounded)
        {
            if (grounded)
            {
                if (brain.Crouch)
                    StartCrouch();
            }
            else
            {
                //*Need to poll this continuously in case there are obstructions when crouch is first released
                if (!allowCrouchInAir || !stayCrouchedWhenLeavingGround)
                    StopCrouch();
            }
        }

        bool CheckHeadspaceObstructed()
        {
            Vector3 center = physicsBody.capsuleCollider.center;
            center.y = mainCrouchCollider.defaultCenterY;
            Vector3 pos = transform.position + center;
            Vector3 offset = transform.up * (mainCrouchCollider.defaultHeight * .5f - physicsBody.capsuleCollider.radius);
            int collisionSize = Physics.OverlapSphereNonAlloc(pos + offset, physicsBody.capsuleCollider.radius, headSpaceObstructions, physicsBody.layerMask);

            for (int i = 0; i < collisionSize; i++)
            {
                if (headSpaceObstructions[i].transform.UnderParent(transform))
                    collisionSize--;
            }

            return collisionSize > 0;
        }

        void SetHeadHeightOffset(float offset)
        {
            head.transform.localPosition = new Vector3(head.transform.localPosition.x, default_headY - offset, head.transform.localPosition.z);
        }

        #endregion

        #region Internal Classes

        [System.Serializable]
        public class CrouchCollider
        {
            public CapsuleCollider collider;
            public CharacterController controller { get; private set; }
            bool isCharacterController = false;
            public float crouchedHeight = 1.1f;

            public float defaultHeight { get; private set; }
            public float defaultCenterY { get; private set; }

            void SetHeightAndCenter(float height, float centerPivot)
            {
                if (isCharacterController)
                {
                    controller.height = height;
                    controller.center = new Vector3(controller.center.x, centerPivot, controller.center.z);
                }
                else
                {
                    collider.height = height;
                    collider.center = new Vector3(collider.center.x, centerPivot, collider.center.z);
                }
            }

            public CrouchCollider(CharacterController controller)
            {
                this.controller = controller;
                isCharacterController = true;
                Setup();
            }

            public CrouchCollider(CapsuleCollider collider)
            {
                this.collider = collider;
                isCharacterController = false;
                Setup();
            }

            public void Setup()
            {
                if (isCharacterController)
                {
                    defaultHeight = controller.height;
                    defaultCenterY = controller.center.y;
                }
                else
                {
                    defaultHeight = collider.height;
                    defaultCenterY = collider.center.y;
                }
            }

            public void SetCrouch(bool crouch)
            {
                float height = crouch ? crouchedHeight : defaultHeight;
                float centerPivot = crouch ? (defaultCenterY - (defaultHeight - crouchedHeight) * .5f) : defaultCenterY;

                SetHeightAndCenter(height, centerPivot);
            }
        }

        #endregion
    }
}