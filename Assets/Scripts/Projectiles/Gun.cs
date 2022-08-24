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
        public AudioClipGroup sfx_shoot;
        public AudioClipGroup sfx_reload;
        public int ammoPerClip = -1;
        public int currentClipAmmo { get; private set; } = 0;

        bool HasAmmo => currentClipAmmo != 0;

        bool CanReload => !inEquipTransition && !isReloading && currentClipAmmo != ammoPerClip;


        public float equipTime = .5f;
        public float unequipTime = .2f;

        public float equipTransition_speedLimit = 6;
        public Brain brain;
        public PhysicsBody physicsBody { get; private set; }
        [System.Serializable] public class ProjectilePool : Pool<Projectile> { }
        public ProjectilePool projectiles;

        public Transform bodyRoot;

        public float chargeup = 0;
        public float cooldown = 0.5f;
        public bool holdForAutomatic = true;

        public bool isShooting { get; private set; } = false;
        public bool isReloading { get; private set; } = false;
        public System.Action onShoot;
        public UnityEvent onShootEvent;

        public float shooting_moveSpeedLimit = 6;

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
        public RecoilBody.Settings gunRecoil = new RecoilBody.Settings(new Vector3(1f, 1f, 0.5f), 6, 6, .3333f, 1);

        public bool IsMovingBeyondRecoilThreshold => physicsBody.LastMoveStep.magnitude > recoil_moveSpeedMultiplierThreshold;



        public static readonly int ANIM_HASH_SPRINTING = Animator.StringToHash("Sprinting");
        public static readonly int ANIM_HASH_SHOOTING = Animator.StringToHash("Shooting");
        public static readonly int ANIM_HASH_AIMING = Animator.StringToHash("Aiming");
        public static readonly int ANIM_HASH_RELOAD = Animator.StringToHash("Reload");
        public static readonly int ANIM_HASH_RELOAD_CANCEL = Animator.StringToHash("ReloadCancel");
        public static readonly int ANIM_HASH_EQUIP = Animator.StringToHash("Equip");
        public static readonly int ANIM_HASH_UNEQUIP = Animator.StringToHash("Unequip");

        public static readonly int ANIM_HASH_ZOOM_MULTIPLIER = Animator.StringToHash("AimEnter_Multiplier");
        public static readonly int ANIM_HASH_ZOOM_RESET_MULTIPLIER = Animator.StringToHash("AimExit_Multiplier");
        public static readonly int ANIM_HASH_RELOAD_MULTIPLIER = Animator.StringToHash("Reload_Multiplier");
        public static readonly int ANIM_HASH_SPRINT_TRANSITION_MULTIPLIER = Animator.StringToHash("SprintTransition_Multiplier");
        public static readonly int ANIM_HASH_EQUIP_MULTIPLIER = Animator.StringToHash("Equip_Multiplier");
        public static readonly int ANIM_HASH_UNEQUIP_MULTIPLIER = Animator.StringToHash("Unequip_Multiplier");


        Task<Projectile> nextProjectile;

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
            animator.SetBool(ANIM_HASH_SHOOTING, isShooting);
            animator.SetBool(ANIM_HASH_AIMING, isAiming);
            animator.SetBool(ANIM_HASH_SPRINTING, isSprinting);
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

        void OnValidate()
        {
            for (int i = 0; i < animators_len; i++)
                SetupAnimatorParameters(animators[i]);
        }


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


            if (projectiles.prewarmSize < 0)
            {
                var prewarmEstimate = EstimatePrewarmSize();
                await prewarmEstimate;
                projectiles.prewarmSize = prewarmEstimate.Result;
            }

            projectiles.Setup();
            nextProjectile = projectiles.Get(false);
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
        public async Task<int> EstimatePrewarmSize()
        {
            var getFirstProjectile = projectiles.Get(false);
            await getFirstProjectile;
            Projectile p = getFirstProjectile.Result;
            p.Init();
            projectiles.ReturnToPool(p);

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

            int count = Mathf.CeilToInt(bulletMaxLifeTime / (chargeup + cooldown));
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
            isShooting = false;
            inEquipTransition = false;
            //brain.Shoot.onPress -= ShootInput;

            for (int i = 0; i < animators_len; i++)
            {
                animators[i].ResetTrigger(ANIM_HASH_RELOAD_CANCEL);
                animators[i].ResetTrigger(ANIM_HASH_RELOAD);
                animators[i].ResetTrigger(ANIM_HASH_EQUIP);
                animators[i].ResetTrigger(ANIM_HASH_UNEQUIP);
            }
        }

        public void Equip(bool equipActive = true)
        {
            if (equipCoroutine != null)
                StopCoroutine(equipCoroutine);

            int trigger = equipActive ? ANIM_HASH_EQUIP : ANIM_HASH_UNEQUIP;
            int resetTrigger = equipActive ? ANIM_HASH_UNEQUIP : ANIM_HASH_EQUIP;

            for (int i = 0; i < animators_len; i++)
            {
                animators[i].SetTrigger(trigger);
                animators[i].ResetTrigger(resetTrigger);
            }

            equipCoroutine = EquipCoroutine(equipActive ? equipTime : unequipTime);
            StartCoroutine(equipCoroutine);
        }

        bool inEquipTransition = false;
        IEnumerator equipCoroutine;
        IEnumerator EquipCoroutine(float time)
        {
            inEquipTransition = true;

            for (float t = 0; t < time; t += Time.deltaTime)
                yield return null;

            inEquipTransition = false;
        }

        private void Reload()
        {
            if (!CanReload)
                return;

            isReloading = true;
            reloadCoroutine = ReloadCoroutine();
            StartCoroutine(reloadCoroutine);
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

            for (int i = 0; i < animators_len; i++)
            {
                animators[i].SetTrigger(ANIM_HASH_RELOAD_CANCEL);
                animators[i].ResetTrigger(ANIM_HASH_RELOAD);
            }
        }

        float current_reloadTime;
        IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            CancelAim();
            gunAudio.PlayOneShot(sfx_reload);

            for (int i = 0; i < animators_len; i++)
            {
                animators[i].ResetTrigger(ANIM_HASH_RELOAD_CANCEL);
                animators[i].SetTrigger(ANIM_HASH_RELOAD);
            }


            for (current_reloadTime = 0; current_reloadTime < reloadTime; current_reloadTime += Time.deltaTime)
            {
                yield return null;
            }

            if (isReloading) //* Only top up our clip if the animator hasn't already done so
                Reload_Finish();

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
        bool inCooldown => current_cooldown > 0;

        bool isSprinting;
        void Update()
        {
            MoveLimitUpdate();

            isSprinting = physicsBody.lastManualMovement.magnitude > sprintThreshold;
            for (int i = 0; i < animators_len; i++)
                UpdateAnimationParameters(animators[i]);

            if (isSprinting)
                current_cooldown = Mathf.Max(current_cooldown, sprintTransitionTime);

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
            isShooting = false;

            if (inEquipTransition || !brain.Shoot)
            {
                current_chargeup = 0;
                current_heldShotsFired = 0;
                return;
            }

            //*Ensure that non-automatic weapons don't fire beyond the first held shot
            if (!holdForAutomatic && current_heldShotsFired > 0)
                return;

            if (isReloading)
            {
                bool canCancelReload = HasAmmo && current_heldShotsFired == 0 && current_reloadTime < canCancelReloadByShootingBeforeTime;
                if (canCancelReload)
                    CancelReload();
                else
                    return;
            }

            isShooting = true;

            if (inCooldown)
                return;

            if (!HasAmmo)
            {
                OutOfAmmo();
                return;
            }

            if (current_chargeup < chargeup)
            {
                current_chargeup += Time.deltaTime;
                return;
            }

            if (!nextProjectile.IsCompleted)
                return;

            Projectile p = nextProjectile.Result;

            if (p == null)
            {
                isShooting = false;
                return;
            }

            ShootProjectile(p);

            current_cooldown = cooldown;
        }

        void MoveLimitUpdate()
        {
            float limit = Mathf.Infinity;
            if (isShooting)
                limit = shooting_moveSpeedLimit;
            if (isAiming)
                limit = Mathf.Min(limit, aiming_moveSpeedLimit);
            if (current_reloadTime > 0)
                limit = Mathf.Min(limit, reload_moveSpeedLimit);
            if (inEquipTransition)
                limit = Mathf.Min(limit, equipTransition_speedLimit);

            if (limit < Mathf.Infinity)
            {
                physicsBody.ApplyMoveSpeedLimit_Ground(limit);
                physicsBody.ApplyMoveSpeedLimit_Air(limit);
            }
        }

        public void OutOfAmmo()
        {
            isShooting = false;
            current_chargeup = 0;
        }


        public virtual void ShootProjectile(Projectile projectile)
        {
            current_heldShotsFired++;
            projectile.Setup(this);

            projectile.onHit -= OnProjectileHit;
            projectile.onHit += OnProjectileHit;
            projectile.onDeath -= OnProjectileDeath;
            projectile.onDeath += OnProjectileDeath;

            projectile.gameObject.SetActive(true);
            projectile.Shoot(transform.forward);

            bool moving = IsMovingBeyondRecoilThreshold;
            aimRecoil.ExecuteRecoil(isAiming, moving);
            gunRecoil.ExecuteRecoil(isAiming, moving);

            gunAudio.PlayOneShot(sfx_shoot);
            onShoot?.Invoke();
            onShootEvent?.Invoke();

            if (currentClipAmmo > 0)
                currentClipAmmo--;

            nextProjectile = projectiles.Get(false);
        }

        protected virtual void OnProjectileHit(Projectile.HitData hitData)
        {

        }

        protected virtual void OnProjectileDeath(Projectile projectile)
        {
            projectiles.ReturnToPool(projectile);
        }
    }
}