using UnityEngine;
using UnityEngine.UI;

namespace Than.Rendering
{
    [RequireComponent(typeof(RawImage))]
    public class RawImage_DrawRenderTextureCam : MonoBehaviour
    {
        public RenderTextureCam renderCam;

        RawImage rawImage;

        void OnEnable()
        {
            if (renderCam)
                Subscribe();
        }

        private void OnValidate()
        {
            if (renderCam)
                Subscribe();
        }

        void Subscribe()
        {
            if (!rawImage)
                rawImage = GetComponent<RawImage>();

            renderCam.onRegen -= Assign;
            renderCam.onRegen += Assign;
            Assign(renderCam.cam.targetTexture);
        }

        void Assign(RenderTexture tex)
        {
            rawImage.texture = tex;
        }
    }
}