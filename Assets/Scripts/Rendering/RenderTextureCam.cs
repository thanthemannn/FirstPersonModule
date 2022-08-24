using UnityEngine;

namespace Than.Rendering
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class RenderTextureCam : MonoBehaviour
    {
        public Camera cam { get; protected set; }
        public float resolutionScale = 1;
        Vector2 currentScreenRes;

        [SerializeField] bool forceRegen = false;

        public System.Action<RenderTexture> onRegen;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void OnEnable()
        {
            if (!cam)
                cam = GetComponent<Camera>();
        }

        void OnValidate()
        {
            if (forceRegen)
            {
                forceRegen = false;
                RegenTexture();
            }
        }

        public void RegenTexture()
        {
            currentScreenRes = new Vector2(Screen.width, Screen.height);

            int width = (int)(currentScreenRes.x * resolutionScale);
            int height = (int)(currentScreenRes.y * resolutionScale);

            if (cam.targetTexture != null)
            {
                cam.targetTexture.Release();
            }

            RenderTexture tex = new RenderTexture(width, height, 24);
            tex.useDynamicScale = true;

            cam.targetTexture = tex;

            onRegen?.Invoke(tex);
        }

        void Update()
        {
            if ((Screen.width != currentScreenRes.x || Screen.height != currentScreenRes.y))
            {
                RegenTexture();
            }
        }

    }
}