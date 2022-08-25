using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Than.Projectiles
{
    public abstract class Projectile : MonoBehaviour
    {
        [Min(0)] public float hitRadius = 0;
        [Min(0)] public float maxShootDistance = Mathf.Infinity;

        [HideInInspector] public Gun source;

        public System.Action<HitData> onHit;
        public System.Action<Projectile> onDeath;
        protected Collider[] colliders;
        int collidersLen = 0;
        bool collidersSetup = false;
        [Min(1)] public int hitsBeforeDeath = 1;

        public float deathTime = 0;

        public Vector3 current_shotStartPoint { get; private set; }
        public Vector3 current_shotDirection { get; private set; }
        public List<HitData> currentHits { get; private set; } = new List<HitData>();

        public Vector3 InitialShotStartPoint => currentHits.Count > 0 ? currentHits[0].shotStartPoint : current_shotStartPoint;
        public Vector3 InitialShotDirection => currentHits.Count > 0 ? currentHits[0].shotDirection : current_shotDirection;

        public float aliveTime = Mathf.Infinity;

        WaitForSeconds wait_aliveTime;

        public ParticleSystem hitParticle;
        bool hasHitParticle;

        public bool waitForParticlesBeforeDeath = true;
        bool hasInit = false;

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

            if (aliveTime < Mathf.Infinity)
                wait_aliveTime = new WaitForSeconds(aliveTime);
        }

        float startTime = 0;
        public virtual void Setup(Gun source)
        {
            startTime = Time.time;
            StopAllCoroutines();
            this.source = source;
            transform.position = source.transform.position;
            transform.rotation = source.transform.rotation;

            currentHits.Clear();

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

        public void Shoot(Vector3 direction) => Shoot(transform.position, direction);
        public void Shoot(Vector3 position, Vector3 direction)
        {
            transform.position = position;
            current_shotStartPoint = position;
            current_shotDirection = direction;
            ShootAction(direction);

            if (aliveTime < Mathf.Infinity)
            {
                if (deathCountdownCoroutine != null)
                    StopCoroutine(deathCountdownCoroutine);

                deathCountdownCoroutine = DeathCountdown();
                StartCoroutine(DeathCountdown());
            }
        }

        IEnumerator deathCountdownCoroutine;
        IEnumerator DeathCountdown()
        {
            yield return wait_aliveTime;
            Die();

            deathCountdownCoroutine = null;
        }

        protected abstract void ShootAction(Vector3 direction);

        protected bool ColliderAllowed(Collider collider)
        {
            return !collider.transform.UnderParent(source.bodyRoot);
        }

        public bool TryHit(HitData hitData)
        {
            if (!ColliderAllowed(hitData.collider))
                return false;

            currentHits.Add(hitData);

            bool willDie = currentHits.Count < hitsBeforeDeath && CanReflect(hitData);
            OnHitAction(hitData, willDie);
            PerformParticleHit(hitData);
            onHit?.Invoke(hitData);

            if (willDie)
            {
                Shoot(hitData.point, GetHitReflect(hitData));
            }
            else
            {
                Die();
            }

            return true;
        }

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

        protected virtual void OnHitAction(HitData hitData, bool willDieAfterHit) { }
        protected virtual void OnDeathStart() { }
        protected virtual void OnDeathEnd() { }

        protected virtual bool CanReflect(HitData hitData)
        {
            return true;
        }

        protected virtual Vector3 GetHitReflect(HitData hitData)
        {
            return Vector3.Reflect(hitData.shotDirection, hitData.normal);
        }

        bool destroyed = false;
        void OnDestroy()
        {
            destroyed = true;
        }

        protected virtual async void Die()
        {
            OnDeathStart();
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

        void OnCollisionEnter(Collision collision)
        {
            HitData hitData = new HitData(this, collision);
            TryHit(hitData);
        }

        public struct HitData
        {
            public Collider collider;
            public Vector3 point;
            public Vector3 normal;
            public Projectile projectile;
            public Vector3 shotDirection;
            public Vector3 shotStartPoint;

            public HitData(Projectile projectile, Collider collider, Vector3 point, Vector3 normal)
            {
                this.collider = collider;
                this.point = point;
                this.normal = normal;
                this.projectile = projectile;
                this.shotDirection = projectile.current_shotDirection;
                this.shotStartPoint = projectile.current_shotStartPoint;
            }

            public HitData(Projectile projectile, RaycastHit raycastHit)
            {
                this.collider = raycastHit.collider;
                this.point = raycastHit.point;
                this.normal = raycastHit.normal;
                this.projectile = projectile;
                this.shotDirection = projectile.current_shotDirection;
                this.shotStartPoint = projectile.current_shotStartPoint;
            }

            public HitData(Projectile projectile, Collision collision)
            {
                this.collider = collision.collider;
                var contact = collision.GetContact(0);
                this.point = contact.point;
                this.normal = contact.normal;
                this.projectile = projectile;
                this.shotDirection = projectile.current_shotDirection;
                this.shotStartPoint = projectile.current_shotStartPoint;
            }
        }
    }
}