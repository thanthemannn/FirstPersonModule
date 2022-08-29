using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Than.Input;
using System;

namespace Than.Projectiles
{
    public class GunManager : MonoBehaviour
    {
        public Brain brain;

        List<Gun> guns = new List<Gun>();
        int current_gunIndex = 0;
        int next_gunIndex = 0;
        public float equipTransition_speedLimit = 6;

        void Awake()
        {
            if (guns.Count == 0)
                guns.AddRange(GetComponentsInChildren<Gun>(true));

            for (int i = guns.Count - 1; i >= 0; i--)
            {
                guns[i].gameObject.SetActive(false);
            }

            next_gunIndex = current_gunIndex;
            StartCoroutine(SwitchGunCoroutine());
        }

        void OnEnable()
        {
            brain.SwitchWeapon.onPress += SwitchToWeaponInput;
        }

        void OnDisable()
        {
            current_equipState = EquipState.none;
            brain.SwitchWeapon.onPress -= SwitchToWeaponInput;
            StopAllCoroutines();
        }

        private void SwitchToWeaponInput()
        {
            if (!guns[current_gunIndex].IsActionCancellable)
                return;

            int dir = UMath.GetSignIfValue(Mathf.RoundToInt(brain.SwitchWeapon.value));

            next_gunIndex = UMath.Mod(current_gunIndex + dir, guns.Count);

            if (current_equipState != EquipState.unequipping)
            {
                StopAllCoroutines();
                StartCoroutine(SwitchGunCoroutine());
            }
        }

        public enum EquipState { none, equipping, unequipping };
        public EquipState current_equipState { get; private set; } = EquipState.none;

        IEnumerator SwitchGunCoroutine()
        {
            current_equipState = EquipState.unequipping;
            guns[current_gunIndex].Equip(false);
            while (guns[current_gunIndex].gameObject.activeSelf)
            {
                yield return null;
                guns[current_gunIndex].physicsBody.ApplyMoveSpeedLimit_Air(equipTransition_speedLimit);
                guns[current_gunIndex].physicsBody.ApplyMoveSpeedLimit_Ground(equipTransition_speedLimit);
            }

            current_equipState = EquipState.equipping;
            current_gunIndex = next_gunIndex;
            guns[current_gunIndex].Equip(true);
            do
            {
                yield return null;
                guns[current_gunIndex].physicsBody.ApplyMoveSpeedLimit_Air(equipTransition_speedLimit);
                guns[current_gunIndex].physicsBody.ApplyMoveSpeedLimit_Ground(equipTransition_speedLimit);
            }
            while (guns[current_gunIndex].inEquipTransition);
            current_equipState = EquipState.none;
        }
    }
}
