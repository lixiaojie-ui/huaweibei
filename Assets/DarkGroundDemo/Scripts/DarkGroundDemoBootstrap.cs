using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace Daima
{
    /// <summary>
    /// 数字孪生暗场地面 Demo：运行时搭出 Environment / Particles / Global Volume，
    /// 参数集中在 Inspector，README 里有逐项说明。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DarkGroundDemoBootstrap : MonoBehaviour
    {
        [Header("网格")]
        public Color gridColor = new Color(0.098f, 0.784f, 1f, 1f);
        public float cellSize = 0.6f;
        public int majorEvery = 5;
        public float fadeStart = 60f;
        public float fadeEnd = 100f;
        public float gridIntensity = 1f;

        [Header("扫描亮带")]
        public float scanPeriod = 15f;
        public float scanWidth = 8f;
        public float scanIntensity = 1.6f;

        [Header("粒子")]
        public int particleCount = 300;
        public int particleMax = 400;

        [Header("平台")]
        public Vector3 platformPosition = new Vector3(-15f, 0f, 24f);
        public Vector3 platformSize = new Vector3(12f, 1.6f, 7f);

        [Header("相机")]
        public float cameraPitch = 15f;

        Material gridMaterial;
        Material particleMaterial;

        void Awake()
        {
            SetupRenderSettings();
            SetupLights();
            SetupEnvironment();
            SetupParticles();
            SetupCamera();
            SetupPostFx();
        }

        void SetupRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.030f, 0.060f, 0.100f);
            RenderSettings.ambientEquatorColor = new Color(0.012f, 0.025f, 0.045f);
            RenderSettings.ambientGroundColor = new Color(0.004f, 0.008f, 0.016f);
            RenderSettings.ambientIntensity = 0.6f;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            RenderSettings.reflectionIntensity = 0.4f;
        }

        void SetupLights()
        {
            GameObject sunGo = new GameObject("Directional Light");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.55f, 0.72f, 1f);
            sun.intensity = 0.5f;
            sun.shadows = LightShadows.None;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light topLight = CreatePointLight("Platform Light", new Color(0.20f, 0.75f, 1f), 3f, 20f);
            topLight.transform.position = platformPosition + new Vector3(0f, 4.2f, -2f);

            Light underLight = CreatePointLight("Platform Under Glow", new Color(0.45f, 0.30f, 1f), 2f, 11f);
            underLight.transform.position = platformPosition + new Vector3(0f, 0.5f, 0f);
        }

        static Light CreatePointLight(string name, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        void SetupEnvironment()
        {
            GameObject environment = new GameObject("Environment");

            CreateGrid(environment.transform);
            CreatePlatform(environment.transform);
            CreateScanMarkers(environment.transform);
            CreateLightBeams(environment.transform);
        }

        void CreateGrid(Transform parent)
        {
            gridMaterial = CreateMaterial("DarkGround/HoloGrid", "HoloGrid (Runtime)");
            if (gridMaterial == null) return;

            gridMaterial.SetColor("_Color", gridColor);
            gridMaterial.SetFloat("_CellSize", cellSize);
            gridMaterial.SetFloat("_MajorEvery", majorEvery);
            gridMaterial.SetFloat("_MinorWidth", 0.012f);
            gridMaterial.SetFloat("_MajorWidth", 0.03f);
            gridMaterial.SetFloat("_MinorIntensity", 0.55f);
            gridMaterial.SetFloat("_MajorIntensity", 1.35f);
            gridMaterial.SetFloat("_FadeStart", fadeStart);
            gridMaterial.SetFloat("_FadeEnd", fadeEnd);
            gridMaterial.SetFloat("_ScanPeriod", scanPeriod);
            gridMaterial.SetFloat("_ScanWidth", scanWidth);
            gridMaterial.SetFloat("_ScanIntensity", scanIntensity);
            gridMaterial.SetFloat("_ScanMinZ", -40f);
            gridMaterial.SetFloat("_ScanMaxZ", 60f);
            gridMaterial.SetFloat("_Intensity", gridIntensity);

            // 网格本体比 100×100 大一圈，避免渐隐边界露出 Plane 的硬边
            GameObject grid = CreatePrimitive("GridPlane", PrimitiveType.Plane, parent,
                Vector3.zero, new Vector3(20f, 1f, 20f), gridMaterial);
            grid.transform.localRotation = Quaternion.identity;
        }

        void CreatePlatform(Transform parent)
        {
            GameObject platform = new GameObject("Platform");
            platform.transform.SetParent(parent, false);
            platform.transform.position = platformPosition;

            Material bodyMaterial = new Material(Shader.Find("Standard")) { name = "Platform Metal" };
            bodyMaterial.SetColor("_Color", new Color(0.045f, 0.060f, 0.080f));
            bodyMaterial.SetFloat("_Metallic", 0.30f);
            bodyMaterial.SetFloat("_Glossiness", 0.60f);

            Material topMaterial = new Material(Shader.Find("Standard")) { name = "Platform Top" };
            topMaterial.SetColor("_Color", new Color(0.070f, 0.090f, 0.120f));
            topMaterial.SetFloat("_Metallic", 0.40f);
            topMaterial.SetFloat("_Glossiness", 0.60f);

            Vector3 size = platformSize;
            CreatePrimitive("Base", PrimitiveType.Cube, platform.transform,
                new Vector3(0f, size.y * 0.5f, 0f), size, bodyMaterial);
            CreatePrimitive("Top Plate", PrimitiveType.Cube, platform.transform,
                new Vector3(0f, size.y + 0.02f, 0f), new Vector3(size.x - 0.2f, 0.04f, size.z - 0.2f), topMaterial);

            Material stripMaterial = CreateEmissiveMaterial("Platform Strip", gridColor, 3f);
            CreatePrimitive("Light Strip", PrimitiveType.Cube, platform.transform,
                new Vector3(0f, size.y + 0.055f, -size.z * 0.5f + 0.02f),
                new Vector3(size.x, 0.03f, 0.03f), stripMaterial);

            // 台体下方的蓝紫柔光：复用粒子的软圆点材质，铺一块大面片
            Material glowMaterial = CreateMaterial("DarkGround/ParticleAdditive", "Platform Glow (Runtime)");
            if (glowMaterial != null)
            {
                glowMaterial.SetColor("_TintColor", new Color(0.42f, 0.30f, 1f, 1f));
                glowMaterial.SetFloat("_Intensity", 0.8f);
                glowMaterial.SetFloat("_UseVertexColor", 0f);
                GameObject glow = CreatePrimitive("Under Glow", PrimitiveType.Quad, platform.transform,
                    new Vector3(0f, 0.02f, 0f), new Vector3(20f, 12f, 1f), glowMaterial);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                // 台体正面再铺一块，低机位时也能看到蓝紫柔光
                CreatePrimitive("Front Glow", PrimitiveType.Quad, platform.transform,
                    new Vector3(0f, size.y * 0.5f, -size.z * 0.5f - 0.03f),
                    new Vector3(size.x + 0.8f, size.y + 0.4f, 1f), glowMaterial);
            }
        }

        void CreateScanMarkers(Transform parent)
        {
            GameObject markers = new GameObject("ScanMarkers");
            markers.transform.SetParent(parent, false);

            Vector3[] positions =
            {
                new Vector3(2f, 0f, 0f),
                new Vector3(8f, 0f, 7f),
                new Vector3(-4f, 0f, 12f)
            };
            float[] sizes = { 2.0f, 2.4f, 1.6f };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject marker = new GameObject("ScanMarker " + (i + 1));
                marker.transform.SetParent(markers.transform, false);
                marker.transform.localPosition = positions[i];

                Material material = CreateEmissiveMaterial("ScanMarker " + (i + 1), gridColor, 1.4f);
                float half = sizes[i] * 0.5f;

                CreatePrimitive("North", PrimitiveType.Cube, marker.transform,
                    new Vector3(0f, 0.02f, half), new Vector3(sizes[i], 0.02f, 0.05f), material);
                CreatePrimitive("South", PrimitiveType.Cube, marker.transform,
                    new Vector3(0f, 0.02f, -half), new Vector3(sizes[i], 0.02f, 0.05f), material);
                CreatePrimitive("East", PrimitiveType.Cube, marker.transform,
                    new Vector3(half, 0.02f, 0f), new Vector3(0.05f, 0.02f, sizes[i]), material);
                CreatePrimitive("West", PrimitiveType.Cube, marker.transform,
                    new Vector3(-half, 0.02f, 0f), new Vector3(0.05f, 0.02f, sizes[i]), material);
                CreatePrimitive("Center Dot", PrimitiveType.Cube, marker.transform,
                    new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.02f, 0.12f), material);

                DemoMarkerPulse pulse = marker.AddComponent<DemoMarkerPulse>();
                pulse.material = material;
                pulse.color = gridColor;
                pulse.phase = i * 0.33f;
            }
        }

        void CreateLightBeams(Transform parent)
        {
            GameObject beams = new GameObject("LightBeams");
            beams.transform.SetParent(parent, false);

            Material beamMaterial = CreateEmissiveMaterial("Light Beam", gridColor, 2.2f);
            Vector3[] positions = { new Vector3(-7f, 0f, 6f), new Vector3(4f, 0f, 10f), new Vector3(11f, 0f, 16f) };
            float[] heights = { 4.5f, 3.8f, 5.2f };

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 position = positions[i];
                CreatePrimitive("LightBeam " + (i + 1), PrimitiveType.Cube, beams.transform,
                    new Vector3(position.x, heights[i] * 0.5f, position.z),
                    new Vector3(0.07f, heights[i], 0.07f), beamMaterial);
                CreatePrimitive("Beam Base " + (i + 1), PrimitiveType.Cube, beams.transform,
                    new Vector3(position.x, 0.02f, position.z),
                    new Vector3(0.25f, 0.02f, 0.25f), beamMaterial);
            }
        }

        void SetupParticles()
        {
            particleMaterial = CreateMaterial("DarkGround/ParticleAdditive", "Particle (Runtime)");
            if (particleMaterial == null) return;

            particleMaterial.SetColor("_TintColor", new Color(0.80f, 0.95f, 1f, 1f));
            particleMaterial.SetFloat("_Intensity", 1.2f);

            GameObject particlesGo = new GameObject("Particles");
            particlesGo.transform.position = new Vector3(0f, 5.5f, 12f);

            ParticleSystem particles = particlesGo.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.prewarm = true;
            main.duration = 30f;
            main.maxParticles = particleMax;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.561f, 0.902f, 1f, 0.2f),
                new Color(0.561f, 0.902f, 1f, 0.5f));
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = particleCount / 11.5f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50f, 50f, 10f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.randomDirectionAmount = 0.2f;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.35f, 0.8f), new Keyframe(1f, 0.35f)));

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.561f, 0.902f, 1f), 0f),
                    new GradientColorKey(new Color(0.561f, 0.902f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.65f, 0.5f),
                    new GradientAlphaKey(1f, 0.88f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.03f;
            noise.frequency = 0.2f;
            noise.scrollSpeed = 0.05f;
            noise.damping = true;
            noise.octaveCount = 2;

            ParticleSystemRenderer renderer = particlesGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = particleMaterial;
            renderer.sortMode = ParticleSystemSortMode.None;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            particles.Play();
        }

        void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraGo = new GameObject("CameraRig");
                cameraGo.tag = "MainCamera";
                cameraGo.transform.position = new Vector3(0f, 2.4f, -16f);
                cameraGo.transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);

                camera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
                cameraGo.AddComponent<DemoCameraRig>();
            }

            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 300f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.004f, 0.008f, 0.016f, 1f);
            camera.allowHDR = true;

            if (camera.GetComponent<DemoCameraRig>() == null)
                camera.gameObject.AddComponent<DemoCameraRig>();
        }

        void SetupPostFx()
        {
            PostProcessResources resources = Resources.Load<PostProcessResources>("DarkGroundPostProcessResources");
            if (resources == null)
            {
                Debug.LogWarning("[DarkGroundDemo] 缺少 PostProcessResources，后期未启用。");
                return;
            }

            Camera camera = Camera.main;
            if (camera == null) return;

            PostProcessLayer layer = camera.GetComponent<PostProcessLayer>();
            if (layer == null) layer = camera.gameObject.AddComponent<PostProcessLayer>();

            layer.Init(resources);
            layer.volumeTrigger = camera.transform;
            layer.volumeLayer = 1 << 0;

            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            profile.name = "DarkGround Demo PostFX";

            Bloom bloom = profile.AddSettings<Bloom>();
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(0.9f);
            bloom.softKnee.Override(0.5f);
            bloom.diffusion.Override(7f);

            Vignette vignette = profile.AddSettings<Vignette>();
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.45f);
            vignette.roundness.Override(1f);

            ColorGrading grading = profile.AddSettings<ColorGrading>();
            grading.gradingMode.Override(GradingMode.HighDefinitionRange);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.contrast.Override(10f);

            ChromaticAberration chromatic = profile.AddSettings<ChromaticAberration>();
            chromatic.intensity.Override(0.03f);
            chromatic.fastMode.Override(true);

            GameObject volumeGo = new GameObject("Global Volume");
            PostProcessVolume volume = volumeGo.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        static Material CreateMaterial(string shaderName, string materialName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("[DarkGroundDemo] 找不到 Shader: " + shaderName);
                return null;
            }
            return new Material(shader) { name = materialName };
        }

        static Material CreateEmissiveMaterial(string name, Color color, float intensity)
        {
            Material material = new Material(Shader.Find("Standard")) { name = name };
            material.SetColor("_Color", new Color(0.02f, 0.035f, 0.05f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.5f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return material;
        }

        static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return go;
        }
    }
}
