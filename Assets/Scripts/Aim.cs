using UnityEngine;
using Than.Input;

namespace Than
{
    public class Aim : MonoBehaviour
    {
        #region Inspector Fields

        public Brain brain;
        public Transform head;
        public float sensitivityModifier = 1;
        public float yAxisClamp = 85;

        #endregion

        #region Other Variables and Properties

        Vector3 headDirection = Vector3.zero;

        #endregion

        #region Unity Methods

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void LateUpdate()
        {
            LookUpdate();
        }

        #endregion

        #region Aim Processing

        void LookUpdate()
        {
            Vector2 lookDirection = brain.Look;
            lookDirection *= sensitivityModifier * Time.deltaTime;

            headDirection.x -= lookDirection.y;
            headDirection.x = Mathf.Clamp(headDirection.x % 360, -yAxisClamp, yAxisClamp);
            headDirection.y += lookDirection.x;

            transform.Rotate(Vector3.up * lookDirection.x, Space.Self);//eulerAngles = new Vector3(transform.eulerAngles.z, headDirection.y, transform.eulerAngles.z);
            head.localEulerAngles = new Vector3(headDirection.x, 0, 0);
        }

        #endregion
    }
}