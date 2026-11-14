using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace Daima
{
    /// <summary>
    /// 运行时给“展示车间场景”铺一套科技感动态背景：
    /// 深色渐变星空天幕 + 发光网格地面 + 漂浮粒子 + Bloom 后处理。
    /// 只在看向车间的第二台相机上生效，factory 的全局天空盒和环境光完全不动。
    /// 不改场景文件，进入 Play 后自动生成；场景里没有车间模型时不做任何事。
    /// </summary>
    public static class TechBackground
    {
        const string RootName = "[TechBackground]";
        const string ContentRootName = "展示车间场景";

        // 31 层在工程里未被使用，专供车间背景；别的相机剔除掉就看不到这些物体
        const int BackgroundLayer = 31;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (GameObject.Find(RootName) != null)
                return;

            Bounds bounds;
            if (!TryGetContentBounds(out bounds))
            {
                Debug.LogWarning("[TechBackground] 没有找到“" + ContentRootName + "”的网格，动态背景未创建。");
                return;
            }

            Camera mainCamera = Camera.main;
            Camera workshopCamera = FindWorkshopCamera(mainCamera);
            if (workshopCamera == null)
            {
                Debug.LogWarning("[TechBackground] 没有找到车间相机，动态背景未创建。");
                return;
            }

            GameObject root = new GameObject(RootName);

            CreateGround(root.transform, bounds);
            CreateGrid(root.transform, bounds, workshopCamera);
            CreateLightBeams(root.transform, bounds, workshopCamera);
            SetLayerRecursively(root, BackgroundLayer);

            ApplyCameraMasks(workshopCamera);
            AttachSkyboxOverride(workshopCamera);
            SetupPostProcessing(root.transform, workshopCamera);

            Debug.Log("[TechBackground] 暗场背景已生成（只作用于 " + workshopCamera.gameObject.name +
                "，模型半径 " + Mathf.Max(bounds.extents.x, bounds.extents.z).ToString("F1") + "m）。");
        }

        static Camera FindWorkshopCamera(Camera mainCamera)
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>(true);

            foreach (Camera camera in cameras)
                if (camera != null && camera != mainCamera && camera.gameObject.name == "Camera")
                    return camera;

            foreach (Camera camera in cameras)
                if (camera != null && camera != mainCamera)
                    return camera;

            return null;
        }

        static void ApplyCameraMasks(Camera workshopCamera)
        {
            int backgroundMask = 1 << BackgroundLayer;
            Camera[] cameras = Object.FindObjectsOfType<Camera>(true);

            foreach (Camera camera in cameras)
            {
                if (camera == null) continue;
                if (camera == workshopCamera)
                    camera.cullingMask |= backgroundMask;
                else
                    camera.cullingMask &= ~backgroundMask;
            }
        }

        static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        /// <summary>
        /// 给车间相机挂一个只在它渲染期间生效的天空盒替代组件；
        /// RenderSettings.skybox 在渲染结束后立刻恢复，factory 视角仍是原来的天空盒。
        /// </summary>
        static void AttachSkyboxOverride(Camera workshopCamera)
        {
            Shader shader = Shader.Find("Daima/TechSkybox");
            if (shader == null)
            {
                Debug.LogWarning("[TechBackground] 找不到 Daima/TechSkybox Shader。");
                return;
            }

            // 环境光固定成原场景的三色，避免天空盒临时切换时模型失去环境光变黑
            if (RenderSettings.ambientMode == AmbientMode.Skybox)
                RenderSettings.ambientMode = AmbientMode.Trilight;

            CameraSkyboxOverride skyboxOverride = workshopCamera.GetComponent<CameraSkyboxOverride>();
            if (skyboxOverride == null)
                skyboxOverride = workshopCamera.gameObject.AddComponent<CameraSkyboxOverride>();

            Material skybox = new Material(shader) { name = "DarkGroundSkybox (Runtime)" };
            skybox.SetColor("_TopColor", new Color(0.003f, 0.0065f, 0.013f));
            skybox.SetColor("_HorizonColor", new Color(0.020f, 0.040f, 0.072f));
            skybox.SetColor("_BottomColor", new Color(0.002f, 0.004f, 0.008f));
            skybox.SetColor("_GlowColor", new Color(0.08f, 0.20f, 0.34f));
            skybox.SetFloat("_GlowStrength", 0.18f);
            skybox.SetFloat("_StarDensity", 0.12f);
            skybox.SetFloat("_StarBrightness", 0.3f);
            skybox.SetFloat("_Vignette", 0.7f);
            skyboxOverride.skyboxMaterial = skybox;

            // 车间视角专用补光：只在这个相机渲染期间启用，其它视角（factory）不变；
            // 网格和天空盒是无光照 Shader，亮度不受这盏灯影响
            GameObject fillGo = new GameObject("Workshop Fill Light");
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.88f, 0.94f, 1f);
            fill.intensity = 1.25f;
            fill.shadows = LightShadows.None;
            fill.cullingMask = ~(1 << BackgroundLayer);
            fillGo.transform.rotation = Quaternion.Euler(38f, -20f, 0f);
            fill.enabled = false;

            GameObject rimGo = new GameObject("Workshop Rim Light");
            Light rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.70f, 0.82f, 1f);
            rim.intensity = 0.7f;
            rim.shadows = LightShadows.None;
            rim.cullingMask = ~(1 << BackgroundLayer);
            rimGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            rim.enabled = false;

            skyboxOverride.fillLights = new Light[] { fill, rim };
            skyboxOverride.ambientIntensityMultiplier = 1.6f;

            Debug.Log("[TechBackground] 车间视角补光已挂载：环境光 ×1.6 + 主补光 1.25 + 轮廓光 0.7。");
        }

        static bool TryGetContentBounds(out Bounds bounds)
        {
            bounds = new Bounds();
            GameObject content = GameObject.Find(ContentRootName);
            if (content == null)
                return false;

            Renderer[] renderers = content.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

                Bounds rendererBounds = renderer.bounds;
                if (rendererBounds.size.sqrMagnitude < 0.0001f) continue;

                if (!hasBounds)
                {
                    bounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }

            return hasBounds;
        }

        /// <summary>
        /// 半透明深蓝地面，压暗车间原来的地板，让发光网格看起来浮在暗色地面上。
        /// </summary>
        static void CreateGround(Transform parent, Bounds bounds)
        {
            Shader shader = Shader.Find("Daima/TechGround");
            if (shader == null)
            {
                Debug.LogWarning("[TechBackground] 找不到 Daima/TechGround Shader。");
                return;
            }

            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float fadeDistance = Mathf.Clamp(radius * 2.2f, 60f, 220f);
            float planeSize = fadeDistance * 2.2f;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(
                bounds.center.x,
                bounds.min.y + Mathf.Clamp(radius * 0.004f, 0.03f, 0.5f) - 0.02f,
                bounds.center.z);
            ground.transform.localScale = new Vector3(planeSize / 10f, 1f, planeSize / 10f);

            Collider collider = ground.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            Material material = new Material(shader) { name = "TechGround (Runtime)" };
            material.SetColor("_Color", new Color(0.016f, 0.032f, 0.055f, 1f));
            material.SetFloat("_Alpha", 0.75f);
            material.SetVector("_Center", new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, 0f));
            material.SetFloat("_Radius", fadeDistance * 1.15f);

            MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        static void CreateGrid(Transform parent, Bounds bounds, Camera camera)
        {
            Shader shader = Shader.Find("DarkGround/HoloGrid");
            if (shader == null)
            {
                Debug.LogWarning("[TechBackground] 找不到 DarkGround/HoloGrid Shader。");
                return;
            }

            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float cameraDistance = camera != null
                ? Vector3.Distance(camera.transform.position, bounds.center)
                : radius * 2f;
            // 网格只铺在车间周围，不再铺满整个背景
            float fadeEnd = Mathf.Clamp(cameraDistance * 3.5f, 60f, 160f);
            float fadeStart = fadeEnd * 0.45f;
            float planeSize = fadeEnd * 2.2f;
            // 屏幕上一个格子约 30px，线细、格子疏，避免变成一堵网格墙
            float cell = Mathf.Clamp(ScreenWidthAt(bounds.center, camera) * 0.018f, 0.6f, 1.6f);
            // 网格放在模型地板下方一点：只铺在车间周围，被地板挡住的部分不会叠在模型表面
            float groundY = bounds.min.y - 0.05f;

            GameObject grid = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grid.name = "Grid";
            grid.transform.SetParent(parent, false);
            grid.transform.position = new Vector3(bounds.center.x, groundY, bounds.center.z);
            // Unity 内置 Plane 是 10x10，缩放到需要的覆盖范围
            grid.transform.localScale = new Vector3(planeSize / 10f, 1f, planeSize / 10f);

            Collider collider = grid.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            Material material = new Material(shader) { name = "HoloGrid (Runtime)" };
            // 颜色按 2^(-0.5EV) 预先压暗：曝光从 0.35 提到 0.85 后，网格像素亮度保持不变
            material.SetColor("_Color", new Color(0.060f, 0.438f, 0.636f, 1f));
            material.SetFloat("_CellSize", cell);
            material.SetFloat("_MajorEvery", 5f);
            material.SetFloat("_MinorWidth", cell * 0.015f);
            material.SetFloat("_MajorWidth", cell * 0.035f);
            material.SetFloat("_MinorIntensity", 0.28f);
            material.SetFloat("_MajorIntensity", 0.65f);
            material.SetFloat("_Intensity", 0.55f);
            material.SetFloat("_FadeStart", fadeStart);
            material.SetFloat("_FadeEnd", fadeEnd);
            material.SetFloat("_ScanPeriod", 12f);
            material.SetFloat("_ScanWidth", Mathf.Clamp(radius * 0.3f, 3f, 10f));
            material.SetFloat("_ScanIntensity", 0.32f);
            material.SetFloat("_ScanMinZ", bounds.min.z - radius * 0.6f);
            material.SetFloat("_ScanMaxZ", bounds.max.z + radius * 0.6f);
            material.SetFloat("_FlowIntensity", 0.9f);
            material.SetFloat("_FlowWavelength", Mathf.Clamp(cell * 24f, 10f, 30f));
            material.SetFloat("_FlowSpeed", 2.2f);
            material.SetFloat("_SpotSpacing", Mathf.Clamp(cell * 12f, 6f, 14f));
            material.SetFloat("_SpotSize", Mathf.Clamp(cell * 0.35f, 0.2f, 0.5f));
            material.SetFloat("_SpotIntensity", 0.55f);
            material.SetFloat("_SpotSpeed", 0.22f);

            MeshRenderer renderer = grid.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        static Vector3 GetFlatForward(Camera camera, Bounds bounds)
        {
            Vector3 forward = camera != null
                ? bounds.center - camera.transform.position
                : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            return forward.normalized;
        }

        /// <summary>
        /// 车间外圈的 3 根细光柱；按 AABB 支撑距离放置，保证在模型占地范围之外。
        /// </summary>
        static void CreateLightBeams(Transform parent, Bounds bounds, Camera camera)
        {
            Shader beamShader = Shader.Find("DarkGround/LightBeam");
            if (beamShader == null)
            {
                Debug.LogWarning("[TechBackground] 找不到 DarkGround/LightBeam Shader。");
                return;
            }

            float groundY = bounds.min.y + 0.02f;
            Vector3 center = new Vector3(bounds.center.x, groundY, bounds.center.z);
            Vector3 forward = GetFlatForward(camera, bounds);
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            GameObject beams = new GameObject("LightBeams");
            beams.transform.SetParent(parent, false);

            Shader glowShader = Shader.Find("DarkGround/ParticleAdditive");
            Material baseGlowMaterial = null;
            if (glowShader != null)
            {
                baseGlowMaterial = new Material(glowShader) { name = "Beam Base Glow (Runtime)" };
                baseGlowMaterial.SetColor("_TintColor", new Color(0.098f, 0.784f, 1f, 1f));
                baseGlowMaterial.SetFloat("_Intensity", 0.7f);
                baseGlowMaterial.SetFloat("_UseVertexColor", 0f);
            }

            float[] angles = { -110f, -25f, 60f };
            float[] heightScales = { 1.35f, 1.55f, 1.45f };

            for (int i = 0; i < angles.Length; i++)
            {
                float radians = angles[i] * Mathf.Deg2Rad;
                Vector3 direction = forward * Mathf.Cos(radians) + right * Mathf.Sin(radians);
                // 沿该方向的 AABB 支撑半径
                float surface = Mathf.Abs(direction.x) * bounds.extents.x
                              + Mathf.Abs(direction.z) * bounds.extents.z;
                float margin = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.45f;
                Vector3 position = center + direction * (surface + margin);

                float height = Mathf.Clamp(bounds.size.y * heightScales[i], 10f, 30f);
                float quadWidth = Mathf.Clamp(height * 0.09f, 0.8f, 2.2f);

                GameObject beamRoot = new GameObject("LightBeam " + (i + 1));
                beamRoot.transform.SetParent(beams.transform, false);
                beamRoot.transform.position = new Vector3(position.x, groundY, position.z);

                Material beamMaterial = new Material(beamShader) { name = "Light Beam " + (i + 1) };
                beamMaterial.SetColor("_EmissionColor", new Color(0.098f, 0.784f, 1f, 1f));
                beamMaterial.SetFloat("_Intensity", 0.9f);
                beamMaterial.SetFloat("_Phase", i * 0.37f);

                // 两张交叉 Quad，绕水平方向看时总有一张是正面
                CreatePrimitive("Beam A", PrimitiveType.Quad, beamRoot.transform,
                    new Vector3(0f, height * 0.5f, 0f),
                    new Vector3(quadWidth, height, 1f), beamMaterial);
                GameObject quadB = CreatePrimitive("Beam B", PrimitiveType.Quad, beamRoot.transform,
                    new Vector3(0f, height * 0.5f, 0f),
                    new Vector3(quadWidth, height, 1f), beamMaterial);
                quadB.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                if (baseGlowMaterial != null)
                {
                    float baseSize = Mathf.Clamp(height * 0.08f, 0.8f, 2.5f);
                    GameObject baseGlow = CreatePrimitive("Base Glow " + (i + 1), PrimitiveType.Quad,
                        beams.transform,
                        new Vector3(position.x, groundY + 0.02f, position.z),
                        new Vector3(baseSize, baseSize, 1f), baseGlowMaterial);
                    baseGlow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }

                DemoMarkerPulse pulse = beamRoot.AddComponent<DemoMarkerPulse>();
                pulse.material = beamMaterial;
                pulse.color = new Color(0.098f, 0.784f, 1f);
                pulse.period = 4f + i * 0.5f;
                pulse.phase = i * 0.27f;
                pulse.emissionMin = 0.7f;
                pulse.emissionMax = 1.25f;
                pulse.scaleAmplitude = 0.1f;
            }
        }

        static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return go;
        }


        /// <summary>
        /// 指定世界位置处、一屏幕宽对应的世界尺寸；用于让网格/光点在不同大小的车间模型上保持相近的视觉占比。
        /// </summary>
        static float ScreenWidthAt(Vector3 worldPosition, Camera camera)
        {
            if (camera == null)
                return 100f;

            float distance = Vector3.Distance(camera.transform.position, worldPosition);
            float aspect = camera.aspect > 0.01f
                ? camera.aspect
                : (float)Screen.width / Mathf.Max(Screen.height, 1);
            float halfHeight = distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return halfHeight * 2f * aspect;
        }



        static void SetupPostProcessing(Transform parent, Camera workshopCamera)
        {
            PostProcessLayer[] layers = Object.FindObjectsOfType<PostProcessLayer>(true);
            if (layers.Length == 0)
            {
                Debug.LogWarning("[TechBackground] 场景里没有 PostProcessLayer，Bloom 未启用。");
                return;
            }

            PostProcessLayer source = null;
            foreach (PostProcessLayer layer in layers)
            {
                if (layer != null)
                {
                    source = layer;
                    break;
                }
            }
            if (source == null)
                return;

            PostProcessLayer workshopLayer = workshopCamera.GetComponent<PostProcessLayer>();
            if (workshopLayer == null)
            {
                PostProcessResources resources = GetResources(source);
                if (resources == null)
                {
                    Debug.LogWarning("[TechBackground] 拿不到 PostProcessResources，Bloom 未启用。");
                    return;
                }

                workshopLayer = workshopCamera.gameObject.AddComponent<PostProcessLayer>();
                workshopLayer.Init(resources);
                workshopLayer.volumeTrigger = workshopCamera.transform;
                workshopLayer.stopNaNPropagation = source.stopNaNPropagation;
            }

            // 这层后期只挂到车间相机，Main Camera（factory 视角）保持原样
            int backgroundMask = 1 << BackgroundLayer;
            workshopLayer.volumeLayer = backgroundMask;
            foreach (PostProcessLayer layer in layers)
                if (layer != null && layer != workshopLayer)
                    layer.volumeLayer = layer.volumeLayer.value & ~backgroundMask;

            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            profile.name = "TechBackground PostFX (Runtime)";

            Bloom bloom = profile.AddSettings<Bloom>();
            bloom.active = true;
            bloom.intensity.Override(0.45f);
            bloom.threshold.Override(0.95f);
            bloom.softKnee.Override(0.5f);
            bloom.diffusion.Override(6f);
            bloom.color.Override(new Color(0.65f, 0.90f, 1.00f));

            Vignette vignette = profile.AddSettings<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.34f);
            vignette.smoothness.Override(0.45f);
            vignette.roundness.Override(1f);
            vignette.color.Override(Color.black);

            ColorGrading grading = profile.AddSettings<ColorGrading>();
            grading.active = true;
            grading.gradingMode.Override(GradingMode.HighDefinitionRange);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.contrast.Override(8f);
            grading.postExposure.Override(0.85f);

            ChromaticAberration chromatic = profile.AddSettings<ChromaticAberration>();
            chromatic.active = true;
            chromatic.intensity.Override(0.02f);
            chromatic.fastMode.Override(true);

            GameObject fx = new GameObject("PostFX");
            fx.transform.SetParent(parent, false);
            fx.layer = BackgroundLayer;

            PostProcessVolume volume = fx.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        static PostProcessResources GetResources(PostProcessLayer layer)
        {
            FieldInfo field = typeof(PostProcessLayer).GetField(
                "m_Resources", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(layer) as PostProcessResources : null;
        }
    }
}
