using UnityEngine;

namespace Daima
{
    /// <summary>
    /// 替代 Cinemachine：固定俯角，缓慢向右横移（0.1m/s，最多 2m），叠加很轻的 Perlin 噪声。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DemoCameraRig : MonoBehaviour
    {
        public float pitch = 15f;
        public float panSpeed = 0.1f;
        public float panDistance = 2f;
        public float noiseAmplitude = 0.3f;
        public float noiseFrequency = 0.3f;

        Vector3 origin;
        float yaw;
        float seed;

        void Awake()
        {
            origin = transform.position;
            yaw = transform.eulerAngles.y;
            seed = Random.value * 100f;

            Camera camera = GetComponent<Camera>();
            if (camera != null)
                camera.fieldOfView = 35f;
        }

        void LateUpdate()
        {
            float time = Time.time;
            float pan = Mathf.Min(time * panSpeed, panDistance);
            float nx = (Mathf.PerlinNoise(seed, time * noiseFrequency) - 0.5f) * 2f * noiseAmplitude;
            float ny = (Mathf.PerlinNoise(seed + 31.7f, time * noiseFrequency) - 0.5f) * 2f * noiseAmplitude;

            transform.position = origin + Vector3.right * (pan + nx) + Vector3.up * ny;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
