using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Than.Input;

namespace Than.Physics3D
{
    public class EdgeGrab : MonoBehaviour
    {
        PhysicsBody pb;

        [Header("General")]
        public Brain brain;
        [Tooltip("Transform that is used to represent the head / eye level. Usually the camera")] public Transform head;

        [Tooltip("How far beyond the collider radius to check for safe clamber edges.")] public float edgeCheckDistance = .25f;

        [Tooltip("How far above our head should we begin the edge check.")] public float edgeCheckYOffset = .25f;

        [Tooltip("How long is the edge check line that begins near the objects's head and moves relative downward.")] public float edgeCheckHeight = 1f;

        public float edgeGrabSnapOffset = 0.1f;

        void Awake()
        {
            pb = GetComponent<PhysicsBody>();
        }

        bool ClamberInputHeld => brain.Jump.held && !brain.Crouch.held;

        // Update is called once per frame
        void Update()
        {
            if (ClamberInputHeld && !pb.isGrounded && EdgeCheck())
            {
                ActivateClamber();
            }
        }


        bool EdgeCheck()
        {
            Vector3 relUp = transform.up;
            Vector3 relFwd = transform.forward;

            float dist = pb.capsuleCollider.radius + edgeCheckDistance;

            Vector3 downStart = head.transform.position + (relUp * edgeCheckYOffset) + (relFwd * dist);
            Vector3 downEnd = downStart - (relUp * edgeCheckHeight);

            RaycastHit downHit;
            bool hitDown = Physics.Linecast(downStart, downEnd, out downHit, pb.layerMask);
            Debug.DrawLine(downStart, downEnd, Color.grey);

            if (hitDown)
            {
                Vector3 fwdEnd = downHit.point - (relUp * edgeGrabSnapOffset);
                Vector3 fwdStart = fwdEnd - (relFwd * dist);

                Debug.DrawLine(downStart, downHit.point, Color.green);
                Debug.DrawLine(fwdStart, fwdEnd, Color.green);

                //TODO finish casting and add edge scan / grab
            }

            return false;
        }

        void ActivateClamber()
        {

        }

        // void OnDrawGizmosSelected()
        // {
        //     Gizmos.color = Color.blue;

        //     Gizmos.Draw
        // }
    }
}