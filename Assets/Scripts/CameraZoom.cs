using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace Than
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CameraZoom : MonoBehaviour
    {
        CinemachineVirtualCamera virtualCamera;

        public AnimationCurve zoomInCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve zoomResetCurve = AnimationCurve.Linear(0, 1, 1, 0);

        float default_fov; //TODO allow this to be changed in game settings somewhere!!!

        float current_zoomStart;
        float current_zoomEnd;
        float current_zoomSpeed;
        float GetZoomSpeed() => current_zoomSpeed;

        void Awake()
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
            default_fov = virtualCamera.m_Lens.FieldOfView;
        }

        public void SetZoom(float multiplier, float zoomTime)
        {
            current_zoomStart = virtualCamera.m_Lens.FieldOfView;
            current_zoomEnd = default_fov / multiplier;
            current_zoomSpeed = 1f / zoomTime;
            StopAllCoroutines();
            StartCoroutine(zoomInCurve.Animate(AnimUpdateZoomLevel, GetZoomSpeed));
        }

        public void ResetZoom(float resetTime)
        {
            current_zoomStart = default_fov;
            current_zoomEnd = virtualCamera.m_Lens.FieldOfView;
            current_zoomSpeed = 1f / resetTime;
            StopAllCoroutines();
            StartCoroutine(zoomResetCurve.Animate(AnimUpdateZoomLevel, GetZoomSpeed));
        }

        void AnimUpdateZoomLevel(float t)
        {
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(current_zoomStart, current_zoomEnd, t);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            virtualCamera.m_Lens.FieldOfView = default_fov;
        }
    }
}