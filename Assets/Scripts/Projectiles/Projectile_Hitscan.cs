using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class Projectile_Hitscan : Projectile
    {
        //const float infinity_maxVisualDistance = 100;

        // public LineRenderer lineRenderer;
        // bool hasLineRenderer = false;

        // void Awake()
        // {
        //     hasLineRenderer = lineRenderer;
        // }

        protected override Vector3 GetShotStartPosition(Vector3 aimPosition, Vector3 barrelPosition)
        {
            //*realign the raycast to always start from the aim center, for accuracy sake
            return aimPosition;
        }

        protected override void OnShootAction()
        {
            float dist = maxShootDistance;
            while (dist > 0)
            {
                current_stepPoint = transform.position;

                Vector3 endPosition = transform.position + current_direction * dist;
                int hits = Hitscan(current_stepPoint, current_direction, dist);

                if (hits > 0)
                {
                    HitData lastHitData = cached_hitData[hits - 1];
                    transform.position = lastHitData.point;
                    dist -= lastHitData.distanceFromShotStart;

                    bool reflect = current_reflects < reflects && CanReflect(lastHitData) && dist > 0;
                    if (reflect)
                        current_direction = GetHitReflect(lastHitData);
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