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
        public AudioSource gunAudio;
        // public AudioClipGroup sfx_shoot;
        // public AudioClipGroup sfx_reload;
        // public AudioClipGroup sfx_reload_partial;
        // public AudioClipGroup sfx_reload_complete;

        public System.Action onReloadStart;
        public System.Action onReloadLoop;
        public System.Action onReloadEnd;



        public float Current_ProjectileSpread => Calculate_ProjectileSpreadAtTime(current_shootTime);
        public float Calculate_ProjectileSpreadAtTime(float time) => Mathf.Min(base_projectileSpread + bloom_projectileSpreadIncreaseOverTime * time, bloom_max_projectileSpreadIncreaseOverTime);
        [Min(0)] public float base_projectileSpread = 0;
        [Min(0)] public float bloom_projectileSpreadIncreaseOverTime = 0;
        [Min(0)] public float bloom_max_projectileSpreadIncreaseOverTime = .1f;

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

        public bool isAlert => current_shootTime > 0;
        public float current_shootTime { get; private set; } = 0;
        public bool isReloading { get; private set; } = false;
        public System.Action onShoot;
        public UnityEvent onShootEvent;

        public float shooting_moveSpeedLimit = 6;

        public bool autoReloadWhenShootingEmpty = true;
        public float reloadTime = 1;
        public float canCancelReloadByShootingBeforeTime = .5f;
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


        List<Animator> animators = new List<Animator>();
        int animators_len;

        public void SubscribeAnimator(Animator animator)
        {
            if (!animators.Contains(animator))
                animators.Add(animator);

            animators_len = animators.Count;

            SetupAnimatorParameters(animator);
        }

        public void UnsubscribeAnimator(Animator animator)
        {
            animators.Remove(animator);
            animators_len = animators.Count;
        }

        public void UpdateAnimationParameters(Animator animator)
        {
            animator.SetBool(ANIM_HASH_ALERT, isAlert);
            animator.SetBool(ANIM_HASH_AIMING, isAiming);
            animator.SetBool(ANIM_HASH_SPRINTING, isSprinting);
            animator.SetBool(ANIM_HASH_FULL_AMMO, FullAmmo);
        }

        public void SetupAnimatorParameters(Animator animator)
        {
            animator.SetFloat(ANIM_HASH_ZOOM_MULTIPLIER, 1f / aim_zoomTime);
            animator.SetFloat(ANIM_HASH_ZOOM_RESET_MULTIPLIER, 1f / aim_zoomResetTime);
            animator.SetFloat(ANIM_HASH_RELOAD_MULTIPLIER, 1f / reloadTime);
            animator.SetFloat(ANIM_HASH_SPRINT_TRANSITION_MULTIPLIER, 1f / sprintTransitionTime);
            animator.SetFloat(ANIM_HASH_EQUIP_MULTIPLIER, 1f / equipTime);
            animator.SetFloat(ANIM_HASH_UNEQUIP_MULTIPLIER, 1f / unequipTime);
        }




        int prev_projectilesPerShot;
        bool awakeRun = false;
        async void Awake()
        {
            if (!bodyRoot)
                bodyRoot = brain.transform;

            //shootCoroutine = ShootInputCoroutine();

            currentClipAmmo = ammoPerClip;

            physicsBody = GetComponentInParent<PhysicsBody>();
            cameraZoom = GetComponentInParent<CameraZoom>();

            //*Subscribe the gun to our animator system
            Animator gunAnim = GetComponentInChildren<Animator>();
            if (gunAnim) SubscribeAnimator(gunAnim);


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

            float bulletMaxLifeTime = p.aliveTime * p.hitsBeforeDeath + p.deathTime;

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
            Equip();
        }

        public void OnDisable()
        {
            isReloading = false;
            current_cooldown = 0;
            StopAllCoroutines();
            current_shootTime = 0;
            inEquipTransition = false;
            //brain.Shoot.onPress -= ShootInput;

            for (int i = 0; i < animators_len; i++)
            {
                animators[i].ResetTrigger(ANIM_HASH_RELOAD_END);
                animators[i].ResetTrigger(ANIM_HASH_RELOAD);
                animators[i].ResetTrigger(ANIM_HASH_EQUIP);
                animators[i].ResetTrigger(ANIM_HASH_UNEQUIP);
            }
        }

        public void Equip(bool equipActive = true)
        {
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

            for (int i = 0; i < animators_len; i++)
            {
                animators[i].SetTrigger(trigger);
                animators[i].ResetTrigger(resetTrigger);
            }

            equipCoroutine = EquipCoroutine(equipActive);
            StartCoroutine(equipCoroutine);
        }

        public bool inEquipTransition { get; private set; } = false;
        IEnumerator equipCoroutine;
        IEnumerator EquipCoroutine(bool equipActive)
        {
            inEquipTransition = true;

            float endTime = equipActive ? equipTime : unequipTime;
            for (float t = 0; t < endTime; t += Time.deltaTime)
                yield return null;

            for (int i = 0; i < animators_len; i++)
                SetupAnimatorParameters(animators[i]);

            inEquipTransition = false;

            if (!equipActive)
                gameObject.SetActive(false);
        }

        private void Reload()
        {
            if (!CanReload)
                return;

            isReloading = true;
            reloadCoroutine = ReloadCoroutine();
            StartCoroutine(reloadCoroutine);
        }

        public void Reload_RestartTime()
        {
            if (FullAmmo)
                return;

            current_reloadTime = 0;
            onReloadLoop?.Invoke();
        }

        public void Reload_AddAmmo(int ammo)
        {
            current_chargeup = 0;
            current_heldShotsFired = 0;
            currentClipAmmo = Mathf.Min(ammoPerClip, currentClipAmmo + ammo);

            if (FullAmmo)
                isReloading = false;
        }

        public void Reload_Finish()
        {
            current_chargeup = 0;
            current_heldShotsFired = 0;
            currentClipAmmo = ammoPerClip;
            isReloading = false;
        }

        public void CancelReload()
        {
            current_chargeup = 0;
            current_heldShotsFired = 0;
            current_reloadTime = 0;

            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);
            isReloading = false;

            // for (int i = 0; i < animators_len; i++)
            // {
            //     animators[i].SetTrigger(ANIM_HASH_RELOAD_CANCEL);
            //     animators[i].ResetTrigger(ANIM_HASH_RELOAD);
            // }
        }

        float current_reloadTime;
        bool inReloadAnimation => isReloading || current_reloadTime > 0;
        IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            CancelAim();
            onReloadStart?.Invoke();
            //gunAudio.PlayOneShot(sfx_reload);

            for (int i = 0; i < animators_len; i++)
            {
                animators[i].ResetTrigger(ANIM_HASH_RELOAD_END);
                animators[i].SetTrigger(ANIM_HASH_RELOAD);
            }

            Reload_RestartTime();
            for (; current_reloadTime < reloadTime + Time.deltaTime || !FullAmmo; current_reloadTime += Time.deltaTime)
            {
                yield return null;
            }

            for (int i = 0; i < animators_len; i++)
                animators[i].SetTrigger(ANIM_HASH_RELOAD_END);

            if (isReloading) //* Only top up our clip if the animator hasn't already done so
                Reload_Finish();

            yield return null;
            for (int i = 0; i < animators_len; i++)
                animators[i].ResetTrigger(ANIM_HASH_RELOAD_END);

            onReloadEnd?.Invoke();
            current_reloadTime = 0;
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

        bool isSprinting;
        void Update()
        {
            MoveLimitUpdate();

            isSprinting = physicsBody.lastManualMovement.magnitude > sprintThreshold;
            for (int i = 0; i < animators_len; i++)
                UpdateAnimationParameters(animators[i]);

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

        float current_chargeup = 0;
        int current_heldShotsFired = 0;
        void ShootUpdate()
        {
            if (inEquipTransition || !brain.Shoot)
            {
                current_chargeup = 0;
                current_heldShotsFired = 0;
                current_shootTime = 0;
                return;
            }

            //*Ensure that non-automatic weapons don't fire beyond the first held shot
            if (!holdForAutomatic && current_heldShotsFired > 0)
            {
                current_shootTime = 0;
                return;
            }


            if (inReloadAnimation)
            {
                bool canCancelReload = brain.Shoot.pressedThisFrame && HasAmmo && current_heldShotsFired == 0 && current_reloadTime < canCancelReloadByShootingBeforeTime;
                if (canCancelReload)
                    CancelReload();
                else
                {
                    current_shootTime = 0;
                    return;
                }
            }

            current_shootTime += Time.deltaTime;

            if (inCooldown)
            {
                if (!holdForAutomatic)
                    current_shootTime = 0;

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
                current_shootTime = 0;
                return;
            }

            ShootProjectiles(p);

            current_cooldown = cooldown;
        }

        void MoveLimitUpdate()
        {
            float limit = Mathf.Infinity;
            if (isAlert)
                limit = shooting_moveSpeedLimit;
            if (isAiming)
                limit = Mathf.Min(limit, aiming_moveSpeedLimit);
            if (current_reloadTime > 0)
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
            current_shootTime = 0;

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
                projectiles[i].Shoot(dir.normalized);
            }

            current_heldShotsFired++;

            bool moving = IsMovingBeyondRecoilThreshold;
            aimRecoil.ExecuteRecoil(isAiming, moving);
            gunRecoil.ExecuteRecoil(isAiming, moving, aimRecoil.last_randomValues); //*Use the same random force as the above recoil

            onShoot?.Invoke();
            onShootEvent?.Invoke();

            for (int i = 0; i < animators_len; i++)
                animators[i].SetTrigger(ANIM_HASH_SHOOT);

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
            if (maxDist == Mathf.Infinity)
                maxDist = 100;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * maxDist);

            float potentialShootTimeMax = (ammoPerClip >= 0 ? ammoPerClip : 1000) * (chargeup + cooldown);
            float maxBloomCalculation = Mathf.Min(bloom_max_projectileSpreadIncreaseOverTime, potentialShootTimeMax * bloom_projectileSpreadIncreaseOverTime) + base_projectileSpread;

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

            for (int i = 0; i < animators_len; i++)
                SetupAnimatorParameters(animators[i]);

            if (prev_projectilesPerShot != projectilesPerShot)
                ResizeNextProjectiles();
        }
#endif
    }
}