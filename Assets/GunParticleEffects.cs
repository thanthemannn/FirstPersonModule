using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    [RequireComponent(typeof(Gun))]
    public class GunParticleEffects : MonoBehaviour
    {
        Gun gun;
        public ParticleSystem muzzleFlash;

        void Awake()
        {
            gun = GetComponent<Gun>();
        }

        void OnEnable()
        {
            if (muzzleFlash) gun.onShoot += MuzzleFlash;
        }

        void OnDisable()
        {
            gun.onShoot += MuzzleFlash;
        }

        private void MuzzleFlash()
        {
            muzzleFlash.Play();
        }
    }
}