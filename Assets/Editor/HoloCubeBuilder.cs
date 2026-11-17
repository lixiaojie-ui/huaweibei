using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Daima
{
    /// <summary>
    /// 按参考图在场景里生成全息玻璃立方体：半透玻璃体 + 12 条发光棱边 + 8 个角节点 + 竖向光柱。
    /// 生成位置取当前 Scene 视图焦点，默认边长 4 米；材质保存在 Assets/Resources/HoloCube/Materials。
    /// </summary>
    public static class HoloCubeBuilder
    {
        const string MaterialFolder = "Assets/Resources/HoloCube/Materials";
        const float CubeSize = 4f;

        [MenuItem("大屏/生成全息立方体（参考图）")]
        public static void Build()
        {
            // 重新生成时先删掉旧物体，并沿用它原来的位置
            Vector3? keepPosition = null;
            GameObject existing = GameObject.Find("全息立方体");
            if (existing != null)
            {
                // 场景里已有物体时先确认，避免覆盖手动调好的效果
                if (!EditorUtility.DisplayDialog("全息立方体",
                    "场景里已经有「全息立方体」了，重新生成会覆盖它。确定继续吗？",
                    "覆盖生成", "取消"))
                    return;

                keepPosition = existing.transform.position;
                Object.DestroyImmediate(existing);
            }

            Shader glassShader = Shader.Find("Daima/HoloGlass");
            Shader emissiveShader = Shader.Find("Daima/HoloEmissive");
            if (glassShader == null || emissiveShader == null)
            {
                Debug.LogError("[全息立方体] 找不到 Daima/HoloGlass 或 Daima/HoloEmissive，请确认 Assets/Resources/HoloCube 下的 Shader 已导入。");
                return;
            }

            EnsureFolder(MaterialFolder);

            Material glass = CreateMaterial("HoloCubeGlass", glassShader, m =>
            {
                m.SetColor("_FaceColor", new Color(0.09f, 0.20f, 0.40f, 1f));
                m.SetFloat("_FaceAlpha", 0.878f);
                m.SetColor("_FresnelColor", new Color(0.75f, 0.90f, 1f, 1f));
                m.SetFloat("_FresnelPower", 6.57f);
                m.SetFloat("_FresnelIntensity", 0.05f);
                m.SetColor("_GridColor", new Color(0.5f, 0.78f, 1f, 1f));
                m.SetFloat("_GridCell", 0.25f);
                m.SetFloat("_GridWidth", 0.001f);
                m.SetFloat("_GridIntensity", 0.45f);
                m.SetFloat("_GridCenterScale", 0.12f);
                m.SetFloat("_CornerFalloff", 0.5f);
                m.SetFloat("_GridScroll", 0.08f);
                m.SetFloat("_ScanPeriod", 5f);
                m.SetFloat("_ScanSpeed", 0.4f);
                m.SetFloat("_ScanIntensity", 0.79f);
            });

            Material edge = CreateMaterial("HoloCubeEdge", emissiveShader, m =>
            {
                m.SetColor("_Color", new Color(0.72f, 0.92f, 1f, 1f));
                m.SetFloat("_Intensity", 2.8f);
                m.SetFloat("_FresnelBlend", 0f);
                m.SetFloat("_FadeBottom", 0f);
                m.SetFloat("_FadeTop", 1f);
                m.SetFloat("_PulseSpeed", 0.35f);
                m.SetFloat("_PulseAmount", 0.08f);
                m.SetFloat("_PulsePhase", 0f);
            });

            Material bar = CreateMaterial("HoloCubeBar", emissiveShader, m =>
            {
                m.SetColor("_Color", new Color(0.85f, 0.97f, 1f, 1f));
                m.SetFloat("_Intensity", 3.6f);
                m.SetFloat("_FresnelBlend", 0f);
                m.SetFloat("_PulseSpeed", 0.3f);
                m.SetFloat("_PulseAmount", 0.15f);
                m.SetFloat("_PulsePhase", 0.25f);
            });

            Material hardware = CreateMaterial("HoloCubeHardware", emissiveShader, m =>
            {
                m.SetColor("_Color", new Color(0.30f, 0.45f, 0.62f, 1f));
                m.SetFloat("_Intensity", 0.35f);
                m.SetFloat("_FresnelBlend", 0.8f);
                m.SetFloat("_FresnelPower", 1.2f);
                m.SetFloat("_PulseAmount", 0f);
            });

            Material node = CreateMaterial("HoloCubeNode", emissiveShader, m =>
            {
                m.SetColor("_Color", new Color(0.88f, 0.97f, 1f, 1f));
                m.SetFloat("_Intensity", 3.6f);
                m.SetFloat("_FresnelBlend", 1f);
                m.SetFloat("_FresnelPower", 1.8f);
                m.SetFloat("_PulseSpeed", 0.8f);
                m.SetFloat("_PulseAmount", 0.22f);
            });

            Material halo = CreateMaterial("HoloCubeNodeHalo", emissiveShader, m =>
            {
                m.SetColor("_Color", new Color(0.45f, 0.72f, 1f, 1f));
                m.SetFloat("_Intensity", 0.5f);
                m.SetFloat("_FresnelBlend", 1f);
                m.SetFloat("_FresnelPower", 1.4f);
                m.SetFloat("_PulseSpeed", 0.6f);
                m.SetFloat("_PulseAmount", 0.18f);
            });

            Material beam = CreateMaterial("HoloCubeBeam", emissiveShader, m =>
            {
                m.SetColor("_Color", new Color(0.78f, 0.94f, 1f, 1f));
                m.SetFloat("_Intensity", 1.6f);
                m.SetFloat("_FresnelBlend", 1f);
                m.SetFloat("_FresnelPower", 1.6f);
                m.SetFloat("_FadeBottom", 0f);
                m.SetFloat("_FadeTop", 0.85f);
                m.SetFloat("_FadePower", 1.8f);
                m.SetFloat("_PulseSpeed", 0.5f);
                m.SetFloat("_PulseAmount", 0.2f);
            });

            // 全息动效：世界空间里循环上升的亮带，串起棱边、亮条和角节点
            SetFlow(edge, 1.4f);
            SetFlow(bar, 1.6f);
            SetFlow(node, 0.8f);
            SetFlow(halo, 0.4f);
            SetFlow(beam, 1.0f, 0.2f);

            AssetDatabase.SaveAssets();

            GameObject root = new GameObject("全息立方体");
            Undo.RegisterCreatedObjectUndo(root, "生成全息立方体");

            Vector3 position = keepPosition ?? (SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero);
            if (keepPosition == null)
                position.y = Mathf.Max(position.y, CubeSize * 0.5f + 0.05f);
            root.transform.position = position;

            float half = CubeSize * 0.5f;
            float lineWidth = Mathf.Max(CubeSize * 0.006f, 0.008f);
            float barWidth = lineWidth * 2.2f;
            float barLength = CubeSize * 0.22f;

            // 玻璃体
            CreatePrimitive("玻璃体", PrimitiveType.Cube, root.transform, Vector3.zero, Vector3.one * CubeSize, glass);

            // 12 条细棱边 + 每条棱边靠近两端的亮条
            Transform frame = new GameObject("框架").transform;
            frame.SetParent(root.transform, false);
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 along = Axis(axis);
                Vector3 sideA = Axis((axis + 1) % 3);
                Vector3 sideB = Axis((axis + 2) % 3);

                for (int a = -1; a <= 1; a += 2)
                {
                    for (int b = -1; b <= 1; b += 2)
                    {
                        Vector3 localPos = sideA * (half * a) + sideB * (half * b);
                        Vector3 lineScale = along * (CubeSize + lineWidth) + sideA * lineWidth + sideB * lineWidth;
                        CreatePrimitive("棱边", PrimitiveType.Cube, frame, localPos, lineScale, edge);

                        for (int e = -1; e <= 1; e += 2)
                        {
                            Vector3 barPos = localPos + along * ((half - barLength * 0.5f - CubeSize * 0.015f) * e);
                            Vector3 barScale = along * barLength + sideA * barWidth + sideB * barWidth;
                            CreatePrimitive("亮条", PrimitiveType.Cube, frame, barPos, barScale, bar);
                        }
                    }
                }
            }

            // 8 个角节点：卡座 + 三条抱臂 + 朝外的发光镜片 + 光晕
            Transform nodes = new GameObject("角节点").transform;
            nodes.SetParent(root.transform, false);
            float seatSize = CubeSize * 0.075f;
            float armLength = CubeSize * 0.16f;
            float armWidth = CubeSize * 0.016f;
            float lensSize = CubeSize * 0.05f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = new Vector3(half * x, half * y, half * z);
                        Transform joint = new GameObject("角节点").transform;
                        joint.SetParent(nodes, false);
                        joint.localPosition = corner;

                        CreatePrimitive("卡座", PrimitiveType.Cube, joint, Vector3.zero, Vector3.one * seatSize, hardware);

                        // 沿三个轴向盒内伸出的抱臂
                        CreatePrimitive("抱臂 X", PrimitiveType.Cube, joint,
                            new Vector3(-x * armLength * 0.5f, 0f, 0f),
                            new Vector3(armLength, armWidth, armWidth), edge);
                        CreatePrimitive("抱臂 Y", PrimitiveType.Cube, joint,
                            new Vector3(0f, -y * armLength * 0.5f, 0f),
                            new Vector3(armWidth, armLength, armWidth), edge);
                        CreatePrimitive("抱臂 Z", PrimitiveType.Cube, joint,
                            new Vector3(0f, 0f, -z * armLength * 0.5f),
                            new Vector3(armWidth, armWidth, armLength), edge);

                        // 朝盒外的发光镜片 + 光晕
                        Vector3 lensOffset = new Vector3(x, y, z) * (CubeSize * 0.032f);
                        CreatePrimitive("镜片", PrimitiveType.Sphere, joint, lensOffset, Vector3.one * lensSize, node);
                        CreatePrimitive("光晕", PrimitiveType.Sphere, joint, lensOffset, Vector3.one * lensSize * 3f, halo);
                    }
                }
            }

            // 前下方角点上的竖向光柱
            Transform beamRoot = new GameObject("竖向光柱").transform;
            beamRoot.SetParent(root.transform, false);
            beamRoot.localPosition = new Vector3(-half, 0f, -half);
            float beamDiameter = CubeSize * 0.012f;
            float beamHeight = CubeSize * 1.06f;
            CreatePrimitive("光柱", PrimitiveType.Cylinder, beamRoot, Vector3.zero,
                new Vector3(beamDiameter, beamHeight * 0.5f, beamDiameter), beam);

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }

            Debug.Log("[全息立方体] 已生成在 " + position + "，边长 " + CubeSize + " 米。");
        }

        static Vector3 Axis(int index)
        {
            if (index == 0) return Vector3.right;
            if (index == 1) return Vector3.up;
            return Vector3.forward;
        }

        static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            return go;
        }

        static void SetFlow(Material material, float intensity, float speed = 0.12f)
        {
            material.SetFloat("_FlowHeight", CubeSize);
            material.SetFloat("_FlowSpeed", speed);
            material.SetFloat("_FlowIntensity", intensity);
        }

        static Material CreateMaterial(string name, Shader shader, System.Action<Material> configure)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            configure(material);
            EditorUtility.SetDirty(material);

            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent))
                return;

            parent = parent.Replace('\\', '/');
            EnsureFolder(parent);

            // 目录可能已经由外部创建，只是还没被 Unity 导入
            if (Directory.Exists(path))
            {
                AssetDatabase.Refresh();
                return;
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
