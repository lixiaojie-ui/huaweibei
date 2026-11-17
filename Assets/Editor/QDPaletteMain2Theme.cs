using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 把 Canvas main2 的配色收敛到 Canvas main1 / main5 的深蓝玻璃 + 青蓝主色体系：
    /// - 纯白/灰字统一为 main1 的浅蓝白文字；
    /// - 大片琥珀半透明格线改成青蓝色；
    /// - 紫色段改成青色；
    /// - 红/琥珀只保留少量状态强调色；
    /// - 网格线改成淡蓝。
    /// 只改颜色，不动字号、位置、内容、层级，不新增/删除对象。
    /// </summary>
    static class QDPaletteMain2Theme
    {
        const string Key = "QD_Main2_Main1Theme_v1";
        const string CanvasName = "Canvas main2";

        // 取自 Canvas main1 / main5 的主题色
        static readonly Color TextInk    = new Color(0.905f, 0.950f, 0.980f);   // main1 主文字
        static readonly Color TextSoft   = new Color(0.843f, 0.878f, 0.914f);   // main1 次级文字
        static readonly Color TextMuted  = new Color(0.620f, 0.740f, 0.850f);   // main1 弱化文字
        static readonly Color Cyan       = new Color(0.200f, 0.760f, 0.920f);   // main1 青
        static readonly Color Sky        = new Color(0.480f, 0.900f, 0.990f);   // main1 亮青
        static readonly Color Green      = new Color(0.260f, 0.870f, 0.620f);   // main1 绿
        static readonly Color Amber      = new Color(0.980f, 0.700f, 0.240f);   // main1 琥珀
        static readonly Color AmberLight = new Color(0.987f, 0.805f, 0.480f);   // main1 浅琥珀
        static readonly Color Red        = new Color(1.000f, 0.420f, 0.420f);   // main1 告警红
        static readonly Color AccentLine = new Color(0.300f, 0.700f, 0.950f);   // main1 描边/网格
        static readonly Color GridCool   = new Color(0.700f, 0.880f, 1.000f);   // main1 淡蓝网格

        [MenuItem("Tools/配色/main2 套用 main1 风格", false, 43)]
        static void RunMenu()
        {
            int changed = Apply();
            Debug.Log(changed > 0
                ? "[QD] Canvas main2 已套用 main1/main5 配色，改动 " + changed + " 处。"
                : "[QD] Canvas main2 没有需要调整的颜色（或未找到 Canvas main2）。");
        }

        [InitializeOnLoadMethod]
        static void Auto()
        {
            if (!hooked)
            {
                hooked = true;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }

            if (EditorPrefs.GetInt(Key, 0) >= 1) return;
            EditorApplication.delayCall += Try;
        }

        static bool hooked;

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && EditorPrefs.GetInt(Key, 0) < 1)
                EditorApplication.delayCall += Try;
        }

        static int retries;

        static void Try()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (++retries < 60) EditorApplication.delayCall += Try;
                return;
            }

            GameObject root = FindCanvas();
            if (root == null)
            {
                if (++retries < 60) EditorApplication.delayCall += Try;
                return;
            }

            retries = 0;
            int changed = Apply();
            EditorPrefs.SetInt(Key, 1);
            if (changed > 0)
                Debug.Log("[QD] Canvas main2 已自动套用 main1/main5 配色，改动 " + changed + " 处。");
        }

        static GameObject FindCanvas()
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != CanvasName) continue;
                if (t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded) return t.gameObject;
            }
            return null;
        }

        static int Apply()
        {
            GameObject root = FindCanvas();
            if (root == null) return 0;

            int changed = 0;
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("main2 配色");

            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null) continue;

                string objName = g.gameObject.name;
                Color c = g.color;

                Text text = g as Text;
                Color n = text != null ? MapText(c, objName, text.text) : MapImage(c, objName);

                if (!Same(c, n))
                {
                    Undo.RecordObject(g, "main2 配色");
                    g.color = n;
                    changed++;
                }
                EditorUtility.SetDirty(g);

                // UIRoundRect 的第二个渐变色 / 描边色
                UIRoundRect rr = g as UIRoundRect;
                if (rr != null)
                {
                    Color c2 = rr.color2;
                    Color n2 = MapImage(c2, objName);
                    if (!Same(c2, n2))
                    {
                        Undo.RecordObject(rr, "main2 配色");
                        rr.color2 = n2;
                        changed++;
                    }

                    Color bc = rr.borderColor;
                    Color nb = MapImage(bc, objName);
                    if (!Same(bc, nb))
                    {
                        Undo.RecordObject(rr, "main2 配色");
                        rr.borderColor = nb;
                        changed++;
                    }

                    rr.Refresh();
                }

                // 雷达图 / 面积图的 SerializeField 渐变色
                if (g is UIRadarFillGraphic || g is UIAreaGraphic)
                {
                    foreach (string prop in new[] { "centerColor", "edgeColor", "topColor", "bottomColor" })
                        if (RemapSerializedColor(g, prop, objName)) changed++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(root.scene);
                EditorSceneManager.SaveScene(root.scene);
            }
            return changed;
        }

        static bool RemapSerializedColor(UnityEngine.Object target, string property, string objName)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) return false;

            Color c = p.colorValue;
            Color n = MapImage(c, objName);
            if (Same(c, n)) return false;

            Undo.RecordObject(target, "main2 配色");
            p.colorValue = n;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            return true;
        }

        // ------------------------------------------------------------------ 文字

        static Color MapText(Color c, string objName, string content)
        {
            // 已经是 main1/main5 的主题字色
            if (Near(c, TextInk) ||
                Near(c, 0.900f, 0.950f, 0.990f) ||
                Near(c, 0.920f, 0.965f, 1.000f) ||
                Near(c, 0.663f, 0.788f, 0.859f) ||
                Near(c, 0.745f, 0.851f, 0.910f) ||
                Near(c, 0.847f, 0.929f, 0.973f))
                return c;

            // 纯白 -> main1 浅蓝白
            if (Near(c, 1f, 1f, 1f)) return Alpha(TextInk, c.a);

            // 灰色 -> main1 次级/弱化文字
            if (IsGray(c))
            {
                Color target = c.r >= 0.8f ? TextSoft : TextMuted;
                return Alpha(target, c.a);
            }

            // 青
            if (Near(c, 0.235f, 0.851f, 1f) || Near(c, 0f, 0.694f, 1f))
                return Alpha(Cyan, c.a);

            // 亮青
            if (Near(c, 0.306f, 0.835f, 0.976f)) return Alpha(Sky, c.a);

            // 琥珀
            if (Near(c, 1f, 0.706f, 0.329f)) return Alpha(Amber, c.a);
            if (Near(c, 1f, 0.769f, 0.420f)) return Alpha(AmberLight, c.a);
            if (Near(c, 1f, 0.561f, 0f)) return Alpha(Amber, c.a);

            // 珊瑚/红：状态标签“中”保留琥珀，其余归一到 main1 告警红
            if (Near(c, 1f, 0.545f, 0.584f))
            {
                bool middle = objName == "pillTx" &&
                              !string.IsNullOrEmpty(content) && content.Contains("中");
                return Alpha(middle ? Amber : Red, c.a);
            }

            if (Near(c, 1f, 0.392f, 0.439f) || Near(c, 1f, 0f, 0.086f))
                return Alpha(Red, c.a);

            // 绿
            if (Near(c, 0.361f, 0.910f, 0.678f) || Near(c, 0.259f, 0.871f, 0.620f))
                return Alpha(Green, c.a);

            return c;
        }

        // ------------------------------------------------------------------ 图形

        static Color MapImage(Color c, string objName)
        {
            // 面板/卡片玻璃底：保持 main1 原样
            if (Near(c, 0.055f, 0.180f, 0.298f) || Near(c, 0.027f, 0.078f, 0.125f))
                return c;

            // 琥珀：大幅半透明格线改青蓝；淡底改青色；实心强调保留暖橙
            if (Near(c, 1f, 0.706f, 0.329f) || Near(c, 0.980f, 0.700f, 0.240f))
            {
                if (objName.StartsWith("seg", StringComparison.Ordinal))
                    return new Color(Sky.r, Sky.g, Sky.b, Mathf.Min(c.a, 0.45f));

                if (c.a <= 0.12f)
                    return new Color(Cyan.r, Cyan.g, Cyan.b, c.a);

                return new Color(Amber.r, Amber.g, Amber.b, c.a);
            }

            if (Near(c, 1f, 0.769f, 0.420f))
                return new Color(AmberLight.r, AmberLight.g, AmberLight.b, c.a);

            // 紫色 -> 青色
            if (Near(c, 0.663f, 0.545f, 0.941f) || Near(c, 0.706f, 0.549f, 1f))
                return new Color(Cyan.r, Cyan.g, Cyan.b, c.a);

            // 红/珊瑚 -> main1 告警红
            if (Near(c, 1f, 0.545f, 0.584f) || Near(c, 1f, 0.392f, 0.439f) ||
                Near(c, 1f, 0f, 0.086f) || Near(c, 0.750f, 0.120f, 0.160f))
                return new Color(Red.r, Red.g, Red.b, c.a);

            // 青/亮青/绿
            if (Near(c, 0.235f, 0.851f, 1f))
                return new Color(Cyan.r, Cyan.g, Cyan.b, c.a);
            if (Near(c, 0.306f, 0.835f, 0.976f))
                return new Color(Sky.r, Sky.g, Sky.b, c.a);
            if (Near(c, 0.259f, 0.871f, 0.620f) || Near(c, 0.361f, 0.910f, 0.678f))
                return new Color(Green.r, Green.g, Green.b, c.a);

            // 灰色图例块 -> main1 弱化蓝灰
            if (Near(c, 0.588f, 0.686f, 0.745f))
                return new Color(TextMuted.r, TextMuted.g, TextMuted.b, c.a);

            // 分隔线/坐标轴/描边 -> main1 青色描边
            if (Near(c, 0.298f, 0.627f, 0.804f) || Near(c, 0.251f, 0.667f, 0.863f) ||
                Near(c, 0.353f, 0.647f, 0.804f) || Near(c, 0.243f, 0.608f, 0.769f) ||
                Near(c, 0.141f, 0.439f, 0.561f))
                return new Color(AccentLine.r, AccentLine.g, AccentLine.b, c.a);

            // 白色网格/分隔/行底 -> main1 淡蓝
            if (IsGray(c) && c.a > 0.001f && c.a <= 0.25f)
                return new Color(GridCool.r, GridCool.g, GridCool.b, c.a);

            return c;
        }

        // ------------------------------------------------------------------ 工具

        static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.002f &&
                   Mathf.Abs(a.g - b.g) < 0.002f &&
                   Mathf.Abs(a.b - b.b) < 0.002f &&
                   Mathf.Abs(a.a - b.a) < 0.002f;
        }

        static bool Near(Color c, float r, float g, float b, float tol = 0.035f)
        {
            return Mathf.Abs(c.r - r) < tol &&
                   Mathf.Abs(c.g - g) < tol &&
                   Mathf.Abs(c.b - b) < tol;
        }

        static bool Near(Color c, Color target)
        {
            return Near(c, target.r, target.g, target.b);
        }

        static bool IsGray(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max - min < 0.035f;
        }

        static Color Alpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
