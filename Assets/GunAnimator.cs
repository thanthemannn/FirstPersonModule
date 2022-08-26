using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Than.Projectiles
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Gun))]
    public class GunAnimator : MonoBehaviour
    {
        Gun gun;
        Animator anim;


        int state;

        static readonly int ANIM_HASH_FLOAT_SPRINT_WEIGHT = Animator.StringToHash("SprintWeight");
        static readonly int ANIM_HASH_FLOAT_AIM_WEIGHT = Animator.StringToHash("AimWeight");
        static readonly int ANIM_HASH_FLOAT_RELOAD_TIME = Animator.StringToHash("ReloadTime");
        static readonly int ANIM_HASH_FLOAT_EQUIP_TIME = Animator.StringToHash("EquipTime");
        static readonly int ANIM_HASH_FLOAT_ALERT_LEVEL = Animator.StringToHash("AlertLevel");


        static readonly int STATE_EQUIP = Animator.StringToHash("Equip");
        static readonly int STATE_RELOAD = Animator.StringToHash("Reload");
        static readonly int STATE_SHOOT = Animator.StringToHash("Shoot");
        static readonly int STATE_STANCE = Animator.StringToHash("Stance");


        public void Anim_Reload_StartLoopPoint() { }
        public void Anim_Reload_EndLoopPoint() { }
        public void Anim_Reload_LoadAmmo() { }
        public void Anim_Reload_PointOfNoReturn() { }
        public void Anim_Reload_FinishingFlourish() { }

        void Awake()
        {
            state = STATE_STANCE;
            gun = GetComponent<Gun>();
            anim = GetComponent<Animator>();
        }

        float aimValue = 0f;
        float sprintValue = 0f;
        public float crossfadeDuration = .1f;
        public float shoot_crossfadeDuration = .02f;
        public float shootLockTime = .2f;

        bool reloadValidChecked = false;

#if UNITY_EDITOR
        void ValidateReload()
        {
            if (reloadValidChecked)
                return;

            if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == STATE_RELOAD)
            {
                var clipInfo = anim.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length == 0)
                    return;

                if (clipInfo[0].clip != gun.reloadAnimation)
                    Debug.LogError(gun.gameObject.name + ": The reloadAnimation reference in the gun script does not match its current animator.");

                reloadValidChecked = true;
            }
        }
#endif

        void Update()
        {
            UpdateValues();

            UpdateState();

#if UNITY_EDITOR
            ValidateReload();
#endif
        }

        void UpdateValues()
        {
            sprintValue = Mathf.Clamp01(sprintValue + Time.deltaTime * gun.isSprinting.ToSign() / gun.sprintTransitionTime);
            aimValue = Mathf.Clamp01(aimValue + Time.deltaTime / (gun.isAiming ? gun.aim_zoomTime : -gun.aim_zoomResetTime));

            anim.SetFloat(ANIM_HASH_FLOAT_SPRINT_WEIGHT, sprintValue);
            anim.SetFloat(ANIM_HASH_FLOAT_AIM_WEIGHT, aimValue);
            anim.SetFloat(ANIM_HASH_FLOAT_RELOAD_TIME, gun.current_reloadPercentComplete);
            anim.SetFloat(ANIM_HASH_FLOAT_EQUIP_TIME, gun.equipPercent);
            anim.SetFloat(ANIM_HASH_FLOAT_ALERT_LEVEL, gun.current_alertLevel);
        }

        void UpdateState()
        {
            (int nextState, float crossfade) = GetState();
            if (state == nextState) return;

            anim.CrossFade(nextState, crossfade, 0);
            state = nextState;
        }

        float lockedTill = 0;
        (int, float) GetState()
        {
            if (gun.inEquipTransition)
                return (STATE_EQUIP, crossfadeDuration);
            if (gun.isReloading)
                return (STATE_RELOAD, crossfadeDuration);

            // if (Time.time < lockedTill) return state;

            if (gun.isSprinting)
                return (STATE_STANCE, crossfadeDuration);

            if (gun.isAlert) //TODO make a separate animation for alert, and a separate for shoot?
                return (STATE_SHOOT, shoot_crossfadeDuration);//LockState(STATE_SHOOT, .1f);

            return (STATE_STANCE, crossfadeDuration);

            // int LockState(int s, float t)
            // {
            //     lockedTill = Time.time + t;
            //     return s;
            // }
        }
    }
}
