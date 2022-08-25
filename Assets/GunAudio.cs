using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Than.Projectiles
{
    [RequireComponent(typeof(Gun))]
    public class GunAudio : MonoBehaviour
    {
        Gun gun;
        public AudioSource standardAudio;
        public AudioSource oneShotAudio;

        public SoundEffect sfx_shoot = new SoundEffect(SoundEffect.Type.oneShot_interruptStandard);
        public SoundEffect sfx_reload_start = new SoundEffect(SoundEffect.Type.standard);
        public SoundEffect sfx_reload_loop = new SoundEffect(SoundEffect.Type.standard);
        public SoundEffect sfx_reload_complete = new SoundEffect(SoundEffect.Type.standard);

        public void PlaySFX_Shoot() => sfx_shoot.PlayFromExternal();
        public void PlaySFX_Reload() => sfx_reload_start.PlayFromExternal();
        public void PlaySFX_Reload_Partial() => sfx_reload_loop.PlayFromExternal();
        public void PlaySFX_Reload_Complete() => sfx_reload_complete.PlayFromExternal();

        public void StopSFX_Shoot() => sfx_shoot.StopFromExternal();
        public void StopSFX_Reload() => sfx_reload_start.StopFromExternal();
        public void StopSFX_Reload_Partial() => sfx_reload_loop.StopFromExternal();
        public void StopSFX_Reload_Complete() => sfx_reload_complete.StopFromExternal();

        public void OnEnable()
        {
            gun.onShoot += sfx_shoot.PlayFromGun;
            gun.onReloadStart += sfx_reload_start.PlayFromGun;
            gun.onReloadLoop += sfx_reload_loop.PlayFromGun;
            gun.onReloadEnd += sfx_reload_complete.PlayFromGun;
        }

        public void OnDisable()
        {
            gun.onShoot -= sfx_shoot.PlayFromGun;
            gun.onReloadStart -= sfx_reload_start.PlayFromGun;
            gun.onReloadLoop -= sfx_reload_loop.PlayFromGun;
            gun.onReloadEnd -= sfx_reload_complete.PlayFromGun;
        }

        public static FieldInfo[] reflection_sfxFields;
        public void Awake()
        {
            gun = GetComponent<Gun>();

            if (!standardAudio)
                standardAudio = gameObject.AddComponent<AudioSource>();
            if (!oneShotAudio)
                oneShotAudio = gameObject.AddComponent<AudioSource>();

            standardAudio.playOnAwake = false;
            oneShotAudio.playOnAwake = false;

            InitializeSFX();
        }

        void InitializeSFX()
        {
            //*Uses reflection so that we don't need to update any extra lists every time we add a new soundeffect

            //* Use Linq to build our reflection array for this class, only needed once because it's static
            if (reflection_sfxFields == null)
                reflection_sfxFields = this.GetType().GetFields().Where(x => x.FieldType == (typeof(SoundEffect))).ToArray();

            //* Casts the field info into our local sfx and initializes them
            for (int i = reflection_sfxFields.Length - 1; i >= 0; i--)
            {
                ((SoundEffect)reflection_sfxFields[i].GetValue(this)).Init(standardAudio, oneShotAudio);
            }
        }

        [System.Serializable]
        public class SoundEffect
        {
            public AudioClipGroup clip;
            bool hasClip = false;

            public bool allowCallsFromGunScript = true;
            public bool allowCallsFromExternalSources = true;
            public Type type = Type.standard;

            public enum Type { standard, loop, oneShot, oneShot_interruptStandard }

            public AudioSource audioSourceOverride;

            const int PLAY_REPEAT_THRESHOLD = 1;
            int lastPlayTime = -PLAY_REPEAT_THRESHOLD;

            AudioSource standardAudioSource;
            AudioSource oneShotAudioSource;

            public void Init(AudioSource standardAudioSource, AudioSource oneShotAudioSource)
            {
                hasClip = clip;

                if (audioSourceOverride)
                {
                    this.standardAudioSource = audioSourceOverride;
                    this.oneShotAudioSource = audioSourceOverride;
                }
                else
                {
                    this.standardAudioSource = standardAudioSource;
                    this.oneShotAudioSource = oneShotAudioSource;
                }
            }


            public SoundEffect(Type type)
            {
                this.type = type;
            }

            public void PlayFromExternal()
            {
                if (allowCallsFromExternalSources)
                    Play();
            }

            public void PlayFromGun()
            {
                if (allowCallsFromGunScript)
                    Play();
            }

            public void StopFromExternal()
            {
                if (allowCallsFromExternalSources)
                    Stop();
            }

            public void StopFromGun()
            {
                if (allowCallsFromGunScript)
                    Stop();
            }

            void Play()
            {
                if (!hasClip)
                    return;

                if (Time.frameCount < lastPlayTime + PLAY_REPEAT_THRESHOLD)
                    return;

                lastPlayTime = Time.frameCount;

                if (type == Type.oneShot)
                    oneShotAudioSource.PlayOneShot(clip);
                else if (type == Type.oneShot_interruptStandard)
                {
                    standardAudioSource.Stop();
                    oneShotAudioSource.PlayOneShot(clip);
                }
                else
                {
                    standardAudioSource.Stop();
                    standardAudioSource.clip = clip;
                    standardAudioSource.loop = type == Type.loop;
                    standardAudioSource.Play();
                }
            }

            public void Stop()
            {
                if (!hasClip)
                    return;

                if (standardAudioSource.isPlaying && standardAudioSource.clip == clip)
                    standardAudioSource.Stop();
            }
        }
    }
}