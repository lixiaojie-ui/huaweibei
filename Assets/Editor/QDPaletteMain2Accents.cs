using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// Canvas main2 第二遍配色：在上一版“深蓝 + 青蓝”的底子上补充柔和点缀色，
    /// 让画面不再只有一种青色，但保持低饱和、和现有色调融洽。
    ///
    /// - 顶部四个设备状态：绿 / 天蓝 / 珊瑚红 / 琥珀
    /// - 综合效率五条进度条：绿 → 青绿 → 青 → 天蓝 → 琥珀（渐变过渡）
    /// - 负载柱：当前负载深蓝、峰值负载天蓝
    /// - 健康页四个 KPI 卡：绿 / 珊瑚红 / 琥珀 / 蓝（圆点与涨幅标签同步）
    /// - 风险表格：高风险珊瑚红、中风险琥珀
    /// - 维修效率四段堆叠：蓝 / 青绿 / 琥珀 / 蓝灰（图例同步）
    /// - 大幅半透明青色格线降低透明度，避免整屏发青
    ///
    /// 只改颜色，不动字号、位置、内容、层级，不新增/删除对象。
    /// </summary>
    static class QDPaletteMain2Accents
    {
        const string Key = "QD_Main2_Accents_v1";
        const string CanvasName = "Canvas main2";

        // 低饱和点缀色（与 main1/main5 的深蓝底协调）
        static readonly Color BlueSoft      = new Color(0.298f, 0.561f, 0.796f);   // #4C8FCB
        static readonly Color TealSoft      = new Color(0.333f, 0.749f, 0.686f);   // #55BFAF
        static readonly Color GreenSoft     = new Color(0.310f, 0.749f, 0.561f);   // #4FBF8F
        static readonly Color CyanSoft      = new Color(0.369f, 0.784f, 0.910f);   // #5EC8E8
        static readonly Color SkySoft       = new Color(0.541f, 0.831f, 0.969f);   // #8AD4F7
        static readonly Color AmberSoft     = new Color(0.835f, 0.640f, 0.310f);   // #D5A34F
        static readonly Color AmberLightSoft= new Color(0.898f, 0.769f, 0.549f);   // #E5C48C
        static readonly Color CoralSoft     = new Color(0.850f, 0.480f, 0.460f);   // #D97A75
        static readonly Color SlateSoft     = new Color(0.494f, 0.620f, 0.780f);   // #7E9EC7

        [MenuItem("Tools/配色/main2 增加柔和点缀色", false, 44)]
        static void RunMenu()
        {
            int changed = Apply();
            Debug.Log(changed > 0
                ? "[QD] Canvas main2 已补充柔和点缀色，改动 " + changed + " 处。"
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

            GameObject canvas = FindCanvas(CanvasName);
            if (canvas == null)
            {
                if (++retries < 60) EditorApplication.delayCall += Try;
                return;
            }

            retries = 0;
            int changed = Apply();
            EditorPrefs.SetInt(Key, 1);
            if (changed > 0)
                Debug.Log("[QD] Canvas main2 已自动补充柔和点缀色，改动 " + changed + " 处。");
        }

        static int Apply()
        {
            GameObject canvas = FindCanvas(CanvasName);
            if (canvas == null) return 0;

            int changed = 0;
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("main2 点缀色");

            Transform root = canvas.transform;
            Transform left2 = FindDeep(root, "Canvas left2");
            Transform right2 = FindDeep(root, "Canvas right2");
            Transform leftTop = left2 != null ? FindDeep(left2, "Image1") : null;
            Transform leftBottom = left2 != null ? FindDeep(left2, "Image2") : null;
            Transform rightTop = right2 != null ? FindDeep(right2, "Image1") : null;
            Transform repair = right2 != null ? FindDeep(right2, "Image-设备维修效率") : null;

            // 先把上一版偏艳的红/琥珀/绿统一降饱和
            changed += SoftenExisting(root);

            if (leftTop != null) changed += ColorStatusStrip(leftTop);
            if (leftTop != null) changed += ColorMetricFills(leftTop);
            if (leftTop != null) changed += ColorRadar(leftTop);
            if (leftBottom != null) changed += ColorLoadBars(leftBottom);
            if (rightTop != null) changed += ColorHealthPanel(rightTop);
            if (repair != null) changed += ColorRepair(repair);

            // 大片天空蓝格线降透明度，避免整屏发青
            changed += CalmSegs(root);

            Undo.CollapseUndoOperations(undoGroup);

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(canvas.scene);
                EditorSceneManager.SaveScene(canvas.scene);
            }
            return changed;
        }

        // ------------------------------------------------------------------ 顶部设备状态

        static int ColorStatusStrip(Transform panel)
        {
            Color[] colors = { GreenSoft, SkySoft, CoralSoft, AmberSoft };
            int changed = 0;

            List<Image> bars = Images(panel, "eqBar");
            SortByX(bars);
            for (int i = 0; i < bars.Count && i < colors.Length; i++)
                if (SetColor(bars[i], colors[i])) changed++;

            List<Image> bgs = Images(panel, "eqBg");
            SortByX(bgs);
            for (int i = 0; i < bgs.Count && i < colors.Length; i++)
            {
                Color c = colors[i];
                c.a = 0.10f;
                if (SetColor(bgs[i], c)) changed++;
            }

            List<Image> dots = Images(panel, "dot");
            SortByX(dots);
            for (int i = 0; i < dots.Count && i < colors.Length; i++)
                if (SetColor(dots[i], colors[i])) changed++;

            return changed;
        }

        // ------------------------------------------------------------------ 综合效率进度条

        static int ColorMetricFills(Transform panel)
        {
            Color[] colors = { GreenSoft, TealSoft, CyanSoft, SkySoft, AmberSoft };
            List<Image> fills = Images(panel, "mFill");
            // 顶部优先：anchoredPosition.y 越大越靠上
            fills.Sort((a, b) => b.rectTransform.anchoredPosition.y.CompareTo(a.rectTransform.anchoredPosition.y));

            int changed = 0;
            for (int i = 0; i < fills.Count && i < colors.Length; i++)
                if (SetColor(fills[i], colors[i])) changed++;
            return changed;
        }

        static int ColorRadar(Transform panel)
        {
            int changed = 0;
            foreach (UIRadarFillGraphic radar in panel.GetComponentsInChildren<UIRadarFillGraphic>(true))
            {
                if (SetSerializedColor(radar, "centerColor", Alpha(CyanSoft, 0.30f))) changed++;
                if (SetSerializedColor(radar, "edgeColor", Alpha(TealSoft, 0.05f))) changed++;
            }
            return changed;
        }

        // ------------------------------------------------------------------ 设备负载柱

        static int ColorLoadBars(Transform panel)
        {
            int changed = 0;

            // 宽柱 = 当前负载，细柱 = 峰值负载
            foreach (Image bar in Images(panel, "Bar"))
            {
                float width = bar.rectTransform.sizeDelta.x;
                if (SetColor(bar, width >= 25f ? BlueSoft : SkySoft)) changed++;
            }

            Image lgA = FindDirectImage(panel, "lgA");
            if (lgA != null && SetColor(lgA, BlueSoft)) changed++;

            Image lgB = FindDirectImage(panel, "lgB");
            if (lgB != null && SetColor(lgB, SkySoft)) changed++;

            return changed;
        }

        // ------------------------------------------------------------------ 设备健康 KPI / 风险表

        static int ColorHealthPanel(Transform panel)
        {
            int changed = 0;

            for (int i = 0; i < panel.childCount; i++)
            {
                Transform child = panel.GetChild(i);
                if (child.name == "tileK")
                {
                    Text label = child.GetComponent<Text>();
                    if (label == null) continue;

                    Color c;
                    if (label.text.Contains("综合健康")) c = GreenSoft;
                    else if (label.text.Contains("高风险")) c = CoralSoft;
                    else if (label.text.Contains("预测工单")) c = AmberSoft;
                    else if (label.text.Contains("备件齐套")) c = BlueSoft;
                    else continue;

                    Transform dot = FindSibling(panel, i, "dot", -1);
                    if (dot != null)
                    {
                        Image image = dot.GetComponent<Image>();
                        if (image != null && SetColor(image, c)) changed++;
                    }

                    Text tag = FindSiblingText(panel, i, "tileTag", 1);
                    if (tag != null && SetColor(tag, c)) changed++;
                }
                else if (child.name == "pillTx")
                {
                    Text label = child.GetComponent<Text>();
                    if (label == null) continue;

                    string value = (label.text ?? "").Trim();
                    Color c;
                    if (value == "高") c = CoralSoft;
                    else if (value == "中") c = AmberSoft;
                    else continue;

                    Transform dot = FindSibling(panel, i, "dot", -1);
                    if (dot != null)
                    {
                        Image image = dot.GetComponent<Image>();
                        if (image != null && SetColor(image, c)) changed++;
                    }

                    if (SetColor(label, c)) changed++;
                }
            }

            return changed;
        }

        // ------------------------------------------------------------------ 维修效率堆叠

        static int ColorRepair(Transform repair)
        {
            Color[] palette = { BlueSoft, TealSoft, AmberSoft, SlateSoft };
            int changed = 0;

            // 20 段共用一个父物体，按行（y）分组、行内按 x 对应 响应/诊断/维修/验收
            List<Image> segs = Images(repair, "repSeg");
            segs.Sort((a, b) =>
            {
                float dy = b.rectTransform.anchoredPosition.y - a.rectTransform.anchoredPosition.y;
                if (Mathf.Abs(dy) > 1f) return dy > 0f ? 1 : -1;
                return a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x);
            });

            float lastY = float.NaN;
            int index = 0;
            foreach (Image seg in segs)
            {
                float y = seg.rectTransform.anchoredPosition.y;
                if (float.IsNaN(lastY) || Mathf.Abs(y - lastY) > 1f)
                {
                    index = 0;
                    lastY = y;
                }

                Color c = palette[Mathf.Min(index, palette.Length - 1)];
                if (SetColor(seg, c)) changed++;
                index++;
            }

            List<Image> dots = Images(repair, "dot");
            SortByX(dots);
            for (int i = 0; i < dots.Count && i < palette.Length; i++)
                if (SetColor(dots[i], palette[i])) changed++;

            List<Text> labels = Texts(repair, "lg");
            SortByX(labels);
            for (int i = 0; i < labels.Count && i < palette.Length; i++)
                if (SetColor(labels[i], palette[i])) changed++;

            return changed;
        }

        // ------------------------------------------------------------------ 降饱和 / 降亮度

        static int SoftenExisting(Transform root)
        {
            int changed = 0;

            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null) continue;

                Color c = g.color;
                Color target;

                if (Near(c, 0.980f, 0.700f, 0.240f)) target = AmberSoft;
                else if (Near(c, 0.988f, 0.804f, 0.478f)) target = AmberLightSoft;
                else if (Near(c, 1.000f, 0.420f, 0.420f)) target = CoralSoft;
                else if (Near(c, 1.000f, 0.560f, 0.480f)) target = CoralSoft;
                else if (Near(c, 0.260f, 0.870f, 0.620f)) target = GreenSoft;
                else continue;

                target.a = c.a;
                if (SetColor(g, target)) changed++;
            }

            return changed;
        }

        static int CalmSegs(Transform root)
        {
            int changed = 0;

            foreach (Image image in Images(root, "seg"))
            {
                Color c = image.color;
                if (Near(c, 0.480f, 0.900f, 0.990f) && c.a > 0.3f)
                {
                    c.a = 0.30f;
                    if (SetColor(image, c)) changed++;
                }
            }

            return changed;
        }

        // ------------------------------------------------------------------ 工具

        static bool SetColor(Graphic g, Color c)
        {
            if (g == null) return false;
            if (Same(g.color, c)) return false;

            Undo.RecordObject(g, "main2 点缀色");
            g.color = c;
            EditorUtility.SetDirty(g);
            return true;
        }

        static bool SetSerializedColor(UnityEngine.Object target, string property, Color color)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) return false;

            Color current = p.colorValue;
            if (Same(current, color)) return false;

            Undo.RecordObject(target, "main2 点缀色");
            p.colorValue = color;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            return true;
        }

        static List<Image> Images(Transform root, string name)
        {
            var result = new List<Image>();
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image != null && image.gameObject.name == name) result.Add(image);
            return result;
        }

        static List<Text> Texts(Transform root, string name)
        {
            var result = new List<Text>();
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
                if (text != null && text.gameObject.name == name) result.Add(text);
            return result;
        }

        static void SortByX(List<Image> list)
        {
            list.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));
        }

        static void SortByX(List<Text> list)
        {
            list.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));
        }

        static Transform FindSibling(Transform parent, int index, string name, int direction)
        {
            for (int i = index + direction; i >= 0 && i < parent.childCount; i += direction)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }

        static Text FindSiblingText(Transform parent, int index, string name, int direction)
        {
            Transform t = FindSibling(parent, index, name, direction);
            return t != null ? t.GetComponent<Text>() : null;
        }

        static Image FindDirectImage(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) return child.GetComponent<Image>();
            }
            return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static GameObject FindCanvas(string name)
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != name) continue;
                if (t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded) return t.gameObject;
            }
            return null;
        }

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

        static Color Alpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
