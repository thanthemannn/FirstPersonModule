using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class Projectile_Rigidbody : Projectile
    {
        Rigidbody rb;
        public float force = 10;
        public bool resetForceOnReflect = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        protected override void ShootAction(Vector3 direction)
        {
            float f = force;
            if (!resetForceOnReflect && currentHits.Count > 0)
                f = rb.velocity.magnitude;


            rb.velocity = direction * f;
        }
    }
}
