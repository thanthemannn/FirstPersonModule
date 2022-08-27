using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class RecoilBody : MonoBehaviour
    {
        public Vector3 current_recoilTarget { get; private set; }
        public float current_snappiness { get; private set; }
        public float current_returnSpeed { get; private set; }
        private void OnDisable()
        {
            StopAllCoroutines();
            transform.localEulerAngles = Vector3.zero;
            recoilRunning = false;
        }

        [System.Serializable]
        public class Settings
        {
            public RecoilBody recoilBody;
            public float snappiness = 12;
            public float returnSpeed = 12;
            public float aimMultiplier = 0.3333f;
            public float moveMultiplier = 1.2f;


            [Header("Random Ranges")]
            public Vector3 forceRange_max = new Vector3(1, 1, .6f);
            public Vector3 forceRange_min = new Vector3(-.5f, -1, -.6f);


            public Vector3 last_randomValues { get; private set; }

            public Settings(Vector3 forceRange_max, Vector3 forceRange_min, float snappiness, float returnSpeed, float aimMultiplier, float moveMultiplier)
            {
                this.forceRange_max = forceRange_max;
                this.forceRange_min = forceRange_min;
                this.snappiness = snappiness;
                this.returnSpeed = returnSpeed;
                this.aimMultiplier = aimMultiplier;
                this.moveMultiplier = moveMultiplier;
            }

            public void ExecuteRecoil(bool isAiming, bool isMoving)
            {
                Vector3 rand = new Vector3(Random.value, Random.value, Random.value);
                ExecuteRecoil(isAiming, isMoving, rand);
            }

            public void ExecuteRecoil(bool isAiming, bool isMoving, Vector3 randomWeights)
            {
                last_randomValues = randomWeights;

                Vector3 appliedForce = new Vector3(
                    Mathf.Lerp(forceRange_min.x, forceRange_max.x, last_randomValues.x),
                    Mathf.Lerp(forceRange_min.y, forceRange_max.y, last_randomValues.y),
                    Mathf.Lerp(forceRange_min.z, forceRange_max.z, last_randomValues.z)
                );

                if (isAiming)
                    appliedForce *= aimMultiplier;
                if (isMoving)
                    appliedForce *= moveMultiplier;

                recoilBody?.ExecuteRecoil(appliedForce, snappiness, returnSpeed);
            }
        }

        public void ExecuteRecoil(Vector3 force, float snappiness, float returnSpeed)
        {
            current_recoilTarget += force;
            current_snappiness = snappiness;
            current_returnSpeed = returnSpeed;

            if (recoilRunning == false)
            {
                StopAllCoroutines();
                StartCoroutine(RecoilCoroutine());
            }
        }

        bool recoilRunning = false;
        IEnumerator RecoilCoroutine()
        {
            recoilRunning = true;

            Vector3 current_rotation = transform.localEulerAngles;
            while (current_recoilTarget != Vector3.zero)
            {
                current_recoilTarget = Vector3.Lerp(current_recoilTarget, Vector3.zero, current_returnSpeed * Time.deltaTime);
                current_rotation = Vector3.Slerp(current_rotation, current_recoilTarget, current_snappiness * Time.fixedDeltaTime);
                transform.localEulerAngles = current_rotation;
                yield return null;
            }

            transform.localEulerAngles = Vector3.zero;
            recoilRunning = false;
        }
    }
}
