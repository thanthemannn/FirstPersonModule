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

        public Vector3 current_castStepPoint { get; protected set; }
        public Vector3 current_direction { get; protected set; }
        public List<HitData> allHits { get; private set; } = new List<HitData>();

        public Vector3 InitialShotStartPoint => allHits.Count > 0 ? allHits[0].shotStartPoint : current_castStepPoint;
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
            return penetrateTargetCount >= currentHits && ((1 << hitData.raycast.collider.gameObject.layer) & canPenetrateLayers) != 0;
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

        protected virtual bool ShootsFromCrosshair => true;

        public struct ShootData
        {
            public Vector3 shootDirection;
            public Vector3 gunForward;

            public Vector3 barrel_castStart;
            public Vector3 crosshair_castStart;

            public Vector3 offset_positionStart;

            public Vector3 Barrel_positionStart => barrel_castStart + offset_positionStart;
            public Vector3 Crosshair_positionStart => crosshair_castStart + offset_positionStart;

            public (Vector3, Vector3) GetShotCastAndPositionStart(bool startFromCrosshair)
            {
                if (startFromCrosshair)
                    return (crosshair_castStart, Crosshair_positionStart);
                else
                    return (barrel_castStart, Barrel_positionStart);
            }


            public ShootData(Gun gun)
            {

                gunForward = gun.transform.forward;
                shootDirection = gunForward; //* This may be changed later

                Vector3 barrelWorld = gun.projectile_gunOffsetTransform.position;
                Vector3 barrelLocalOffset = gun.transform.InverseTransformPoint(barrelWorld);

                offset_positionStart = gunForward * barrelLocalOffset.z;
                barrel_castStart = barrelWorld - offset_positionStart;
                crosshair_castStart = gun.transform.position;
            }
        }

        public void Shoot(ShootData shootData)//Vector3 aimPosition, Vector3 barrelPosition, Vector3 direction)
        {
            (current_castStepPoint, transform.position) = shootData.GetShotCastAndPositionStart(ShootsFromCrosshair);
            current_direction = shootData.shootDirection;

            if (aliveTime < Mathf.Infinity)
            {
                if (deathCountdownCoroutine != null)
                    StopCoroutine(deathCountdownCoroutine);

                deathCountdownCoroutine = DeathCountdown();
                StartCoroutine(DeathCountdown());
            }

            OnShootAction(shootData);
        }

        IEnumerator deathCountdownCoroutine;
        IEnumerator DeathCountdown()
        {
            yield return wait_aliveTime;
            Die();

            deathCountdownCoroutine = null;
        }

        protected virtual void OnShootAction(ShootData shootData) { }

        protected bool ColliderAllowed(Collider collider)
        {
            return !collider.transform.UnderParent(source.bodyRoot);
        }


        public enum HitResult { reflect, die }

        public virtual bool CanHit(HitData hitData)
        {
            if (!ColliderAllowed(hitData.raycast.collider))
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

        void PerformParticleHit(HitData hitData)
        {
            if (!hasHitParticle)
                return;


            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = hitData.raycast.point;

            var burstData = hitParticle.emission.GetBurst(0);

            hitParticle.transform.rotation = Quaternion.FromToRotation(hitParticle.transform.up, hitData.raycast.normal) * hitParticle.transform.rotation;
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

        protected virtual Vector3 HitReflect(Vector3 direction, HitData hitData)
        {
            //Debug.Log(direction + " | " + hitData.raycast.normal);
            current_reflects++;
            return Vector3.Reflect(direction, hitData.raycast.normal);
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
            public Projectile projectile;
            public Vector3 shotDirection;
            public Vector3 shotStartPoint;

            public RaycastHit raycast;
            public Vector3 projectileHitPoint;
            public float time;

            public HitData(Projectile projectile, RaycastHit raycastHit)
            {
                time = Time.time;
                raycast = raycastHit;
                this.projectile = projectile;
                this.shotDirection = projectile.current_direction;
                this.shotStartPoint = projectile.current_castStepPoint;

                this.projectileHitPoint = shotStartPoint + shotDirection * raycastHit.distance;
            }
        }
    }
}