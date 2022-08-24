using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class RecoilBody : MonoBehaviour
    {
        Vector3 current_recoilTarget;
        float current_snappiness;
        float current_returnSpeed;

        private void OnDisable()
        {
            StopAllCoroutines();
            recoilRunning = false;
        }

        [System.Serializable]
        public class Settings
        {
            public RecoilBody recoilBody;
            public Vector3 force = new Vector3(6, 6, .6f);
            public float snappiness = 6;
            public float returnSpeed = 6;
            public float aimMultiplier = 0.3333f;
            public float moveMultiplier = 1.2f;

            public Settings(Vector3 force, float snappiness, float returnSpeed, float aimMultiplier, float moveMultiplier)
            {
                this.force = force;
                this.snappiness = snappiness;
                this.returnSpeed = returnSpeed;
                this.aimMultiplier = aimMultiplier;
                this.moveMultiplier = moveMultiplier;
            }

            public void ExecuteRecoil(bool isAiming, bool isMoving)
            {
                Vector3 f = force;
                if (isAiming)
                    f *= aimMultiplier;
                if (isMoving)
                    f *= moveMultiplier;

                recoilBody?.ExecuteRecoil(f, snappiness, returnSpeed);
            }
        }

        public void ExecuteRecoil(Vector3 force, float snappiness, float returnSpeed)
        {
            current_recoilTarget += new Vector3(-force.x, Random.Range(-force.y, force.y), Random.Range(-force.z, force.z));
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
