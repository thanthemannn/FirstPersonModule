using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Physics3D
{
    public class HeadMovement : MonoBehaviour
    {
        float current_headbobValue = 0;
        float current_headbobTime = 0;
        public float headBob_returnSpeed = 2;
        public float headBob_snappiness = 5;
        public AnimationCurve headBob = AnimationCurve.EaseInOut(0, 0, 1, .5f);
        public AnimationCurve headBob_speedCurve = AnimationCurve.EaseInOut(0, 2, 100, 8);
        public PhysicsBody pb;

        public Vector3 leanStrength = new Vector3(1, 2, 3);
        public float lean_returnSpeed = 3;
        public float lean_snappiness = 5;

        void Awake()
        {
            headBob.preWrapMode = WrapMode.PingPong;
            headBob.postWrapMode = WrapMode.PingPong;
            headBob_speedCurve.preWrapMode = WrapMode.Clamp;
            headBob_speedCurve.postWrapMode = WrapMode.Clamp;
        }

        Vector3 current_leanTime;

        void Update()
        {
            //*Calculate headbob
            float movementSqrMagnitude = pb.manualMovement_lastFixedUpdate.sqrMagnitude;
            if (movementSqrMagnitude < .1f || !pb.isGrounded)
            {
                current_headbobTime = 0;
                current_headbobValue = Mathf.MoveTowards(current_headbobValue, 0, Time.deltaTime * headBob_returnSpeed);
            }
            else
            {
                current_headbobValue = Mathf.MoveTowards(current_headbobValue, headBob.Evaluate(current_headbobTime), Time.deltaTime * headBob_snappiness);
                current_headbobTime += Time.deltaTime * headBob_speedCurve.Evaluate(movementSqrMagnitude);
            }



            //*Lean calculations
            Vector3 clampedMovement = Vector3.ClampMagnitude(transform.InverseTransformDirection(pb.LastMoveStep), 10) / 10;
            Vector3 leanMod = -Vector3.one;
            if (pb.isGrounded) //*don't lean up/down if we're on the ground
                leanMod.y = 0;
            Vector3 leanTime = Vector3.Scale(clampedMovement, leanMod);

            //*Lean mapping
            float speed = leanTime.sqrMagnitude < .01f ? lean_returnSpeed : lean_snappiness;
            current_leanTime = Vector3.MoveTowards(current_leanTime, leanTime, Time.deltaTime * speed);
            Vector3 lean = Vector3.Scale(leanStrength, current_leanTime);

            //*Apply rotation
            Vector3 rot = transform.localEulerAngles;
            rot.x = lean.y + current_headbobValue;
            rot.y = lean.x;
            rot.z = lean.z;
            transform.localEulerAngles = rot;
        }
    }
}
