using System;
using UnityEngine.InputSystem;

using UnityEngine;

namespace Than.Input
{
    [DefaultExecutionOrder(-5)]
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerBrain : MonoBehaviour
    {
        Brain brain;
        PlayerInput playerInput;
        public float mouse_lookMultiplier = 20;
        public float gamepad_lookMultiplier = 200;

        bool usingGamepad => !playerInput.currentControlScheme.StartsWith("Keyboard", false, System.Globalization.CultureInfo.CurrentCulture);
        public const string CUSTOM_INPUT_PREFIX = "Custom_";

        protected void Start()
        {
            brain = GetComponent<Brain>();
            playerInput = GetComponent<PlayerInput>();
            RegisterInputs();
        }

        #region Custom Inputs

        //*
        //* All Custom input methods should use the name convention "Custom_" + input.name
        //* ie. "Custom_Look"
        //*

        public Vector2 lookRaw { get; private set; }
        public Vector2 Custom_Look()
        {
            lookRaw = playerInput.actions[brain.Look.name].ReadValue<Vector2>();
            return lookRaw * (usingGamepad ? gamepad_lookMultiplier : mouse_lookMultiplier);
        }

        #endregion

        #region Input Registration
        InputAction GetAction(string actionName)
        {
            try { return playerInput.actions[actionName]; }
            catch
            {
                Debug.LogWarning(playerInput + " does not reference an InputActionMap with any InputAction named " + actionName);
                return null;
            }
        }

        Func<T> GetCustomInputMethod<T>(string actionName)
        {
            var m = this.GetType().GetMethod(String.Concat(CUSTOM_INPUT_PREFIX, actionName));
            if (m != null && m.ReturnType == typeof(T)) return () => (T)m.Invoke(this, null);

            return null;
        }

        /// <summary>
        /// Goes through all the exposed input properties in the base brain class and attempts to connect them to actions within the PlayerInput action maps.
        /// <para>Supports custom input rerouting by creating methods in the PlayerBrain class with the prefix "Custom_".</para>
        /// </summary>
        void RegisterInputs()
        {
            //*Register Vector2 inputs
            foreach (var input in brain.allVector2Inputs)
            {
                Func<Vector2> func = GetCustomInputMethod<Vector2>(input.name);
                if (func == null)
                {
                    InputAction action = GetAction(input.name);
                    if (action == null) return;
                    func = () => action.ReadValue<Vector2>();
                }

                input.Register(func);
            }

            //*Register bool inputs
            foreach (var input in brain.allBoolInputs)
            {
                Func<bool> func = GetCustomInputMethod<bool>(input.name);
                if (func == null)
                {
                    InputAction action = GetAction(input.name);
                    if (action == null) return;
                    func = () => action.ReadValue<float>() > .5f;
                }

                input.Register(func);
            }

            //*Register float inputs
            foreach (var input in brain.allFloatInputs)
            {
                Func<float> func = GetCustomInputMethod<float>(input.name);
                if (func == null)
                {
                    InputAction action = GetAction(input.name);
                    if (action == null) return;
                    func = () => action.ReadValue<float>();
                }

                input.Register(func);
            }
        }

        #endregion
    }
}