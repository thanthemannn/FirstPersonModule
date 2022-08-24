using UnityEngine;

namespace Than.Projectiles
{
    public class GunMessenger : MonoBehaviour
    {
        public Gun gun;
        void Awake()
        {
            if (!gun) gun = GetComponentInParent<Gun>();
        }

        public void Reload_Finish()
        {
            gun.Reload_Finish();
        }
    }
}