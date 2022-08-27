using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class Projectile_Ballistic : Projectile
    {
        public float force = 10;
        public float correctionRate = 5;
        public AnimationCurve correctionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        Vector3 aimPosition;
        protected override Vector3 GetShotStartPosition(Vector3 aimPosition, Vector3 barrelPosition)
        {
            dist = 0;
            this.aimPosition = aimPosition;
            //*realign the raycast to always start from the aim center, for accuracy sake
            return barrelPosition;
        }

        float dist = 0;
        void Update()
        {
            if (dead)
                return;

            float forceDist = force * Time.deltaTime;
            Vector3 dir = current_direction * forceDist;

            current_stepPoint = Vector3.Lerp(current_stepPoint, aimPosition, correctionCurve.Evaluate(dist * correctionRate));
            aimPosition += dir;

            int hits = Hitscan(current_stepPoint, current_direction, forceDist);

            if (hits > 0)
            {
                HitData lastHitData = cached_hitData[hits - 1];
                current_stepPoint = lastHitData.point;
                Die();
            }
            else
                current_stepPoint += dir;

            dist += forceDist;

            transform.position = current_stepPoint;
        }


    }
}
