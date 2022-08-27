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

        public Transform projectile_gunOffsetTransform;
        public AudioSource gunAudio;
        // public AudioClipGroup sfx_shoot;
        // public AudioClipGroup sfx_reload;
        // public AudioClipGroup sfx_reload_partial;
        // public AudioClipGroup sfx_reload_complete;

        public System.Action onReloadStart;
        public System.Action onReloadLoop;
        public System.Action onReloadEnd;



        public float Current_ProjectileSpread => Calculate_ProjectileSpreadAtTime(current_bloomTime);
        public float Calculate_ProjectileSpreadAtTime(float time) => base_projectileSpread + bloom_projectileSpreadIncreaseOverTime * Mathf.Min(time, ProjectileSpread_TimeToMaxSpread);//Mathf.Min(base_projectileSpread + bloom_projectileSpreadIncreaseOverTime * time, bloom_max_projectileSpreadIncreaseOverTime);
        [Min(0)] public float base_projectileSpread = 0;
        [Min(0)] public float bloom_projectileSpreadIncreaseOverTime = 0;
        [Min(0)] public float bloom_returnSpeed = 1;
        [Min(0)] public float bloom_max_projectileSpreadIncreaseOverTime = .1f;

        float ProjectileSpread_TimeToMaxSpread => bloom_max_projectileSpreadIncreaseOverTime / bloom_projectileSpreadIncreaseOverTime;

        [Min(1)] public int projectilesPerShot = 1;
        public int ammoPerClip = -1;
        public int currentClipAmmo { get; private set; } = 0;

        bool FullAmmo => currentClipAmmo == ammoPerClip;
        bool HasAmmo => currentClipAmmo != 0;

        bool CanReload => !inEquipTransition && !isReloading && currentClipAmmo != ammoPerClip;


        public float equipTime = .5f;
        public float unequipTime = .2f;
        public Brain brain;
        public PhysicsBody physicsBody { get; private set; }
        [System.Serializable] public class ProjectilePool : Pool<Projectile> { }
        public ProjectilePool projectiles;
        Projectile sampleProjectile;

        public Transform bodyRoot;

        public float chargeup = 0;
        public float cooldown = 0.5f;
        public bool holdForAutomatic = true;

        public bool isAlert => current_alertLevel > 0;
        public float current_alertLevel { get; private set; } = 0;
        public float returnSpeed_alertLevel = 5;

        float lastShotTime = Mathf.NegativeInfinity;
        public bool isShooting => Time.time < lastShotTime + cooldown;
        public float current_bloomTime { get; private set; } = 0;
        public bool isReloading => current_reloadTimeLeft > 0;
        public System.Action onShoot;
        public UnityEvent onShootEvent;

        public float shooting_moveSpeedLimit = 6;

        public bool autoReloadWhenShootingEmpty = true;
        public float reloadTime { get; private set; } = 1;
        public int reloadAmountPerLoop = -1;
        public float reload_moveSpeedLimit = 6;

        [Header("Aim")]
        CameraZoom cameraZoom;
        public float aiming_moveSpeedLimit = 8;
        [Min(1)] public float aim_zoomMultiplier = 1.6f;
        public bool HasAim => aim_zoomMultiplier > 1;

        public float aim_zoomTime = .1f;
        public float aim_zoomResetTime = .2f;
        public bool isAiming { get; private set; } = false;

        public float sprintThreshold = 12;

        public float sprintTransitionTime = .2f;


        [Header("Recoil")]
        public float recoil_moveSpeedMultiplierThreshold = 4f;
        public RecoilBody.Settings aimRecoil;
        public RecoilBody.Settings gunRecoil = new RecoilBody.Settings(new Vector3(1f, 1f, 0.5f), new Vector3(-1f, -1f, -0.5f), 6f, 6f, .5f, 2f);

        public bool IsMovingBeyondRecoilThreshold => physicsBody.LastMoveStep.magnitude > recoil_moveSpeedMultiplierThreshold;



        public static readonly int ANIM_HASH_SPRINTING = Animator.StringToHash("Sprinting");
        public static readonly int ANIM_HASH_SHOOT = Animator.StringToHash("Shoot");
        public static readonly int ANIM_HASH_ALERT = Animator.StringToHash("Alert");
        public static readonly int ANIM_HASH_AIMING = Animator.StringToHash("Aiming");
        public static readonly int ANIM_HASH_FULL_AMMO = Animator.StringToHash("FullAmmo");
        public static readonly int ANIM_HASH_RELOAD = Animator.StringToHash("Reload");
        public static readonly int ANIM_HASH_RELOAD_END = Animator.StringToHash("ReloadEnd");
        public static readonly int ANIM_HASH_EQUIP = Animator.StringToHash("Equip");
        public static readonly int ANIM_HASH_UNEQUIP = Animator.StringToHash("Unequip");

        public static readonly int ANIM_HASH_ZOOM_MULTIPLIER = Animator.StringToHash("AimEnter_Multiplier");
        public static readonly int ANIM_HASH_ZOOM_RESET_MULTIPLIER = Animator.StringToHash("AimExit_Multiplier");
        public static readonly int ANIM_HASH_RELOAD_MULTIPLIER = Animator.StringToHash("Reload_Multiplier");
        public static readonly int ANIM_HASH_SPRINT_TRANSITION_MULTIPLIER = Animator.StringToHash("SprintTransition_Multiplier");
        public static readonly int ANIM_HASH_EQUIP_MULTIPLIER = Animator.StringToHash("Equip_Multiplier");
        public static readonly int ANIM_HASH_UNEQUIP_MULTIPLIER = Animator.StringToHash("Unequip_Multiplier");


        Task<Projectile[]> nextProjectiles;

        IEnumerator reloadCoroutine;


        int prev_projectilesPerShot;
        bool awakeRun = false;
        async void Awake()
        {
            if (!bodyRoot)
                bodyRoot = brain.transform;

            //shootCoroutine = ShootInputCoroutine();

            ReadReloadAnimation();

            currentClipAmmo = ammoPerClip;

            physicsBody = GetComponentInParent<PhysicsBody>();
            cameraZoom = GetComponentInParent<CameraZoom>();

            //*Subscribe the gun to our animator system
            //Animator gunAnim = GetComponentInChildren<Animator>();
            //if (gunAnim) SubscribeAnimator(gunAnim);


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

        void Reset()
        {
            if (aimRecoil.recoilBody == null)
                aimRecoil.recoilBody = GetComponentInParent<RecoilBody>();

            if (gunRecoil.recoilBody == null)
                gunRecoil.recoilBody = GetComponentInChildren<RecoilBody>();
        }

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

            int perSecond = Mathf.CeilToInt(bulletMaxLifeTime / (chargeup + cooldown));
            if (ammoPerClip > 0)
                perSecond = Mathf.Min(ammoPerClip, perSecond);
            int count = perSecond * projectilesPerShot;
            //* Return the prwarm count - the existing pool
            return Mathf.Max(0, count - projectiles.inactivePooled.Count);
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

            int trigger = equipActive ? ANIM_HASH_EQUIP : ANIM_HASH_UNEQUIP;
            int resetTrigger = equipActive ? ANIM_HASH_UNEQUIP : ANIM_HASH_EQUIP;

            equipCoroutine = EquipCoroutine(equipActive);
            StartCoroutine(equipCoroutine);
        }

        public float equipPercent { get; private set; }
        public bool inEquipTransition { get; private set; } = false;
        IEnumerator equipCoroutine;
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

        private void Reload()
        {
            if (!CanReload)
                return;

            reloadCoroutine = ReloadCoroutine();
            StartCoroutine(reloadCoroutine);
        }

        public void Reload_AddAmmo(int ammo)
        {
            if (ammo < 0)
                ammo = ammoPerClip;

            current_chargeup = 0;
            current_heldShotsFired = 0;
            currentClipAmmo = Mathf.Min(ammoPerClip, currentClipAmmo + ammo);
        }

        public void CancelReload()
        {
            current_chargeup = 0;
            current_heldShotsFired = 0;
            current_reloadTimeLeft = 0;

            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);
        }

        public AnimationClip reloadAnimation;



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

        float reloadAnimation_loopStart;
        float reloadAnimation_loopEnd;
        float reloadAnimation_loadAmmoAtTime;
        float reloadAnimation_pointOfNoReturn;
        float reloadAnimation_finishingFlourish;

        Vector2 reloadLoop = Vector2.zero;

        //float current_reloadTime;
        public float current_reloadPercentComplete => current_reloadTime / reloadTime;

        public float current_reloadTimeLeft { get; private set; } = 0;
        public float current_reloadTime => reloadTime - current_reloadTimeLeft;
        //bool inReloadAnimation => isReloading || current_reloadTime > 0;
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
                    if (!FullAmmo)
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

            if (!FullAmmo)
            {
                current_reloadTimeLeft = reloadTime - reloadAnimation_loopStart;
                goto ReloadLoop;
            }

            current_reloadTimeLeft = 0;

            yield return null;
        }

        bool CanAim => !inEquipTransition && !isReloading && HasAim;
        public void CancelAim()
        {
            if (isAiming)
                cameraZoom.ResetZoom(aim_zoomResetTime);

            isAiming = false;
        }

        float current_cooldown = 0;
        float sprintToFire_cooldown = 0;
        bool inCooldown => current_cooldown > 0;

        public bool isSprinting { get; private set; }
        void Update()
        {
            MoveLimitUpdate();

            AlertLevelUpdate();

            if (isShooting)
                current_bloomTime = Mathf.Min(current_bloomTime + Time.deltaTime, ProjectileSpread_TimeToMaxSpread);
            else
                current_bloomTime = Mathf.Max(0, current_bloomTime - Time.deltaTime * bloom_returnSpeed);

            isSprinting = physicsBody.lastManualMovement.magnitude > sprintThreshold;
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

        void AlertLevelUpdate()
        {
            if (isShooting)
                current_alertLevel = 1;
            else
                current_alertLevel -= returnSpeed_alertLevel * Time.deltaTime;

            current_alertLevel = Mathf.Clamp01(current_alertLevel);

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

        public bool isActionCancellable
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

        float current_chargeup = 0;
        int current_heldShotsFired = 0;
        void ShootUpdate()
        {
            if (inEquipTransition || !brain.Shoot)
            {
                current_chargeup = 0;
                current_heldShotsFired = 0;
                //isShooting = false;
                return;
            }

            //*Ensure that non-automatic weapons don't fire beyond the first held shot
            if (!holdForAutomatic && current_heldShotsFired > 0)
            {
                //isShooting = false;
                return;
            }


            if (isReloading)
            {
                bool canShoot = brain.Shoot.pressedThisFrame && HasAmmo && current_heldShotsFired == 0;
                if (canShoot && isActionCancellable)
                    CancelReload();
                else
                {
                    //isShooting = false;
                    return;
                }
            }

            //isShooting = true;

            if (inCooldown)
            {
                //     if (!holdForAutomatic)
                //         isShooting = false;

                return;
            }


            if (!HasAmmo)
            {
                OutOfAmmo();
                return;
            }

            if (sprintToFire_cooldown > 0)
                return;

            if (current_chargeup < chargeup)
            {
                current_chargeup += Time.deltaTime;
                return;
            }

            if (!nextProjectiles.IsCompleted)
                return;

            Projectile[] p = nextProjectiles.Result;

            if (p == null || p[0] == null)
            {
                //isShooting = false;
                return;
            }

            ShootProjectiles(p);

            current_cooldown = cooldown;
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

        public void OutOfAmmo()
        {
            current_chargeup = 0;
            //isShooting = false;

            if (autoReloadWhenShootingEmpty && brain.Shoot.pressedThisFrame)
                Reload();
        }



        public AnimationCurve spreadPatternRandomWeightDistribution = AnimationCurve.Linear(0, 0, 1, 1);
        Vector3 GetSpreadPoint(int index, int max, float spread, float angleOffsetPercent, Quaternion forwardRotation)
        {
            if (spread <= 0.0f)
                return Vector3.zero;

            //*Surround the spread circumference uniformly
            float angle = 2f * Mathf.PI * ((index + 1f) / max + angleOffsetPercent);

            //*Use a random value for the distance from the center to give us the random feel
            //*spreadPatternRandomWeightDistribution is used here to provide some weighted output (0f-1f)
            float dst = spreadPatternRandomWeightDistribution.Evaluate(Random.value);


            Vector2 spreadPoint = new Vector2(dst * Mathf.Cos(angle), dst * Mathf.Sin(angle));
            return forwardRotation * spreadPoint * spread;
        }

        public virtual void ShootProjectiles(Projectile[] projectiles)
        {
            Vector3 forward = transform.forward;
            Quaternion forwardRotation = Quaternion.LookRotation(forward, transform.up);

            Vector3 barrelStartPosition = projectile_gunOffsetTransform.position;
            Vector3 local = transform.InverseTransformPoint(barrelStartPosition);
            Vector3 aimStartPosition = transform.position + forward * local.z;

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

                Vector3 dir = forward + GetSpreadPoint(i, projectilesLength, currentSpread, radianOffset, forwardRotation);

                projectiles[i].gameObject.SetActive(true);
                projectiles[i].Shoot(aimStartPosition, barrelStartPosition, dir.normalized);
            }

            current_heldShotsFired++;
            lastShotTime = Time.time;

            bool moving = IsMovingBeyondRecoilThreshold;
            aimRecoil.ExecuteRecoil(isAiming, moving);
            gunRecoil.ExecuteRecoil(isAiming, moving, aimRecoil.last_randomValues); //*Use the same random force as the above recoil

            onShoot?.Invoke();
            onShootEvent?.Invoke();

            // for (int i = 0; i < animators_len; i++)
            //     animators[i].SetTrigger(ANIM_HASH_SHOOT);

            if (currentClipAmmo > 0)
                currentClipAmmo--;

            PrepNextProjectiles();
        }

        protected virtual void OnProjectileHit(Projectile.HitData hitData)
        {

        }

        protected virtual void OnProjectileDeath(Projectile projectile)
        {
            projectiles.ReturnToPool(projectile);
        }


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

            float potentialShootTimeMax = (ammoPerClip >= 0 ? ammoPerClip : 1000) * (chargeup + cooldown);
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
    }
}