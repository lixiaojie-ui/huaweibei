using UnityEngine;

namespace Daima
{
    /// <summary>
    /// 扫描标记的 2.5s 正弦呼吸：整体缩放 + 自发光强度轻微起伏。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DemoMarkerPulse : MonoBehaviour
    {
        public Material material;
        public Color color = new Color(0.098f, 0.784f, 1f);
        public float period = 2.5f;
        public float phase;
        public float emissionMin = 0.8f;
        public float emissionMax = 2.4f;
        public float scaleAmplitude = 0.05f;

        Vector3 baseScale;
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            baseScale = transform.localScale;
        }

        void Start()
        {
            if (material != null)
                material.EnableKeyword("_EMISSION");
        }

        void Update()
        {
            float wave = Mathf.Sin((Time.time / Mathf.Max(period, 0.01f) + phase) * Mathf.PI * 2f);
            float k = (wave + 1f) * 0.5f;

            transform.localScale = baseScale * (1f + wave * scaleAmplitude);

            if (material != null)
                material.SetColor(EmissionColorId, color * Mathf.Lerp(emissionMin, emissionMax, k));
        }
    }
}
