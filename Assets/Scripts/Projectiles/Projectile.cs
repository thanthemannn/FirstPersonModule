using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Than.Projectiles
{
    public abstract class Projectile : MonoBehaviour
    {
        [Min(0)] public float hitRadius = 0;
        [Range(0, PROJECTILE_MAX_DISTANCE)] public float maxShootDistance = 1000;
        public const float PROJECTILE_MAX_DISTANCE = 1000;
        [HideInInspector] public Gun source;

        public System.Action<HitData> onHit;
        public System.Action<Projectile> onDeath;
        protected Collider[] colliders;
        int collidersLen = 0;
        bool collidersSetup = false;
        [Min(0)] public int reflects = 0;
        public int current_reflects { get; private set; } = 0;

        public float deathTime = 0;

        public Vector3 current_stepPoint { get; protected set; }
        public Vector3 current_direction { get; protected set; }
        public List<HitData> allHits { get; private set; } = new List<HitData>();

        public Vector3 InitialShotStartPoint => allHits.Count > 0 ? allHits[0].shotStartPoint : current_stepPoint;
        public Vector3 InitialShotDirection => allHits.Count > 0 ? allHits[0].shotDirection : current_direction;

        public float aliveTime = Mathf.Infinity;

        public const int HITBOX_LAYERMASK = 1 << 7;

        WaitForSeconds wait_aliveTime;

        public ParticleSystem hitParticle;
        bool hasHitParticle;

        public bool waitForParticlesBeforeDeath = true;
        bool hasInit = false;

        public LayerMask layerMask { get; private set; }
        public LayerMask canPenetrateLayers = HITBOX_LAYERMASK;

        RaycastHit[] cached_raycastAllocations;
        public HitData[] cached_hitData { get; private set; }
        int base_raycastSafetyAmount = 2;
        public int penetrateTargetCount = 0;

        void Start()
        {
            Init();
        }

        public void Init()
        {
            if (hasInit)
                return;

            hasInit = true;

            hasHitParticle = hitParticle;
            layerMask = gameObject.layer.GetLayerMaskFromCollisionMatrix();
            int castSize = base_raycastSafetyAmount + penetrateTargetCount;
            cached_raycastAllocations = new RaycastHit[castSize];
            cached_hitData = new HitData[castSize];

            if (aliveTime < Mathf.Infinity)
                wait_aliveTime = new WaitForSeconds(aliveTime);
        }

        public int Hitscan(Vector3 position, Vector3 direction, float distance)
        {
            int hits;
            if (hitRadius > 0)
                hits = Physics.RaycastNonAlloc(position, direction, cached_raycastAllocations, distance, layerMask);
            else
                hits = Physics.SphereCastNonAlloc(position, hitRadius, direction, cached_raycastAllocations, distance, layerMask);

            int validatedHits = 0;
            for (int i = 0; i < hits; i++)
            {
                HitData hitData = new HitData(this, cached_raycastAllocations[i]);
                if (CanHit(hitData))
                {
                    cached_hitData[validatedHits] = hitData;
                    validatedHits++;

                    Hit(hitData);

                    if (!CanPenetrate(validatedHits, hitData))
                    {
                        break;
                    }
                }
            }

            return validatedHits;
        }

        bool CanPenetrate(int currentHits, HitData hitData)
        {
            return penetrateTargetCount >= currentHits && ((1 << hitData.collider.gameObject.layer) & canPenetrateLayers) != 0;
        }

        float startTime = 0;
        public virtual void Setup(Gun source)
        {
            startTime = Time.time;
            StopAllCoroutines();
            this.source = source;
            transform.position = source.projectile_gunOffsetTransform.position;
            transform.rotation = source.transform.rotation;
            current_reflects = 0;
            dead = false;
            allHits.Clear();

            SetupCollisionIgnores();
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        void OnDrawGizmos()
        {
            DrawProjectileGizmos(transform.position);
        }

        public void DrawProjectileGizmos(Vector3 position)
        {
            Gizmos.color = Color.red;
            if (hitRadius > 0)
                Gizmos.DrawWireSphere(position, hitRadius);
            else
                Gizmos.DrawSphere(position, .025f);
        }

        void SetupCollisionIgnores()
        {
            if (!collidersSetup && source.physicsBody)
            {
                collidersSetup = true;
                colliders = GetComponentsInChildren<Collider>();
                collidersLen = colliders.Length;


                for (int i = 0; i < collidersLen; i++)
                {
                    for (int j = 0; j < source.physicsBody.attachedCollider_len; j++)
                    {
                        Physics.IgnoreCollision(colliders[i], source.physicsBody.attachedColliders[j]);
                    }
                }
            }
        }

        protected abstract Vector3 GetShotStartPosition(Vector3 aimPosition, Vector3 barrelPosition);

        public void Shoot(Vector3 aimPosition, Vector3 barrelPosition, Vector3 direction)
        {
            current_stepPoint = transform.position = GetShotStartPosition(aimPosition, barrelPosition);
            current_direction = direction;

            if (aliveTime < Mathf.Infinity)
            {
                if (deathCountdownCoroutine != null)
                    StopCoroutine(deathCountdownCoroutine);

                deathCountdownCoroutine = DeathCountdown();
                StartCoroutine(DeathCountdown());
            }

            OnShootAction();
        }

        IEnumerator deathCountdownCoroutine;
        IEnumerator DeathCountdown()
        {
            yield return wait_aliveTime;
            Die();

            deathCountdownCoroutine = null;
        }

        protected virtual void OnShootAction() { }

        protected bool ColliderAllowed(Collider collider)
        {
            return !collider.transform.UnderParent(source.bodyRoot);
        }


        public enum HitResult { reflect, die }

        public bool CanHit(HitData hitData)
        {
            if (!ColliderAllowed(hitData.collider))
                return false;

            return true;
        }

        protected void Hit(HitData hitData)
        {
            allHits.Add(hitData);

            OnHitAction(hitData);
            PerformParticleHit(hitData);
            onHit?.Invoke(hitData);
        }


        // protected int HitStep(float distance)
        // {
        //     (int hits, Vector3 finalPosition) = Hitscan(current_stepPoint, current_direction, distance);
        //     Vector3 startPosition = current_stepPoint;
        //     current_stepPoint = finalPosition;
        //     transform.position = finalPosition;

        //     if (hits > 0)
        //     {
        //         for (int i = 0; i < hits; i++)
        //         {
        //             Hit(cached_hitData[i]);
        //         }

        //         //* try to reflect off the last hit
        //         HitData lastHitData = cached_hitData[hits - 1];
        //         bool reflect = current_reflects < reflects && CanReflect(lastHitData);

        //         if (reflect)
        //         {
        //             current_direction = GetHitReflect(lastHitData);
        //             distance -= Vector3.Distance(startPosition, finalPosition);
        //             return HitStep(distance);
        //         }
        //     }

        //     return hits;
        // }

        void PerformParticleHit(HitData hitData)
        {
            if (!hasHitParticle)
                return;


            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = hitData.point;

            var burstData = hitParticle.emission.GetBurst(0);

            hitParticle.transform.rotation = Quaternion.FromToRotation(hitParticle.transform.up, hitData.normal) * hitParticle.transform.rotation;
            int particleCount = burstData.maxCount;
            if (burstData.minCount > 0)
                particleCount = Random.Range(burstData.minCount, burstData.maxCount + 1);

            hitParticle.Emit(emitParams, particleCount);
        }

        protected virtual void OnHitAction(HitData hitData) { }
        protected virtual void OnDeathStart() { }
        protected virtual void OnDeathEnd() { }

        protected virtual bool CanReflect(HitData hitData)
        {
            return true;
        }

        protected virtual Vector3 GetHitReflect(HitData hitData)
        {
            return Vector3.Reflect(hitData.shotDirection, hitData.normal).normalized;
        }

        bool destroyed = false;
        void OnDestroy()
        {
            destroyed = true;
        }

        protected bool dead = false;
        protected virtual async void Die()
        {
            OnDeathStart();
            dead = true;
            await Task.Delay((int)(deathTime * 1000));

            if (destroyed)
                return;

            if (waitForParticlesBeforeDeath && hasHitParticle)
            {
                while (!destroyed && hitParticle.IsAlive(true))
                {
                    await Task.Delay(100);
                }
            }

            if (destroyed)
                return;

            OnDeathEnd();
            onDeath?.Invoke(this);

            gameObject.SetActive(false);
        }

        public struct HitData
        {
            public Collider collider;
            public Vector3 point;
            public Vector3 normal;
            public Projectile projectile;
            public Vector3 shotDirection;
            public Vector3 shotStartPoint;
            public float distanceFromShotStart;

            // public HitData(Projectile projectile, Collider collider, Vector3 point, Vector3 normal)
            // {
            //     this.collider = collider;
            //     this.point = point;
            //     this.normal = normal;
            //     this.projectile = projectile;
            //     this.shotDirection = projectile.current_direction;
            //     this.shotStartPoint = projectile.current_stepPoint;
            // }

            public HitData(Projectile projectile, RaycastHit raycastHit)
            {
                this.collider = raycastHit.collider;
                this.point = raycastHit.point;
                this.normal = raycastHit.normal;
                distanceFromShotStart = raycastHit.distance;
                this.projectile = projectile;
                this.shotDirection = projectile.current_direction;
                this.shotStartPoint = projectile.current_stepPoint;
            }

            // public HitData(Projectile projectile, Collision collision)
            // {
            //     this.collider = collision.collider;
            //     var contact = collision.GetContact(0);
            //     this.point = contact.point;
            //     this.normal = contact.normal;
            //     this.projectile = projectile;
            //     this.shotDirection = projectile.current_direction;
            //     this.shotStartPoint = projectile.current_stepPoint;
            // }
        }
    }
}