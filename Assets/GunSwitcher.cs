using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Projectiles
{
    public class GunSwitcher : MonoBehaviour
    {
        List<Gun> guns = new List<Gun>();
        void Awake()
        {
            if (guns.Count == 0)
                guns.AddRange(GetComponentsInChildren<Gun>(true));

            for (int i = guns.Count - 1; i > 0; i--)
            {
                guns[i].gameObject.SetActive(false);
            }

            if (guns.Count > 0)
                guns[0].gameObject.SetActive(true);
        }

        public void SwitchToWeapon()
        {

        }
    }
}
