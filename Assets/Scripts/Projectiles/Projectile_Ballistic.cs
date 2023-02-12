using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Than.Physics3D;

namespace Than.Projectiles
{
    public class Projectile_Ballistic : Projectile
    {
        public float force = 30;
        public float hitForceMultiplier = 1;

        public Vector3 velocity { get { return m_vel; } set { m_vel = value; current_direction = value.normalized; } }
        Vector3 m_vel;
        public Vector3 gravity;
        public float drag = 0;
        [Min(0)] public float reflectForce = 1;
        [Min(.01f)] public float correctionDistance = (float)UMath.goldenRatio;

        //*realign the raycast to always start from the barrel, since the projectile can be seen by the player
        protected override bool ShootsFromCrosshair => false;


        Vector3 barrel_visualPosition;
        Vector3 crosshair_visualPosition;
        float shot_correctionRate;

        protected override void OnShootAction(ShootData shootData)
        {
            dist = 0;
            barrel_visualPosition = shootData.Barrel_positionStart;
            crosshair_visualPosition = shootData.Crosshair_positionStart;
            shot_correctionRate = 1 / correctionDistance;
            velocity = force * shootData.shootDirection;
        }

        float dist = 0;
        float threshold_reflectedTime = .05f;
        void Update()
        {
            if (dead)
                return;

            Debug.DrawLine(transform.position, crosshair_visualPosition, Color.blue, 1f);
            Debug.DrawLine(current_castStepPoint, transform.position, Color.red, 1f);

            //* Apply gravity. Gravity is multiplied by deltaTime twice
            //* This is because gravity should be applied as an acceleration (ms^-2)
            Vector3 g = gravity * Time.deltaTime;
            Vector3 v = (velocity + gravity) * Time.deltaTime;


            Vector3 nextPosition;

            if (current_reflects == 0)
            {
                barrel_visualPosition += v;
                crosshair_visualPosition += v;

                nextPosition = Vector3.Lerp(barrel_visualPosition, crosshair_visualPosition, UMath.EaseOut(Mathf.Clamp01(dist * shot_correctionRate)));
            }
            else
                nextPosition = transform.position + v;



            Vector3 adjustedMoveStep = nextPosition - current_castStepPoint;
            Vector3 adjustedMoveStep_dir = adjustedMoveStep.normalized;
            float adjustedMoveStep_magnitude = adjustedMoveStep.magnitude;
            int hits = Hitscan(current_castStepPoint, adjustedMoveStep_dir, adjustedMoveStep_magnitude);

            if (hits > 0)
            {
                HitData lastHit = cached_hitData[hits - 1];
                current_castStepPoint = lastHit.projectileHitPoint;

                bool reflect = current_reflects < reflects && CanReflect(lastHit);
                if (reflect)
                {
                    velocity = HitReflect(v, lastHit).normalized * velocity.magnitude * reflectForce;
                }
                else
                    Die();
            }
            else
            {
                current_castStepPoint = nextPosition;
                dist += v.magnitude;
            }

            //TODO change this to work with fixedUpdate
            m_vel = PhysicsBody.ApplyDrag(velocity, drag, Time.deltaTime);
            transform.position = current_castStepPoint;

            if (dist > maxShootDistance)
                Die();
        }

        protected override Vector3 GetProjectileHitForce(HitData data)
        {
            return velocity * hitForceMultiplier;
        }
    }
}
