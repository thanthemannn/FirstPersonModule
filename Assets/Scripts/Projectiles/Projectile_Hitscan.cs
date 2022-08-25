using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class Projectile_Hitscan : Projectile
    {
        const float infinity_maxVisualDistance = 100;

        RaycastHit[] raycastAllocation = new RaycastHit[10];

        public LineRenderer lineRenderer;
        bool hasLineRenderer = false;

        public LayerMask layerMask { get; private set; }

        void Awake()
        {
            layerMask = gameObject.layer.GetLayerMaskFromCollisionMatrix();
            hasLineRenderer = lineRenderer;
        }

        protected override void ShootAction(Vector3 direction)
        {
            if (hasLineRenderer)
                lineRenderer.enabled = false;

            int hits;
            if (hitRadius > 0)
                hits = Physics.RaycastNonAlloc(current_shotStartPoint, current_shotDirection, raycastAllocation, maxShootDistance, layerMask);
            else
                hits = Physics.SphereCastNonAlloc(current_shotStartPoint, hitRadius, current_shotDirection, raycastAllocation, maxShootDistance, layerMask);

            for (int i = 0; i < hits; i++)
            {
                HitData hitData = new HitData(this, raycastAllocation[i]);
                if (TryHit(hitData))
                {
                    transform.position = hitData.point;
                    return;
                }
            }

            RenderPositionUpdate(true);
        }

        protected override void OnHitAction(HitData hitData, bool willDieAfterHit)
        {
            RenderPositionUpdate(false);
        }

        void RenderPositionUpdate(bool addExtraPoint = false)
        {
            Vector3[] positions = BuildPointData(addExtraPoint);
            RefreshLineRenderer(positions);
        }

        Vector3[] BuildPointData(bool addExtraPoint = false)
        {
            int hitCount = currentHits.Count;
            int positionCount = 1 + hitCount + addExtraPoint.ToInt();

            Vector3[] positions = new Vector3[positionCount];

            positions[0] = hitCount > 0 ? currentHits[0].shotStartPoint : current_shotStartPoint;

            int index;
            for (index = 1; index <= hitCount; index++)
                positions[index] = currentHits[index - 1].point;

            if (index < positionCount)
            {
                //*Set up last shot
                float visualDist = infinity_maxVisualDistance;
                if (maxShootDistance < Mathf.Infinity)
                    visualDist = maxShootDistance;
                positions[index] = current_shotStartPoint + current_shotDirection * visualDist;
            }

            return positions;
        }

        void RefreshLineRenderer(Vector3[] positions)
        {
            if (!hasLineRenderer)
                return;

            lineRenderer.enabled = true;
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
        }
    }
}