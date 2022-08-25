using UnityEngine;
using System.Linq;

namespace Than.Input
{
    [DefaultExecutionOrder(-10)]
    public class Brain : MonoBehaviour
    {
        #region Input Types

        //* Brain uses reflection, so usually no more work is required other than declaring the input here!
        //* Please provide the name that is used within the InputSystems action map for each input constructor

        //* To actually process input in derived classes of brain, simply use Input.Register(Func<T> readAction)
        //* ie. Move.Register(controls.FirstPerson.Move.ReadValue<Vector2>());
        public Vector2Input Move { get; protected set; } = new Vector2Input("Move");
        public Vector2Input Look { get; protected set; } = new Vector2Input("Look");

        public BoolInput Sprint { get; protected set; } = new BoolInput("Sprint");
        public BoolInput Jump { get; protected set; } = new BoolInput("Jump");
        public BoolInput Shoot { get; protected set; } = new BoolInput("Shoot");
        public BoolInput Aim { get; protected set; } = new BoolInput("Aim");
        public BoolInput Reload { get; protected set; } = new BoolInput("Reload");
        public BoolInput Crouch { get; protected set; } = new BoolInput("Crouch");

        public FloatInput SwitchWeapon { get; protected set; } = new FloatInput("SwitchWeapon");

        #endregion


        #region Input Generation

        public InputBase[] allInputs { get; protected set; }
        public Vector2Input[] allVector2Inputs { get; protected set; }
        public BoolInput[] allBoolInputs { get; protected set; }
        public FloatInput[] allFloatInputs { get; protected set; }
        public int allInputs_len { get; protected set; }

        void GenerateInputs()
        {
            //*Populate allInputs with all applicable properties deriving from InputBase
            allInputs = this.GetType().GetProperties()
                .Where(x => x.PropertyType.IsSubclassOf(typeof(InputBase)))
                .Select(x => (InputBase)x.GetValue(this)).ToArray();
            allInputs_len = allInputs.Length;

            //*Subsequent arrays to hold specific input types
            allVector2Inputs = allInputs.Where(x => x is Vector2Input).Cast<Vector2Input>().ToArray();
            allBoolInputs = allInputs.Where(x => x is BoolInput).Cast<BoolInput>().ToArray();
            allFloatInputs = allInputs.Where(x => x is FloatInput).Cast<FloatInput>().ToArray();
        }

        #endregion

        #region Standard Methods

        protected virtual void Awake()
        {
            GenerateInputs();
        }

        protected virtual void Update()
        {
            for (int i = 0; i < allInputs_len; i++)
            {
                allInputs[i].StartFrameProcessing();
            }
        }

        protected virtual void LateUpdate()
        {
            for (int i = 0; i < allInputs_len; i++)
            {
                allInputs[i].EndFrameProcessing();
            }
        }

        #endregion

        #region SubClasses

        public class BoolInput : Input<bool>
        {
            public BoolInput(string name) : base(name) { }
            protected override bool HeldCheck() => value;
        }

        public class FloatInput : Input<float>
        {
            public FloatInput(string name) : base(name) { }
            protected override bool HeldCheck() => value != 0;
        }

        public class Vector2Input : Input<Vector2>
        {
            public Vector2Input(string name) : base(name) { }
            protected override bool HeldCheck() => value != Vector2.zero;
        }

        public abstract class Input<T> : InputBase
        {
            public T value;

            public Input(string name) : base(name) { }

            System.Func<T> readValue;
            bool readDelegateRegistered = false;

            protected abstract bool HeldCheck();

            /// <summary>
            /// Provide a callback delegate that will be called each frame to process input.
            /// </summary>
            /// <param name="readAction">The method to read our input value from.</param>
            public void Register(System.Func<T> readAction)
            {
                readValue -= readAction;
                readValue += readAction;
                readDelegateRegistered = readValue != null;
            }

            public void UnRegister(System.Func<T> readAction)
            {
                readValue -= readAction;
                readDelegateRegistered = readValue != null;
            }

            public override void StartFrameProcessing()
            {
                if (readDelegateRegistered)
                    value = readValue.Invoke();

                heldBuffer = HeldCheck();

                pressedThisFrame = heldBuffer && !held;
                releasedThisFrame = !heldBuffer && held;

                held = heldBuffer;

                if (pressedThisFrame)
                {
                    onPress?.Invoke();
                    onHeldChange?.Invoke(true);
                }

                if (releasedThisFrame)
                {
                    onRelease?.Invoke();
                    onHeldChange?.Invoke(false);
                }

            }

            public override void EndFrameProcessing()
            {
                pressedThisFrame = false;
                releasedThisFrame = false;
            }

            public static implicit operator T(Input<T> i) => i.value;
        }

        public abstract class InputBase
        {
            public InputBase(string name) { this.name = name; }

            public string name { get; private set; }
            public bool pressedThisFrame { get; protected set; }

            protected bool heldBuffer;
            public bool held { get; protected set; }
            public bool releasedThisFrame { get; protected set; }

            public System.Action onPress;
            public System.Action onRelease;

            /// <summary>
            /// Called whenever the action is pressed OR released.
            /// </summary>
            public System.Action<bool> onHeldChange;

            public abstract void StartFrameProcessing();
            public abstract void EndFrameProcessing();
        }
        #endregion
    }
}