using UnityEngine;

namespace Daima
{
    /// <summary>
    /// 只在挂载相机的渲染期间替换天空盒，渲染结束后恢复，
    /// 这样其它相机（factory 视角）看到的仍是原场景天空盒。
    /// 同时可临时启用补光、提高环境光强度，让车间在这个视角下更亮；
    /// 网格/天空盒都是无光照 Shader，不受影响。
    /// </summary>
    public sealed class CameraSkyboxOverride : MonoBehaviour
    {
        public Material skyboxMaterial;
        public Light[] fillLights;
        public float ambientIntensityMultiplier = 1f;

        Material originalSkybox;
        bool[] fillLightStates;
        float originalAmbientIntensity;
        bool ambientCaptured;

        void OnPreRender()
        {
            originalSkybox = RenderSettings.skybox;
            if (skyboxMaterial != null)
                RenderSettings.skybox = skyboxMaterial;

            originalAmbientIntensity = RenderSettings.ambientIntensity;
            ambientCaptured = true;
            if (ambientIntensityMultiplier > 0f)
                RenderSettings.ambientIntensity = originalAmbientIntensity * ambientIntensityMultiplier;

            if (fillLights != null)
            {
                if (fillLightStates == null || fillLightStates.Length != fillLights.Length)
                    fillLightStates = new bool[fillLights.Length];

                for (int i = 0; i < fillLights.Length; i++)
                {
                    Light light = fillLights[i];
                    if (light == null) continue;
                    fillLightStates[i] = light.enabled;
                    light.enabled = true;
                }
            }
        }

        void OnPostRender()
        {
            RenderSettings.skybox = originalSkybox;
            RestoreAmbient();
            RestoreLights();
        }

        void OnDisable()
        {
            if (originalSkybox != null)
                RenderSettings.skybox = originalSkybox;
            RestoreAmbient();
            RestoreLights();
        }

        void RestoreAmbient()
        {
            if (!ambientCaptured) return;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            ambientCaptured = false;
        }

        void RestoreLights()
        {
            if (fillLights == null || fillLightStates == null) return;
            for (int i = 0; i < fillLights.Length && i < fillLightStates.Length; i++)
            {
                Light light = fillLights[i];
                if (light == null) continue;
                light.enabled = fillLightStates[i];
            }
        }
    }
}

