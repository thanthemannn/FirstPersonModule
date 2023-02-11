using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Than.Input;
using Than.Physics3D;
using UnityEngine.Events;
using System.Threading.Tasks;

namespace Than.Projectiles
{
    public class Gun : MonoBehaviour
    {

        #region Public Properties and Events

        public Brain brain { get; private set; }
        public Transform bodyRoot => brain.transform;
        public bool isShooting => Time.time < lastShotTime + fire_cooldown;
        public bool isReloading => current_reloadTimeLeft > 0;
        public float reloadTime { get; private set; } = 1;
        public bool isAlert => current_alertLevel > 0;
        public int currentMagazineAmmo { get; private set; } = 0;

        public float current_alertLevel { get; private set; } = 0;

        public float current_bloomTime { get; private set; } = 0;

        public System.Action onShoot;
        public System.Action onReloadStart;
        public System.Action onReloadLoop;
        public System.Action onReloadEnd;

        public bool HasFullMagazine => currentMagazineAmmo == ammoPerMagazine;
        public bool HasAmmoInMagazine => currentMagazineAmmo != 0;

        public float Current_ProjectileSpread => Calculate_ProjectileSpreadAtTime(current_bloomTime);



        public bool HasAim => aim_zoomMultiplier > 1;

        public bool isAiming { get; private set; } = false;
        public Movement movementComponent { get; private set; }
        public PhysicsBody physicsBody => movementComponent.pb;



        public bool IsMovingBeyondRecoilThreshold => physicsBody.MoveStep.magnitude > recoil_moveSpeedMultiplierThreshold;

        public Projectile sampleProjectile { get; private set; }

        public float equipPercent { get; private set; }
        public bool inEquipTransition { get; private set; } = false;

        public float current_reloadPercentComplete => current_reloadTime / reloadTime;

        public float current_reloadTimeLeft { get; private set; } = 0;
        public float current_reloadTime => reloadTime - current_reloadTimeLeft;


        public bool isSprinting { get; private set; }

        public bool IsActionCancellable
        {
            get
            {
                if (isReloading)
                {
                    bool outsideMainReloadAnimation = current_reloadTime < reloadAnimation_pointOfNoReturn || current_reloadTime > reloadAnimation_finishingFlourish;
                    return outsideMainReloadAnimation;
                }

                return true;
            }

        }


        #endregion

        #region Inspector Fields

        [Header("References")]

        public AnimationClip reloadAnimation;
        public Transform gunBarrelTransform;


        [Header("Equip")]
        public float equipTime = .5f;
        public float unequipTime = .2f;


        [Header("Ammo / Reload")]
        public int ammoPerMagazine = -1;
        public int reloadAmountPerLoop = -1;
        public bool autoReloadWhenShootingEmpty = true;


        [Header("Projectile General")]

        public ProjectilePool projectiles;
        [Min(1)] public int projectilesPerShot = 1;

        public float fire_chargeup = 0;
        public float fire_cooldown = 0.5f;
        public bool fire_holdForAutomatic = true;



        [Header("Projectile Spread")]
        [Min(0)] public float base_projectileSpread = 0;
        public AnimationCurve spreadPatternRandomWeightDistribution_projectileSpread = AnimationCurve.Linear(0, 0, 1, 1);

        [Min(0)] public float bloom_increaseOverTime_projectileSpread = .2f;
        [Min(0)] public float bloom_max_increaseOverTime_projectileSpread = .1f;
        [Min(0)] public float bloom_returnSpeed_projectileSpread = 1;



        [Header("Aim")]
        [Min(1)] public float aim_zoomMultiplier = 1.6f;
        public float aim_zoomTime = .1f;
        public float aim_zoomResetTime = .2f;


        [Header("Recoil")]
        public RecoilBody.Settings aimRecoil;
        public RecoilBody.Settings gunRecoil = new RecoilBody.Settings(new Vector3(1f, 1f, 0.5f), new Vector3(-1f, -1f, -0.5f), 6f, 6f, .5f, 2f);


        [Header("Movement / Stance")]
        public float returnSpeed_alertLevel = 5;
        public float shooting_moveSpeedLimit = 6;
        public float reload_moveSpeedLimit = 6;
        public float aiming_moveSpeedLimit = 8;
        public float recoil_moveSpeedMultiplierThreshold = 4f;
        public float sprintTransitionTime = .2f;


        [Header("Events")]
        public UnityEvent onShootEvent;


        #endregion

        #region Other Properties and Variables
        CameraZoom cameraZoom;

        float ProjectileSpread_TimeToMaxSpread => bloom_max_increaseOverTime_projectileSpread / bloom_increaseOverTime_projectileSpread;
        bool CanReload => !inEquipTransition && !isReloading && currentMagazineAmmo != ammoPerMagazine;

        float lastShotTime = Mathf.NegativeInfinity;

        Task<Projectile[]> nextProjectiles;

        IEnumerator equipCoroutine;
        IEnumerator reloadCoroutine;
        int prev_projectilesPerShot;
        bool awakeRun = false;

        float reloadAnimation_loopStart;
        float reloadAnimation_loopEnd;
        float reloadAnimation_loadAmmoAtTime;
        float reloadAnimation_pointOfNoReturn;
        float reloadAnimation_finishingFlourish;

        float current_cooldown = 0;
        float sprintToFire_cooldown = 0;
        bool inCooldown => current_cooldown > 0;

        float current_chargeup = 0;
        int current_heldShotsFired = 0;

        bool CanAim => !inEquipTransition && !isReloading && HasAim;

        #endregion


        #region Helper Functions

        /// <summary>
        /// Attempts to return prewarm time based off of a projectiles calculated lifetime. There may be innaccuracies and it is always recommended to test each gun and make manual adjustments.
        /// </summary>
        public int EstimatePrewarmSize(Projectile p)
        {
            float aliveTime = p.aliveTime;
            if (aliveTime == Mathf.Infinity) //* No point estimating aliveTime if it's infinite
                aliveTime = 0;

            float bulletMaxLifeTime = p.aliveTime * (p.reflects + 1) + p.deathTime;

            if (p.hitParticle)
            {
                var main = p.hitParticle.main;
                float particleLifetime = main.startDelay.constantMax + main.startLifetime.constantMax + main.duration;
                bulletMaxLifeTime += Mathf.Max(0, particleLifetime - p.deathTime);
            }

            int perSecond = Mathf.CeilToInt(bulletMaxLifeTime / (fire_chargeup + fire_cooldown));
            if (ammoPerMagazine > 0)
                perSecond = Mathf.Min(ammoPerMagazine, perSecond);
            int count = perSecond * projectilesPerShot;
            //* Return the prwarm count - the existing pool
            return Mathf.Max(0, count - projectiles.inactivePooled.Count);
        }


        public float Calculate_ProjectileSpreadAtTime(float time) => base_projectileSpread + bloom_increaseOverTime_projectileSpread * Mathf.Min(time, ProjectileSpread_TimeToMaxSpread);

        Vector3 GetSpreadPoint(int index, int max, float spread, float angleOffsetPercent, Quaternion forwardRotation)
        {
            if (spread <= 0.0f)
                return Vector3.zero;

            //*Surround the spread circumference uniformly
            float angle = 2f * Mathf.PI * ((index + 1f) / max + angleOffsetPercent);

            //*Use a random value for the distance from the center to give us the random feel
            //*spreadPatternRandomWeightDistribution is used here to provide some weighted output (0f-1f)
            float dst = spreadPatternRandomWeightDistribution_projectileSpread.Evaluate(Random.value);

            Vector2 spreadPoint = new Vector2(dst * Mathf.Cos(angle), dst * Mathf.Sin(angle));
            return forwardRotation * spreadPoint * spread;
        }

        #endregion

        #region Unity Methods

        async void Awake()
        {
            brain = GetComponentInParent<Brain>();


            ReadReloadAnimation();

            currentMagazineAmmo = ammoPerMagazine;

            movementComponent = GetComponentInParent<Movement>();
            cameraZoom = GetComponentInParent<CameraZoom>();


            var sample = projectiles.Get(false);
            await sample;
            sampleProjectile = sample.Result;
            projectiles.ReturnToPool(sampleProjectile);

            if (projectiles.prewarmSize < 0)
            {
                projectiles.prewarmSize = EstimatePrewarmSize(sampleProjectile);
            }

            projectiles.Setup();

            prev_projectilesPerShot = projectilesPerShot;
            PrepNextProjectiles();

            awakeRun = true;
        }

        public void OnEnable()
        {
            equipPercent = 0;
            Equip();
        }

        public void OnDisable()
        {
            lastShotTime = Mathf.NegativeInfinity;
            equipPercent = 0;
            current_bloomTime = 0;
            current_alertLevel = 0;
            current_reloadTimeLeft = 0;
            current_cooldown = 0;
            StopAllCoroutines();
            // isShooting = false;
            inEquipTransition = false;
        }


        void Update()
        {
            MoveLimitUpdate();

            AlertLevelUpdate();

            if (isShooting)
                current_bloomTime = Mathf.Min(current_bloomTime + Time.deltaTime, ProjectileSpread_TimeToMaxSpread);
            else
                current_bloomTime = Mathf.Max(0, current_bloomTime - Time.deltaTime * bloom_returnSpeed_projectileSpread);

            isSprinting = physicsBody.LastControlledMovement.magnitude >= Mathf.Lerp(movementComponent.moveSpeed, movementComponent.sprintSpeed, .5f);
            // for (int i = 0; i < animators_len; i++)
            //     UpdateAnimationParameters(animators[i]);

            if (isSprinting)
                sprintToFire_cooldown = sprintTransitionTime;
            else if (sprintToFire_cooldown > 0)
                sprintToFire_cooldown -= Time.deltaTime;

            if (current_cooldown > 0)
                current_cooldown -= Time.deltaTime;

            if (brain.Reload)
                Reload();

            AimUpdate();
            ShootUpdate();
        }





        #endregion

        #region Updates

        void AlertLevelUpdate()
        {
            if (isShooting)
                current_alertLevel = 1;
            else
                current_alertLevel -= returnSpeed_alertLevel * Time.deltaTime;

            current_alertLevel = Mathf.Clamp01(current_alertLevel);

        }

        void MoveLimitUpdate()
        {
            float limit = Mathf.Infinity;
            if (isShooting)
                limit = shooting_moveSpeedLimit;
            if (isAiming)
                limit = Mathf.Min(limit, aiming_moveSpeedLimit);
            if (isReloading)
                limit = Mathf.Min(limit, reload_moveSpeedLimit);

            if (limit < Mathf.Infinity)
            {
                physicsBody.ApplyMoveSpeedLimit_Ground(limit);
                physicsBody.ApplyMoveSpeedLimit_Air(limit);
            }
        }

        #endregion

        #region Equip

        public void Equip(bool equipActive = true)
        {
            current_reloadTimeLeft = 0;

            if (!gameObject.activeSelf)
            {
                if (equipActive)
                    gameObject.SetActive(true);
                else
                    return;
            }

            if (equipCoroutine != null)
                StopCoroutine(equipCoroutine);

            equipCoroutine = EquipCoroutine(equipActive);
            StartCoroutine(equipCoroutine);
        }

        IEnumerator EquipCoroutine(bool equipActive)
        {
            inEquipTransition = true;


            if (equipActive)
            {
                float speed = 1 / equipTime;
                for (equipPercent = 0; equipPercent < 1; equipPercent += Time.deltaTime * speed)
                    yield return null;

                equipPercent = 1;
            }
            else
            {
                float speed = 1 / unequipTime;
                for (equipPercent = 1; equipPercent > 0; equipPercent -= Time.deltaTime * speed)
                    yield return null;

                equipPercent = 0;
            }

            //for (int i = 0; i < animators_len; i++)
            //    SetupAnimatorParameters(animators[i]);

            inEquipTransition = false;

            if (!equipActive)
                gameObject.SetActive(false);
        }

        #endregion

        #region Reload

        private void Reload()
        {
            if (!CanReload)
                return;

            reloadCoroutine = ReloadCoroutine();
            StartCoroutine(reloadCoroutine);
        }

        public void CancelReload()
        {
            current_chargeup = 0;
            current_heldShotsFired = 0;
            current_reloadTimeLeft = 0;

            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);
        }

        void Reload_AddAmmo(int ammo)
        {
            if (ammo < 0)
                ammo = ammoPerMagazine;

            current_chargeup = 0;
            current_heldShotsFired = 0;
            currentMagazineAmmo = Mathf.Min(ammoPerMagazine, currentMagazineAmmo + ammo);
        }

        IEnumerator ReloadCoroutine()
        {
            CancelAim();
            onReloadStart?.Invoke();

            current_reloadTimeLeft = reloadTime;
        ReloadLoop:
            bool hasReloadedThisLoop = false;
            bool loopStarted = false;
            bool loopFinished = false;
            for (; current_reloadTimeLeft > 0; current_reloadTimeLeft -= Time.deltaTime)
            {
                float time = reloadTime - current_reloadTimeLeft;

                if (!loopStarted && time >= reloadAnimation_loopStart)
                {
                    onReloadLoop?.Invoke();
                    loopStarted = true;
                }

                if (!hasReloadedThisLoop && time >= reloadAnimation_loadAmmoAtTime)
                {
                    Reload_AddAmmo(reloadAmountPerLoop);
                    hasReloadedThisLoop = true;
                }

                if (time >= reloadAnimation_loopEnd)
                {
                    if (!HasFullMagazine)
                    {
                        current_reloadTimeLeft = reloadTime - reloadAnimation_loopStart;
                        goto ReloadLoop;
                    }
                    else if (!loopFinished)
                    {
                        onReloadEnd?.Invoke();
                        loopFinished = true;
                    }
                }

                yield return null;
            }

            if (!hasReloadedThisLoop)
                Reload_AddAmmo(reloadAmountPerLoop);

            if (!HasFullMagazine)
            {
                current_reloadTimeLeft = reloadTime - reloadAnimation_loopStart;
                goto ReloadLoop;
            }

            current_reloadTimeLeft = 0;

            yield return null;
        }

        void ReadReloadAnimation()
        {
            reloadTime = reloadAnimation.length;
            reloadAnimation_loopEnd = reloadTime;
            reloadAnimation_loadAmmoAtTime = reloadTime;
            reloadAnimation_pointOfNoReturn = reloadTime;
            reloadAnimation_finishingFlourish = reloadTime;

            var events = reloadAnimation.events;
            foreach (var e in events)
            {
                float eventTime = e.time;
                switch (e.functionName)
                {
                    case "Anim_Reload_StartLoopPoint":
                        reloadAnimation_loopStart = eventTime;
                        break;

                    case "Anim_Reload_EndLoopPoint":
                        reloadAnimation_loopEnd = eventTime;
                        break;

                    case "Anim_Reload_LoadAmmo":
                        reloadAnimation_loadAmmoAtTime = eventTime;
                        break;

                    case "Anim_Reload_PointOfNoReturn":
                        reloadAnimation_pointOfNoReturn = eventTime;
                        break;

                    case "Anim_Reload_FinishingFlourish":
                        reloadAnimation_finishingFlourish = eventTime;
                        break;
                }
            }
        }

        #endregion

        #region Aim
        public void CancelAim()
        {
            if (isAiming)
                cameraZoom.ResetZoom(aim_zoomResetTime);

            isAiming = false;
        }

        void AimUpdate()
        {
            if (brain.Aim)
            {
                if (!CanAim || isAiming)
                    return;

                cameraZoom.SetZoom(aim_zoomMultiplier, aim_zoomTime);
                isAiming = true;
            }
            else if (isAiming)
                CancelAim();
        }

        #endregion

        #region Shoot

        public virtual void ShootProjectiles(Projectile[] projectiles)
        {
            Projectile.ShootData shootData = new Projectile.ShootData(this);
            Vector3 forward = transform.forward;
            Quaternion forwardRotation = Quaternion.LookRotation(forward, transform.up);

            float currentSpread = Current_ProjectileSpread;
            float radianOffset = Random.value;
            int projectilesLength = projectiles.Length;
            for (int i = 0; i < projectilesLength; i++)
            {
                if (projectiles[i] == null)
                    break;

                projectiles[i].Setup(this);

                projectiles[i].onHit -= OnProjectileHit;
                projectiles[i].onHit += OnProjectileHit;
                projectiles[i].onDeath -= OnProjectileDeath;
                projectiles[i].onDeath += OnProjectileDeath;

                shootData.shootDirection = (forward + GetSpreadPoint(i, projectilesLength, currentSpread, radianOffset, forwardRotation)).normalized;

                projectiles[i].gameObject.SetActive(true);
                projectiles[i].Shoot(shootData);
            }

            current_cooldown = fire_cooldown;
            current_heldShotsFired++;
            lastShotTime = Time.time;

            bool moving = IsMovingBeyondRecoilThreshold;
            aimRecoil.ExecuteRecoil(isAiming, moving);
            gunRecoil.ExecuteRecoil(isAiming, moving, aimRecoil.last_randomValues); //*Use the same random force as the above recoil

            onShoot?.Invoke();
            onShootEvent?.Invoke();

            if (currentMagazineAmmo > 0)
                currentMagazineAmmo--;

            PrepNextProjectiles();
        }

        void ShootUpdate()
        {
            //?
            //? CHECKS BEFORE COOLDOWN
            //?
            if (inEquipTransition || !brain.Shoot)
            {
                current_chargeup = 0;
                current_heldShotsFired = 0;
                //isShooting = false;
                return;
            }

            //*Ensure that non-automatic weapons don't fire beyond the first held shot
            if (!fire_holdForAutomatic && current_heldShotsFired > 0)
            {
                //isShooting = false;
                return;
            }

            if (isReloading)
            {
                bool canShoot = brain.Shoot.pressedThisFrame && HasAmmoInMagazine && current_heldShotsFired == 0;
                if (canShoot && IsActionCancellable)
                    CancelReload();
                else
                {
                    //isShooting = false;
                    return;
                }
            }
            //isShooting = true;

            //?
            //? COOLDOWN
            //?
            if (inCooldown)
            {
                return;
            }

            if (!HasAmmoInMagazine)
            {
                OutOfAmmo();
                return;
            }

            if (sprintToFire_cooldown > 0)
            {
                return;
            }

            //?
            //? CHARGEUP
            //?

            if (current_chargeup < fire_chargeup)
            {
                current_chargeup += Time.deltaTime;
                return;
            }

            if (!nextProjectiles.IsCompleted)
            {
                return;
            }

            //?
            //? FIRE PROJECTILE
            //?

            Projectile[] p = nextProjectiles.Result;

            if (p == null || p[0] == null)
            {
                //isShooting = false;
                return;
            }

            ShootProjectiles(p);
        }


        public void OutOfAmmo()
        {
            current_chargeup = 0;
            //isShooting = false;

            if (autoReloadWhenShootingEmpty && brain.Shoot.pressedThisFrame)
                Reload();
        }


        #endregion

        #region Projectile Management

        [System.Serializable] public class ProjectilePool : Pool<Projectile> { }
        void PrepNextProjectiles()
        {
            nextProjectiles = projectiles.Get(projectilesPerShot, false);
        }

        async void ResizeNextProjectiles()
        {
            await nextProjectiles;
            Projectile[] prevResults = nextProjectiles.Result;
            int len = prevResults.Length;

            for (int i = 0; i < len; i++)
            {
                if (prevResults[i] == null)
                    break;

                if (!prevResults[i].gameObject.activeInHierarchy)
                    projectiles.ReturnToPool(prevResults[i]);
            }

            PrepNextProjectiles();
        }

        protected virtual void OnProjectileHit(Projectile.HitData hitData)
        {

        }

        protected virtual void OnProjectileDeath(Projectile projectile)
        {
            projectiles.ReturnToPool(projectile);
        }

        #endregion

        #region Gizmos, OnValidate, Editor

#if UNITY_EDITOR
        [Header("Gizmos")]
        public float gizmosPreview_shotDistanceTestDelta = 5f;
        Vector3[] gizmosSpreadPositions = new Vector3[0];

        bool gizmos_regenerateSpreadPositions = false;

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                if (sampleProjectile == null) return;
            }
            else if (sampleProjectile == null)
            {
                sampleProjectile = projectiles.addressableReference.editorAsset.GetComponent<Projectile>();
            }

            float maxDist = sampleProjectile.maxShootDistance;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * maxDist);

            float potentialShootTimeMax = (ammoPerMagazine >= 0 ? ammoPerMagazine : 1000) * (fire_chargeup + fire_cooldown);
            float maxBloomCalculation = Calculate_ProjectileSpreadAtTime(potentialShootTimeMax);//Mathf.Min(bloom_max_projectileSpreadIncreaseOverTime, potentialShootTimeMax * bloom_projectileSpreadIncreaseOverTime) + base_projectileSpread;

            for (float t = 1; t < maxDist; t += gizmosPreview_shotDistanceTestDelta)
                DrawShotGizmosAtTime(t, maxBloomCalculation);

            DrawShotGizmosAtTime(maxDist, maxBloomCalculation);
        }

        void DrawShotGizmosAtTime(float t, float maxBloomCalculation)
        {
            Vector3 dir = transform.forward * t;
            Vector3 pos = transform.position;

            if (base_projectileSpread > 0)
            {
                Gizmos.color = Color.blue;
                Than.GizmosExt.DrawCircle(pos + dir, Quaternion.LookRotation(transform.forward, transform.up), base_projectileSpread * t);
            }

            if (maxBloomCalculation > base_projectileSpread)
            {
                Gizmos.color = Color.red;
                Than.GizmosExt.DrawCircle(pos + dir, Quaternion.LookRotation(transform.forward, transform.up), maxBloomCalculation * t);
            }

            Quaternion forwardRotation = Quaternion.LookRotation(transform.forward, transform.up);


            if (gizmosSpreadPositions.Length != projectilesPerShot)
            {
                gizmosSpreadPositions = new Vector3[projectilesPerShot];
                gizmos_regenerateSpreadPositions = true;
            }

            for (int i = 0; i < projectilesPerShot; i++)
            {
                if (gizmos_regenerateSpreadPositions)
                    gizmosSpreadPositions[i] = GetSpreadPoint(i, projectilesPerShot, maxBloomCalculation, Time.realtimeSinceStartup, forwardRotation);

                sampleProjectile.DrawProjectileGizmos(pos + (transform.forward + gizmosSpreadPositions[i]) * t);
            }

            gizmos_regenerateSpreadPositions = false;
        }


        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                gizmos_regenerateSpreadPositions = true;
                sampleProjectile = null;
                return;
            }

            if (!awakeRun)
                return;

            ReadReloadAnimation();

            // for (int i = 0; i < animators_len; i++)
            //     SetupAnimatorParameters(animators[i]);

            if (prev_projectilesPerShot != projectilesPerShot)
                ResizeNextProjectiles();
        }
#endif

        #endregion

    }
}