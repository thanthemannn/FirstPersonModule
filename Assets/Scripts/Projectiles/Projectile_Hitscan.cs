using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class Projectile_Hitscan : Projectile
    {
        public float hitForce = 10;

        protected override Vector3 GetProjectileHitForce(HitData data)
        {
            return data.shotDirection * hitForce;
        }

        //const float infinity_maxVisualDistance = 100;

        // public LineRenderer lineRenderer;
        // bool hasLineRenderer = false;

        // void Awake()
        // {
        //     hasLineRenderer = lineRenderer;
        // }



        protected override void OnShootAction(ShootData shootData)
        {
            float dist = maxShootDistance;
            while (dist > 0)
            {
                Vector3 endPosition = current_castStepPoint + current_direction * dist;
                int hits = Hitscan(current_castStepPoint, current_direction, dist);

                if (hits > 0)
                {
                    HitData lastHitData = cached_hitData[hits - 1];
                    transform.position = lastHitData.projectileHitPoint;
                    dist -= lastHitData.raycast.distance;

                    bool reflect = current_reflects < reflects && CanReflect(lastHitData) && dist > 0;
                    if (reflect)
                    {
                        current_castStepPoint = transform.position;
                        current_direction = HitReflect(current_direction, lastHitData).normalized;
                    }
                    else
                        break;
                }
                else
                {
                    transform.position = transform.position + current_direction * maxShootDistance;
                    break;
                }
            }

            Die();
        }

        // protected override void OnHitAction(HitData hitData, bool willDieAfterHit)
        // {
        //     RenderPositionUpdate(false);
        // }

        // void RenderPositionUpdate(bool addExtraPoint = false)
        // {
        //     Vector3[] positions = BuildPointData(addExtraPoint);
        //     RefreshLineRenderer(positions);
        // }


        // Vector3[] BuildPointData(bool addExtraPoint = false)
        // {
        //     int hitCount = currentHits.Count;
        //     int positionCount = 1 + hitCoualldExtraPoint.ToInt();

        //     Vector3[] positions = new Vector3[positionCount];

        //     positions[0] = hitCountallcurrentHits[0].shotStartPoint : current_shotStartPoint;

        //     int index;
        //     for (index = 1; index <= hitCount; index++)
        //         positions[index] = currentHits[index - 1].point;

        //     if (index < positionCount)
        //     {
        //         //*Set up last shot
        //         float visualDist = infinity_maxVisualDistance;
        //         if (maxShootDistance < Mathf.Infinity)
        //             visualDist = maxShootDistance;
        //         positions[index] = current_shotStartPoint + current_shotDirection * visualDist;
        //     }

        //     return positions;
        // }

        // void RefreshLineRenderer(Vector3[] positions)
        // {
        //     if (!hasLineRenderer)
        //         return;

        //     lineRenderer.enabled = true;
        //     lineRenderer.positionCount = positions.Length;
        //     lineRenderer.SetPositions(positions);
        // }
    }
}