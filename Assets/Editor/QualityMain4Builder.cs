using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Daima;

/// <summary>
/// 质量检测 Main4：在 Canvas left4 / right4 的 4 个框里生成纯 uGUI 图表。
/// 内容取自车间设备说明（原料前处理 / 一混发酵 / 二混混合 / 烘干包装 / 动力环保）。
/// 全部由 Image / Text / UIRingGraphic / UILineGraphic 组成，可在 Hierarchy 中直接选中和移动。
/// </summary>
public static class QualityMain4Builder
{
    const string BuiltKey = "QualityMain4_Built_v5";

    static readonly Color Ink = new Color(0.93f, 0.97f, 1f, 1f);
    static readonly Color Muted = new Color(0.62f, 0.74f, 0.85f, 1f);
    static readonly Color Line = new Color(0.47f, 0.75f, 0.88f, 0.34f);
    static readonly Color LineSoft = new Color(0.47f, 0.75f, 0.88f, 0.16f);
    static readonly Color Cyan = new Color(0.18f, 0.74f, 0.92f, 1f);
    static readonly Color Blue = new Color(0.29f, 0.51f, 0.91f, 1f);
    static readonly Color Ok = new Color(0.32f, 0.84f, 0.73f, 1f);
    static readonly Color Warn = new Color(0.96f, 0.75f, 0.39f, 1f);
    static readonly Color Red = new Color(1f, 0.49f, 0.52f, 1f);
    static readonly Color Track = new Color(0.75f, 0.88f, 0.95f, 0.16f);

    static Font _font;
    static Font F
    {
        get
        {
            if (_font == null)
            {
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                }
            }
            return _font;
        }
    }

    [InitializeOnLoadMethod]
    static void AutoBuildOnce()
    {
        if (EditorPrefs.GetBool(BuiltKey, false)) return;
        EditorApplication.delayCall += TryAutoBuild;
    }

    static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoBuild;
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) { EditorApplication.delayCall += TryAutoBuild; return; }

        if (Build())
        {
            EditorPrefs.SetBool(BuiltKey, true);
            Debug.Log("[Main4] 已按车间设备数据重建图表，按 Ctrl+S 保存场景。");
        }
    }

    [MenuItem("Tools/质量检测/Main4 生成图表")]
    public static void BuildMenu()
    {
        if (Build())
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Main4] 图表已重建，请保存场景。");
        }
        else
        {
            Debug.LogWarning("[Main4] 未找到 Canvas left4 / Canvas right4，或缺少 Image1-Image4。");
        }
    }

    [MenuItem("Tools/质量检测/Main4 清除图表")]
    public static void ClearMenu()
    {
        ClearAll("Canvas left4");
        ClearAll("Canvas right4");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // ------------------------------------------------------------------ 构建入口

    static bool Build()
    {
        RectTransform left = FindCanvas("Canvas left4");
        RectTransform right = FindCanvas("Canvas right4");
        if (left == null || right == null) return false;

        RectTransform[] lf = Frames(left);
        RectTransform[] rf = Frames(right);
        if (lf == null || rf == null) return false;

        Retitle(left, new string[] { "检测总览", "产线总览" }, "产线总览");
        Retitle(left, new string[] { "环节合格率", "工段设备分布" }, "工段设备分布");
        Retitle(left, new string[] { "异常类型构成", "设备状态构成" }, "设备状态构成");
        Retitle(left, new string[] { "异常趋势", "产量趋势" }, "产量趋势");
        Retitle(right, new string[] { "实时异常监控", "实时运行监控" }, "实时运行监控");
        Retitle(right, new string[] { "外壳缺陷部位", "工段设备状态" }, "工段设备状态");
        Retitle(right, new string[] { "外壳检测合格率", "设备综合效率" }, "设备综合效率");
        Retitle(right, new string[] { "尺寸偏差记录", "设备状态清单" }, "设备状态清单");

        BuildOverview(lf[0]);
        BuildSections(lf[1]);
        BuildStatus(lf[2]);
        BuildOutputTrend(lf[3]);

        BuildMonitor(rf[0]);
        BuildSectionStatus(rf[1]);
        BuildOee(rf[2]);
        BuildDeviceList(rf[3]);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return true;
    }

    static RectTransform FindCanvas(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.transform as RectTransform : null;
    }

    static RectTransform[] Frames(RectTransform canvas)
    {
        RectTransform[] result = new RectTransform[4];
        for (int i = 0; i < 4; i++)
        {
            Transform t = canvas.Find("Image" + (i + 1));
            if (t == null) return null;
            result[i] = t as RectTransform;
        }
        return result;
    }

    static void Retitle(RectTransform canvas, string[] oldNames, string newName)
    {
        Transform t = null;
        for (int i = 0; i < oldNames.Length && t == null; i++) t = canvas.Find(oldNames[i]);
        if (t == null) return;
        t.name = newName;
        var text = t.GetComponent<Text>();
        if (text != null) text.text = newName;
    }

    static void ClearAll(string canvasName)
    {
        RectTransform canvas = FindCanvas(canvasName);
        if (canvas == null) return;
        RectTransform[] frames = Frames(canvas);
        if (frames == null) return;
        foreach (RectTransform frame in frames)
        {
            Transform chart = frame.Find("__Chart");
            if (chart != null) Object.DestroyImmediate(chart.gameObject);
        }
    }

    static RectTransform Begin(RectTransform frame)
    {
        Transform old = frame.Find("__Chart");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        return Rect(frame, "__Chart", 0f, 0f, frame.sizeDelta.x, frame.sizeDelta.y);
    }

    // ------------------------------------------------------------------ 基础图元

    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Image Img(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        RectTransform rt = Rect(parent, name, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text Txt(Transform parent, string name, string content, int size, Color color,
                    TextAnchor anchor, float x, float y, float w, float h, bool bold = false)
    {
        RectTransform rt = Rect(parent, name, x, y, w, h);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = F;
        t.fontSize = size;
        t.color = color;
        t.text = content;
        t.alignment = anchor;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Divider(Transform parent, float y, float w, Color color)
    {
        Img(parent, "Divider", 0f, y, w, 1f, color);
    }

    static Text Value(Transform parent, string name, string content, int size, Color color,
                      float x, float y, float w, float h, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        return Txt(parent, name, content, size, color, anchor, x, y, w, h, true);
    }

    static UIRingGraphic Ring(Transform parent, string name, float x, float y, float size,
                              float[] values, Color[] colors, float widthRatio,
                              float start, float sweep, float gap)
    {
        RectTransform rt = Rect(parent, name, x, y, size, size);
        var ring = rt.gameObject.AddComponent<UIRingGraphic>();
        ring.SetData(values, colors, widthRatio, start, sweep, gap);
        ring.raycastTarget = false;
        return ring;
    }

    // ------------------------------------------------------------------ 左 1 产线总览

    static void BuildOverview(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        Txt(r, "Tag", "今日", 9, Muted, TextAnchor.UpperRight, w - 134f, 6f, 48f, 14f);
        Txt(r, "Label", "今日生产总量", 10, Muted, TextAnchor.UpperLeft, 14f, 8f, 150f, 14f);
        Value(r, "Total", "318", 34, Ink, 12f, 22f, 150f, 42f, TextAnchor.UpperLeft);
        Txt(r, "Unit", "吨", 10, Muted, TextAnchor.UpperLeft, 92f, 48f, 20f, 14f);

        Img(r, "RunDot", 14f, 74f, 6f, 6f, Ok);
        Txt(r, "Run", "运行正常", 9, Muted, TextAnchor.MiddleLeft, 26f, 68f, 56f, 14f);
        Value(r, "RunDelta", "+1.8%", 10, Ok, 84f, 68f, 44f, 14f);
        Txt(r, "RunNote", "较昨日", 9, Muted, TextAnchor.MiddleLeft, 126f, 68f, 44f, 14f);

        Divider(r, 90f, w, Line);
        Txt(r, "Online",
            "在线设备 <color=#EDF7FF><b>31</b></color> 台        运行 <color=#EDF7FF><b>27</b></color> 台        报警 <color=#F6C063><b>1</b></color> 台",
            9, Muted, TextAnchor.UpperLeft, 14f, 100f, w - 28f, 14f);

        Divider(r, 122f, w, Line);

        float col = (w - 28f) / 3f;
        string[] labels = { "已完成工单", "在制批次", "成品库存" };
        string[] values = { "42", "8", "1,286" };
        for (int i = 0; i < 3; i++)
        {
            float x = 14f + i * col;
            if (i > 0) Img(r, "Sep" + i, x - 6f, 132f, 1f, 36f, LineSoft);
            Txt(r, "StatLabel" + i, labels[i], 9, Muted, TextAnchor.UpperLeft, x + 8f, 130f, col - 10f, 12f);
            Value(r, "Stat" + i, values[i], 17, Ink, x + 8f, 144f, col - 10f, 22f, TextAnchor.UpperLeft);
        }

        float ringSize = 92f;
        float ringX = w - ringSize - 14f;
        Ring(r, "RateTrack", ringX, 22f, ringSize, new float[] { 1f }, new Color[] { Track }, 0.16f, 90f, 360f, 0f);
        Ring(r, "RateRing", ringX, 22f, ringSize, new float[] { 87.1f, 12.9f },
             new Color[] { Cyan, new Color(0f, 0f, 0f, 0f) }, 0.16f, 90f, 360f, 0f);
        Value(r, "RateValue", "87.1%", 18, Cyan, ringX, 58f, ringSize, 20f, TextAnchor.MiddleCenter);
        Txt(r, "RateLabel", "设备开动率", 8, Muted, TextAnchor.MiddleCenter, ringX, 80f, ringSize, 12f);
    }

    // ------------------------------------------------------------------ 左 2 工段设备分布

    static void BuildSections(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        Txt(r, "Note", "各工段设备台数", 9, Muted, TextAnchor.UpperLeft, 14f, 6f, 160f, 12f);
        Txt(r, "Total", "合计 31 台", 9, Ok, TextAnchor.UpperRight, w - 100f, 6f, 86f, 12f);

        string[] names = { "原料与前处理", "一混配料发酵", "二混配料混合", "烘干包装码垛", "动力控制环保" };
        int[] counts = { 8, 7, 5, 5, 6 };
        float barX = 92f;
        float barW = w - 152f;

        for (int i = 0; i < 5; i++)
        {
            float y = 24f + i * 28f;
            Color c = i == 4 ? Blue : Cyan;
            Txt(r, "Name" + i, names[i], 9, Ink, TextAnchor.MiddleLeft, 14f, y, 76f, 16f);
            Img(r, "Track" + i, barX, y + 5f, barW, 6f, Track);
            Img(r, "Fill" + i, barX, y + 5f, barW * (counts[i] / 8f), 6f, c);
            Value(r, "Count" + i, counts[i] + " 台", 11, Ink, w - 58f, y, 44f, 16f, TextAnchor.MiddleRight);
        }

        Txt(r, "Foot", "粉碎、配料、发酵、混合、烘干、包装、环保全流程覆盖", 9, Muted, TextAnchor.UpperLeft, 14f, 172f, w - 28f, 12f);
    }

    // ------------------------------------------------------------------ 左 3 设备状态构成

    static void BuildStatus(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        string[] names = { "运行中", "待机", "报警", "检修" };
        int[] counts = { 27, 2, 1, 1 };
        float[] pct = { 87.1f, 6.5f, 3.2f, 3.2f };
        Color[] colors = { Cyan, Blue, Warn, new Color(0.58f, 0.68f, 0.76f, 1f) };

        float ringSize = 126f;
        float ringX = 16f;
        float ringY = 36f;
        Ring(r, "Donut", ringX, ringY, ringSize, new float[] { 27f, 2f, 1f, 1f }, colors, 0.30f, 90f, 360f, 3f);

        float ccx = ringX + ringSize * 0.5f;
        float ccy = ringY + ringSize * 0.5f;
        Value(r, "Total", "31", 22, Ink, ccx - 42f, ccy - 16f, 84f, 26f, TextAnchor.MiddleCenter);
        Txt(r, "TotalLabel", "在线设备", 8, Muted, TextAnchor.MiddleCenter, ccx - 42f, ccy + 10f, 84f, 12f);

        float lx = ringX + ringSize + 18f;
        for (int i = 0; i < 4; i++)
        {
            float y = 32f + i * 32f;
            Img(r, "Dot" + i, lx, y + 5f, 6f, 6f, colors[i]);
            Txt(r, "Name" + i, names[i], 10, Ink, TextAnchor.MiddleLeft, lx + 10f, y, 66f, 16f);
            Value(r, "Count" + i, counts[i] + " 台", 12, Ink, lx + 82f, y, 46f, 16f, TextAnchor.MiddleRight);
            Txt(r, "Pct" + i, pct[i].ToString("0.0") + "%", 9, Muted, TextAnchor.MiddleRight,
                lx + 134f, y + 1f, 48f, 14f);
        }

        Txt(r, "Foot", "运行 27 台 · 报警 1 台（除尘风机）", 9, Muted, TextAnchor.UpperLeft, 14f, 176f, w - 28f, 12f);
    }

    // ------------------------------------------------------------------ 左 4 产量趋势

    static void BuildOutputTrend(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        int[] tons = { 286, 302, 295, 318, 306, 324, 318 };
        float[] rates = { 84.2f, 85.6f, 84.9f, 86.8f, 87.0f, 87.4f, 87.1f };
        string[] days = { "11/07", "11/08", "11/09", "11/10", "11/11", "11/12", "11/13" };

        Txt(r, "L1", "■ 产量(吨)", 9, Muted, TextAnchor.UpperRight, w - 156f, 6f, 70f, 12f);
        Txt(r, "L2", "— 开动率", 9, Ok, TextAnchor.UpperRight, w - 78f, 6f, 66f, 12f);

        float left = 40f;
        float right = w - 16f;
        float areaW = right - left;
        float top = 26f;
        float plotH = 118f;
        float baseline = top + plotH;

        Txt(r, "Y400", "400", 8, Muted, TextAnchor.MiddleRight, 10f, top - 6f, 26f, 12f);
        Txt(r, "Y200", "200", 8, Muted, TextAnchor.MiddleRight, 10f, top + plotH * 0.5f - 6f, 26f, 12f);
        Txt(r, "Y0", "0", 8, Muted, TextAnchor.MiddleRight, 10f, baseline - 6f, 26f, 12f);
        Img(r, "Base", left, baseline, areaW, 1f, Line);

        float slot = areaW / 7f;
        float barW = 16f;

        for (int i = 0; i < 7; i++)
        {
            float cx = left + slot * (i + 0.5f);
            float h = plotH * (tons[i] / 400f);
            Img(r, "Bar" + i, cx - barW * 0.5f, baseline - h, barW, h, Blue);
            Txt(r, "BarValue" + i, tons[i].ToString(), 8, Muted, TextAnchor.LowerCenter,
                cx - 18f, baseline - h - 14f, 36f, 12f);
            Txt(r, "Day" + i, days[i], 8, Muted, TextAnchor.UpperCenter, cx - 22f, baseline + 4f, 44f, 12f);
        }

        RectTransform lineRect = Rect(r, "TrendLine", left, top, areaW, plotH);
        var line = lineRect.gameObject.AddComponent<UILineGraphic>();
        var points = new List<Vector2>();
        for (int i = 0; i < 7; i++)
        {
            float y = Mathf.Clamp01((rates[i] - 80f) / 20f);
            points.Add(new Vector2((i + 0.5f) / 7f, y));
        }
        line.SetData(points, 1.6f, 3f, Ok);
    }

    // ------------------------------------------------------------------ 右 1 实时运行监控

    static void BuildMonitor(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        string[] names = { "一混粉碎机", "一混发酵床", "天然气烘干机", "除尘风机" };
        string[] limits = { "电流 · 上限 80 A", "床温 · 上限 45 ℃", "进风 · 上限 95 ℃", "风压 · 上限 1.8 kPa" };
        string[] values = { "62", "38.5", "78", "1.62" };
        string[] units = { "A", "℃", "℃", "kPa" };
        float[] ratios = { 0.78f, 0.86f, 0.82f, 0.90f };
        Color[] colors = { Cyan, Warn, Blue, Warn };

        float barX = 96f;
        float barW = w - 190f;

        for (int i = 0; i < 4; i++)
        {
            float y = 16f + i * 42f;
            bool warn = ratios[i] >= 0.85f;

            Txt(r, "Name" + i, names[i], 10, Ink, TextAnchor.MiddleLeft, 14f, y, 82f, 16f);
            Txt(r, "Limit" + i, limits[i], 8, Muted, TextAnchor.MiddleLeft, 14f, y + 15f, 82f, 12f);

            Img(r, "Track" + i, barX, y + 8f, barW, 5f, Track);
            Img(r, "Fill" + i, barX, y + 8f, barW * ratios[i], 5f, colors[i]);
            Img(r, "Mark" + i, barX + barW - 1f, y + 4f, 1f, 13f, Warn);

            Value(r, "Value" + i, values[i], 15, warn ? Warn : Ink,
                  w - 82f, y, 38f, 20f, TextAnchor.MiddleRight);
            Txt(r, "Unit" + i, units[i], 8, Muted, TextAnchor.MiddleLeft, w - 42f, y + 7f, 28f, 12f);
        }
    }

    // ------------------------------------------------------------------ 右 2 工段设备状态

    static void BuildSectionStatus(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        string[] legend = { "运行", "待机", "报警", "检修" };
        Color[] legendColors = { Cyan, Blue, Warn, new Color(0.58f, 0.68f, 0.76f, 1f) };
        float lx = 14f;
        for (int i = 0; i < 4; i++)
        {
            Img(r, "LDot" + i, lx, 7f, 6f, 6f, legendColors[i]);
            Txt(r, "LLabel" + i, legend[i], 8, Muted, TextAnchor.MiddleLeft, lx + 9f, 3f, 34f, 14f);
            lx += 44f;
        }

        string[] names = { "前处理", "一混", "二混", "烘干包装", "动力环保" };
        int[][] parts =
        {
            new int[] { 7, 1, 0, 0 },
            new int[] { 6, 1, 0, 0 },
            new int[] { 5, 0, 0, 0 },
            new int[] { 5, 0, 0, 0 },
            new int[] { 4, 0, 1, 1 }
        };
        int[] totals = { 8, 7, 5, 5, 6 };

        float baseline = 150f;
        float plotH = 104f;
        float max = 9f;
        float slot = (w - 28f) / 5f;
        float barW = 26f;

        for (int i = 0; i < 5; i++)
        {
            float cx = 14f + slot * (i + 0.5f);
            float y = baseline;
            for (int s = 0; s < 4; s++)
            {
                float h = plotH * (parts[i][s] / max);
                y -= h;
                if (parts[i][s] > 0) Img(r, "Seg" + i + "_" + s, cx - barW * 0.5f, y, barW, h, legendColors[s]);
            }
            Value(r, "Total" + i, totals[i].ToString(), 10, Ink, cx - 18f, y - 16f, 36f, 14f, TextAnchor.LowerCenter);
            Txt(r, "Name" + i, names[i], 8, Muted, TextAnchor.UpperCenter, cx - 28f, baseline + 4f, 56f, 12f);
        }

        Txt(r, "Foot", "运行 27 · 待机 2 · 报警 1 · 检修 1", 9, Muted, TextAnchor.UpperRight, w - 196f, 176f, 182f, 12f);
    }

    // ------------------------------------------------------------------ 右 3 设备综合效率

    static void BuildOee(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        float size = 118f;
        float gx = 16f;
        float gy = 16f;
        Ring(r, "Track", gx, gy, size, new float[] { 1f }, new Color[] { Track }, 0.11f, 205f, 235f, 0f);
        Ring(r, "Gauge", gx, gy, size, new float[] { 87.1f, 12.9f },
             new Color[] { Cyan, new Color(0f, 0f, 0f, 0f) }, 0.11f, 205f, 235f, 0f);

        float ccx = gx + size * 0.5f;
        float ccy = gy + size * 0.5f;
        Value(r, "Big", "87.1%", 25, Cyan, ccx - 70f, ccy - 15f, 140f, 26f, TextAnchor.MiddleCenter);
        Txt(r, "BigLabel", "OEE", 8, Muted, TextAnchor.MiddleCenter, ccx - 50f, ccy + 11f, 100f, 12f);

        string[] ticks = { "0", "25", "50", "75", "100" };
        float tickRadius = size * 0.5f - 10f;
        for (int i = 0; i < 5; i++)
        {
            float deg = 205f - 235f * i / 4f;
            float rad = deg * Mathf.Deg2Rad;
            float tx = ccx + Mathf.Cos(rad) * tickRadius - 14f;
            float ty = ccy - Mathf.Sin(rad) * tickRadius - 6f;
            Txt(r, "Tick" + i, ticks[i], 7, Muted, TextAnchor.MiddleCenter, tx, ty, 28f, 12f);
        }

        string[] labels = { "时间开动率", "性能开动率", "合格品率" };
        string[] values = { "91.2%", "95.6%", "99.7%" };
        for (int i = 0; i < 3; i++)
        {
            float x = w - 198f;
            float y = 34f + i * 26f;
            Txt(r, "SL" + i, labels[i], 9, Muted, TextAnchor.MiddleLeft, x, y, 90f, 14f);
            Value(r, "S" + i, values[i], 13, Ink, x + 92f, y, 76f, 16f, TextAnchor.MiddleRight);
        }

        Divider(r, 140f, w, LineSoft);
        Txt(r, "Note", "目标 85% · 较昨日 +0.6%", 9, Muted, TextAnchor.MiddleLeft, 14f, 146f, w - 28f, 12f);
        Img(r, "BarTrack", 14f, 162f, w - 28f, 6f, Track);
        Img(r, "BarFill", 14f, 162f, (w - 28f) * 0.871f, 6f, Cyan);

        Txt(r, "Foot", "累计运行 6,420 h · 故障 2 次", 9, Muted, TextAnchor.UpperLeft, 14f, 178f, w - 28f, 12f);
    }

    // ------------------------------------------------------------------ 右 4 设备状态清单

    static void BuildDeviceList(RectTransform frame)
    {
        RectTransform r = Begin(frame);
        float w = r.sizeDelta.x;

        float c1 = 14f;
        float c2 = w * 0.56f;
        float c3 = w - 74f;

        Txt(r, "H1", "设备名称", 9, Muted, TextAnchor.MiddleLeft, c1, 8f, 200f, 14f);
        Txt(r, "H2", "工段", 9, Muted, TextAnchor.MiddleLeft, c2, 8f, 90f, 14f);
        Txt(r, "H3", "状态", 9, Muted, TextAnchor.MiddleRight, c3, 8f, 60f, 14f);
        Divider(r, 24f, w, Line);

        string[] names = { "一混粉碎机", "一混发酵床", "二混混合机", "天然气烘干机", "除尘风机" };
        string[] sections = { "前处理", "一混", "二混", "烘干", "环保" };
        string[] states = { "运行", "运行", "运行", "运行", "报警" };
        bool[] alarm = { false, false, false, false, true };

        for (int i = 0; i < 5; i++)
        {
            float y = 30f + i * 25f;
            Color sc = alarm[i] ? Warn : Ok;
            Txt(r, "N" + i, names[i], 9, Ink, TextAnchor.MiddleLeft, c1, y, 130f, 14f);
            Txt(r, "C" + i, sections[i], 9, Muted, TextAnchor.MiddleLeft, c2, y, 60f, 14f);
            Img(r, "Dot" + i, c3 - 4f, y + 5f, 5f, 5f, sc);
            Txt(r, "S" + i, states[i], 9, sc, TextAnchor.MiddleRight, c3 + 4f, y, 46f, 14f);
            Divider(r, y + 21f, w, LineSoft);
        }

        Txt(r, "FootL", "在线 31 台", 9, Muted, TextAnchor.UpperLeft, 14f, 176f, 110f, 12f);
        Txt(r, "FootC", "运行 27 台", 9, Muted, TextAnchor.UpperCenter, w * 0.38f, 176f, 110f, 12f);
        Txt(r, "FootR", "报警 1 台", 9, Warn, TextAnchor.UpperRight, w - 118f, 176f, 104f, 12f);
    }
}
