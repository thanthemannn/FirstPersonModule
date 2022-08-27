using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Than.Projectiles
{
    [RequireComponent(typeof(TMP_Text))]
    public class Gun_AmmoDisplay : MonoBehaviour
    {
        TMP_Text textBox;
        public Gun gun;

        void Awake()
        {
            textBox = GetComponent<TMP_Text>();
        }

        void Update()
        {
            textBox.SetText(gun.currentClipAmmo.ToString());
        }
    }
}