using System;
using System.Collections.Generic;
using System.Globalization;
using Daima;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>main5 图表部分：左 02 折线、右侧主柱状图 / 构成 / 环比 / 数据表。</summary>
public static partial class Main5SafetyEnergyBuilder
{
    // ================================================================= 左 02 能耗趋势

    const float ENERGY_TOP = 26.5f;
    const float ENERGY_BOTTOM = 152.5f;
    const float ENERGY_MIN = 17000f;
    const float ENERGY_MAX = 20000f;

    static void BuildEnergyChart(RectTransform p, out RectTransform mask,
        out RectTransform pointer, out RectTransform zone)
    {
        RectTransform chart = DashUI.New("mEnergyChart", p, 16f, -37f, 533.75f, 175.63f)
            .GetComponent<RectTransform>();

        // 网格
        HLine(chart, "mEnergyAxis", 42f, 152.5f, 468f, new Color(0.627f, 0.824f, 0.941f, 0.22f));
        float[] ys = { 26.5f, 68.5f, 110.5f };
        for (int i = 0; i < ys.Length; i++)
            DashH(chart, "mEnergySplit", 42f, ys[i], 468f, new Color(0.627f, 0.824f, 0.941f, 0.13f));

        // y 轴标签
        for (int v = 20000; v >= 17000; v -= 1000)
        {
            float top = ValueToEnergyY(v) - 6.5f;
            TxNum(chart, "mEnergyY", 0f, top, 34f, 13f, (v / 1000) + "k", FS(9.5f),
                Hex("#bed9e8"), TextAnchor.MiddleRight);
        }

        // x 轴标签
        for (int i = 0; i < ENERGY_DAYS.Length; i++)
        {
            float cx = 42f + i / (ENERGY_DAYS.Length - 1f) * 468f;
            TxNum(chart, "mEnergyX", cx - 40f, 158.5f, 80f, 13f, ENERGY_DAYS[i], FS(10f),
                Hex("#bed9e8"), TextAnchor.MiddleCenter);
        }

        // 动画裁剪层
        GameObject maskGo = DashUI.New("mEnergyMask", chart, 42f, -26.5f, 468f, 126f);
        mask = maskGo.GetComponent<RectTransform>();
        RectMask2D clip = maskGo.AddComponent<RectMask2D>();

        float[] nxs = new float[ENERGY_VALUES.Length];
        float[] nys = new float[ENERGY_VALUES.Length];
        for (int i = 0; i < ENERGY_VALUES.Length; i++)
        {
            nxs[i] = i / (ENERGY_VALUES.Length - 1f);
            nys[i] = Mathf.Clamp01((ENERGY_VALUES[i] - ENERGY_MIN) / (ENERGY_MAX - ENERGY_MIN));
        }

        List<Vector2> curve = SampleEnergyCurve(nxs, nys, 20);

        GameObject areaGo = NewFull(mask, "mEnergyArea", typeof(Main5AreaGraphic));
        Main5AreaGraphic area = areaGo.GetComponent<Main5AreaGraphic>();
        area.raycastTarget = false;
        area.SetData(curve, new Color(0.435f, 0.827f, 1f, 0.30f), new Color(0.435f, 0.827f, 1f, 0.02f));

        GameObject lineGo = NewFull(mask, "mEnergyLine", typeof(UILineGraphic));
        UILineGraphic line = lineGo.GetComponent<UILineGraphic>();
        line.raycastTarget = false;
        line.SetData(curve, 2f, 0f, Hex("#6fd3ff"));

        // 节点
        for (int i = 0; i < ENERGY_VALUES.Length; i++)
        {
            float lx = nxs[i] * 468f;
            float ly = (1f - nys[i]) * 126f;
            UIRoundRect dot = RRC(mask, "mEnergyDot" + i, lx, ly, 7f, Hex("#6fd3ff"));
            dot.borderColor = new Color(0.031f, 0.086f, 0.133f, 0.9f);
            dot.borderWidth = 1.6f;
            dot.Refresh();
        }

        // 末点标记文字
        Text mark = TxNum(chart, "mEnergyMark", 470f, 46.5f, 80f, 14f, "18,920", FS(10.5f),
            Hex("#eaf6ff"), TextAnchor.MiddleCenter);
        Outline ol = mark.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.02f, 0.063f, 0.102f, 0.85f);
        ol.effectDistance = new Vector2(1f, -1f);

        // 悬停竖线
        RectTransform ptr = Img(chart, "mEnergyPointer", 42f, 26f, 1f, 126.5f,
            new Color(0.494f, 0.804f, 0.961f, 0.45f));
        pointer = ptr;
        pointer.gameObject.SetActive(false);

        // 命中区
        zone = Zone(chart, "mEnergyZone", 42f, 26f, 468f, 126.5f, 300);

        if (clip == null) { }
    }

    static float ValueToEnergyY(float v)
    {
        return ENERGY_BOTTOM - (v - ENERGY_MIN) / (ENERGY_MAX - ENERGY_MIN) *
            (ENERGY_BOTTOM - ENERGY_TOP);
    }

    /// <summary>按 ECharts smooth(0.35) 的端点控制点规则采样平滑折线。</summary>
    static List<Vector2> SampleEnergyCurve(float[] xs, float[] ys, int steps)
    {
        int n = xs.Length;
        var result = new List<Vector2>((n - 1) * steps + 1);
        if (n < 2) return result;

        for (int i = 0; i < n - 1; i++)
        {
            float x0 = xs[i], y0 = ys[i];
            float x1 = xs[i + 1], y1 = ys[i + 1];
            float dx = x1 - x0;

            float c1x, c2x;
            if (i == 0 || i == n - 2) c1x = x0;
            else c1x = x0 + dx * 0.345f;
            if (i == n - 2) c2x = x0 + dx * 0.658f;
            else if (i == 0) c2x = x1 - dx * 0.356f;
            else c2x = x1 - dx * 0.345f;

            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                float mt = 1f - t;
                float px = mt * mt * mt * x0 + 3f * mt * mt * t * c1x +
                           3f * mt * t * t * c2x + t * t * t * x1;
                float py = mt * mt * mt * y0 + 3f * mt * mt * t * y0 +
                           3f * mt * t * t * y1 + t * t * t * y1;
                result.Add(new Vector2(px, py));
            }
        }
        result.Add(new Vector2(xs[n - 1], ys[n - 1]));
        return result;
    }

    // ================================================================= 右侧主图

    static readonly float[] MAIN_X_MAX = { 50000f, 21000f, 18000f, 15000f, 10000f, 15000f };
    static readonly float[] MAIN_STEP = { 10000f, 3000f, 3000f, 3000f, 2000f, 3000f };

    static void BuildMainChart(RectTransform wrap, Section s, int index)
    {
        RectTransform chart = DashUI.New("mMainChart", wrap, 0f, 0f, 519.95f, WRAP_MAIN_H)
            .GetComponent<RectTransform>();

        float xMax = MAIN_X_MAX[index];
        float step = MAIN_STEP[index];
        const float PL = 44f, PR = 506f, PT = 34f, PB = 196f;
        float plotW = PR - PL;
        float plotH = PB - PT;

        // 横向网格 + 数值刻度
        for (float v = 0f; v <= xMax + 0.1f; v += step)
        {
            float y = PB - v / xMax * plotH;
            HLine(chart, "mMainSplit", PL, y, plotW, new Color(0.627f, 0.824f, 0.941f, 0.10f));
            string label = v < 1f ? "0" : (v / 1000f).ToString("0") + "k";
            TxNum(chart, "mMainTick", 4f, y - 8f, 36f, 16f, label, FS(7.5f),
                Hex("#7fa8bc"), TextAnchor.MiddleRight);
        }

        // 基线
        HLine(chart, "mMainAxis", PL, PB, plotW, new Color(0.627f, 0.824f, 0.941f, 0.2f));

        // 类别标签（柱下方）
        int n = s.Devices.Length;
        float band = plotW / n;
        for (int i = 0; i < n; i++)
        {
            TxCN(chart, "mMainCat", PL + i * band, PB + 4f, band, 18f, s.Devices[i].Name,
                FS(8.5f), Hex("#d8edf8"), TextAnchor.MiddleCenter);
        }

        // 图例
        UIRoundRect icon1 = RR(chart, "mMainLegendDot1", 386f, 6f, 9f, 9f, SERIES1, 5f,
            Color.clear, 0f);
        UIRoundRect icon2 = RR(chart, "mMainLegendDot2", 436f, 6f, 9f, 9f, SERIES2, 5f,
            Color.clear, 0f);
        if (icon1 == null || icon2 == null) { }
        TxCN(chart, "mMainLegend1", 400f, 1f, 44f, 22f, "本月", FS(10f), Hex("#b6d3e3"));
        TxCN(chart, "mMainLegend2", 450f, 1f, 44f, 22f, "上月", FS(10f), Hex("#b6d3e3"));

        // 悬停高亮（位于柱后）
        UIRoundRect highlight = RR(chart, "mBarHighlight", PL, PT, 0f, 0f,
            new Color(0.494f, 0.804f, 0.941f, 0.08f), 0f, Color.clear, 0f);
        highlight.gameObject.SetActive(false);

        // 柱（竖向分组）
        float barW = n >= 6 ? 14f : 16f;
        float gap = barW * 0.35f;
        float groupW = barW * 2f + gap;

        for (int i = 0; i < n; i++)
        {
            float center = PL + (i + 0.5f) * band;
            float nowX = center - groupW * 0.5f;
            float prevX = nowX + barW + gap;

            float nowH = Mathf.Max(1f, s.Devices[i].Now / xMax * plotH);
            float prevH = Mathf.Max(1f, s.Devices[i].Prev / xMax * plotH);

            UIRoundRect prev = AddVerticalBar(chart, "mBarPrev" + i, prevX, PB, barW, prevH,
                SERIES2, Hex("#e3f1f8"));
            UIRoundRect now = AddVerticalBar(chart, "mBarNow" + i, nowX, PB, barW, nowH,
                SERIES1, Hex("#8fe6ff"));
            if (prev == null || now == null) { }

            string label = FormatBarValue(s.Devices[i].Now);
            TxNum(chart, "mBarLabel" + i, center - 35f, PB - nowH - 20f, 70f, 18f,
                label, FS(8f), Hex("#eaf6ff"), TextAnchor.MiddleCenter);
        }

        // 命中区
        Zone(chart, "mBarZone", PL, PT, plotW, plotH, 400 + index);
    }

    static UIRoundRect AddVerticalBar(RectTransform chart, string name, float x, float baseline,
        float w, float h, Color bottom, Color top)
    {
        GameObject go = DashUI.New(name, chart, x, -baseline, w, h, 0f, 0f);
        UIRoundRect rr = go.AddComponent<UIRoundRect>();
        rr.color = bottom;
        rr.color2 = top;
        rr.gradient = true;
        rr.verticalGradient = true;
        rr.radii = new Vector4(5f, 5f, 0f, 0f);
        rr.raycastTarget = false;
        rr.Refresh();
        return rr;
    }

    static string FormatBarValue(float v)
    {
        return v >= 1000f ? (v / 1000f).ToString("0.0") + "k" : v.ToString("0");
    }

    // ================================================================= 构成

    static void BuildMixBlock(RectTransform wrap, Section s, int index)
    {
        float topBlock = WRAP_MIX_TOP;
        float topBar = topBlock + 14f;
        float topLegend = topBlock + 40f;
        const float W = 519.95f;

        float total = 0f;
        for (int i = 0; i < s.Devices.Length; i++) total += s.Devices[i].Now;

        // 小标题
        TxCN(wrap, "mMixSub", 1f, topBlock + 1f, 104f, 26f, "本月电耗构成", FS(11f),
            Hex("#dceff9"));
        TxDisplay(wrap, "mMixSubEn", 112f, topBlock + 5.5f, 150f, 20f, "ENERGY MIX", FS(8.5f),
            Hex("#8fb0c4"), TextAnchor.MiddleLeft, 2f);
        TxCN(wrap, "mMixTotal", W - 190f, topBlock + 1.5f, 190f, 24f,
            total.ToString("N0", CultureInfo.InvariantCulture) + " kWh", FS(10f),
            Hex("#92b4c7"), TextAnchor.MiddleRight);

        // 堆叠条（动画裁剪）
        UIRoundRect mixGlow = RR(wrap, "mMixGlow", 0f, topBar - 4f, W, 32f,
            new Color(0.435f, 0.827f, 1f, 0.10f), 6f, Color.clear, 0f);
        if (mixGlow == null) { }
        GameObject maskGo = DashUI.New("mMixMask", wrap, 0f, -topBar, W, 24f);
        RectTransform mask = maskGo.GetComponent<RectTransform>();
        maskGo.AddComponent<RectMask2D>();

        float cursor = 0f;
        for (int i = 0; i < s.Devices.Length; i++)
        {
            float frac = total > 0f ? s.Devices[i].Now / total : 0f;
            float w = frac * W;
            if (w <= 0.6f) continue;

            UIRoundRect seg = RR(mask, "mMixSeg" + i, cursor, 3f, w, 18f,
                MIX_COLORS[i % MIX_COLORS.Length], 0f, new Color(0.031f, 0.086f, 0.133f, 0.92f), 1.5f);
            seg.gradient = true;
            seg.verticalGradient = true;
            seg.color2 = Color.Lerp(MIX_COLORS[i % MIX_COLORS.Length], Color.white, 0.16f);
            if (i == 0) seg.radii = new Vector4(4f, 0f, 0f, 4f);
            else if (i == s.Devices.Length - 1) seg.radii = new Vector4(0f, 4f, 4f, 0f);
            seg.Refresh();

            cursor += w;
        }

        // 图例（单行，与参考图一致）
        int n = s.Devices.Length;
        float itemW = W / Mathf.Max(1, n);
        for (int i = 0; i < n; i++)
        {
            float lx = i * itemW;
            float ly = topLegend;

            float pct = total > 0f ? s.Devices[i].Now / total * 100f : 0f;
            string label = s.Devices[i].Name;
            string value = pct.ToString("0.0") + "%";
            int fs = FS(7.5f);
            float labelW = TextWidth(label, CN, fs);
            float valueW = TextWidth(value, NUM, fs);

            RRC(wrap, "mMixLegendDot" + i, lx + 2f, ly + 6.5f, 7f,
                MIX_COLORS[i % MIX_COLORS.Length]);
            TxCN(wrap, "mMixLegendK" + i, lx + 11f, ly, labelW + 2f, 20f,
                label, fs, Hex("#b6d3e3"));
            TxNum(wrap, "mMixLegendV" + i, lx + 11f + labelW + 3f, ly,
                valueW + 3f, 20f, value, fs, Hex("#eaf6ff"));
        }

        // 命中区
        Zone(wrap, "mMixZone", 0f, topBar, W, 24f, 500 + index);
    }

    // ================================================================= 环比增幅

    static void BuildChangeBlock(RectTransform wrap, Section s, int index)
    {
        const float topBlock = WRAP_CHANGE_TOP;
        const float topChart = WRAP_CHANGE_TOP + 10f;
        const float W = 519.95f;
        const float PL = 124f, PR = 466f, PT = 6f, PB = 190f;
        float plotW = PR - PL;
        float plotH = PB - PT;

        var rows = new List<Device>(s.Devices);
        rows.Sort((a, b) => PctOf(b).CompareTo(PctOf(a)));

        float hi = 0f;
        for (int i = 0; i < rows.Count; i++) hi = Mathf.Max(hi, PctOf(rows[i]));
        float axisMax = Mathf.Max(5.5f, hi * 1.15f);

        float totalNow = 0f, totalPrev = 0f;
        for (int i = 0; i < s.Devices.Length; i++)
        {
            totalNow += s.Devices[i].Now;
            totalPrev += s.Devices[i].Prev;
        }
        float overall = totalPrev > 0f ? (totalNow - totalPrev) / totalPrev * 100f : 0f;

        // 小标题
        TxCN(wrap, "mChangeSub", 1f, topBlock + 1f, 76f, 24f, "环比增幅", FS(11f),
            Hex("#dceff9"));
        TxDisplay(wrap, "mChangeSubEn", 76f, topBlock + 4.5f, 90f, 20f, "MOM CHANGE", FS(8.5f),
            Hex("#8fb0c4"), TextAnchor.MiddleLeft, 1f);
        TxCN(wrap, "mChangeNote", W - 281f, topBlock + 2f, 280f, 22f,
            "按增幅排序 · 整体 " + (overall >= 0f ? "+" : "") + overall.ToString("0.0") + "%",
            FS(10f), Hex("#92b4c7"), TextAnchor.MiddleRight);

        RectTransform chart = DashUI.New("mChangeChart", wrap, 0f, -topChart, W, 198f)
            .GetComponent<RectTransform>();

        // 竖向刻度网格
        for (int k = 0; k <= 4; k++)
        {
            float x = PL + k / 4f * plotW;
            DashV(chart, "mChangeSplit", x, PT, plotH,
                new Color(0.627f, 0.824f, 0.941f, 0.08f));
        }
        DashV(chart, "mChangeSplitEnd", PR, PT, plotH,
            new Color(0.627f, 0.824f, 0.941f, 0.08f));

        float band = plotH / rows.Count;

        // 整体均值参考线
        float xAvg = PL + Mathf.Clamp(overall / axisMax, 0f, 1f) * plotW;
        DashV(chart, "mChangeAvgLine", xAvg, PT - 2f, plotH + 4f,
            new Color(0.290f, 0.871f, 0.502f, 0.48f));
        Img(chart, "mChangeAvgCap", xAvg - 6f, PT - 4f, 12f, 2f,
            new Color(0.290f, 0.871f, 0.502f, 0.85f));

        for (int i = 0; i < rows.Count; i++)
        {
            float pct = PctOf(rows[i]);
            float cy = PT + i * band + band * 0.5f;

            TxCN(chart, "mChangeCat" + i, 0f, cy - 10f, 118f, 20f, rows[i].Name,
                FS(10.5f), Hex("#d8edf8"), TextAnchor.MiddleRight);

            // 轨道 + 渐变填充（0 起点胶囊进度条）
            RR(chart, "mChangeTrack" + i, PL, cy - 5f, plotW, 10f,
                new Color(0.627f, 0.824f, 0.941f, 0.10f), 5f, Color.clear, 0f);

            float vx = PL + Mathf.Clamp01(pct / axisMax) * plotW;
            UIRoundRect fill = RR(chart, "mChangeFill" + i, PL, cy - 5f,
                Mathf.Max(4f, vx - PL), 10f, Hex("#3987e5"), 5f, Color.clear, 0f);
            fill.gradient = true;
            fill.verticalGradient = false;
            fill.color2 = Hex("#8fe6ff");
            fill.Refresh();

            string label = (pct >= 0f ? "+" : "") + pct.ToString("0.0") + "%";
            TxNum(chart, "mChangeLabel" + i, PR + 8f, cy - 10f, 46f, 20f, label,
                FS(10f), pct >= 0f ? Hex("#4ade80") : Hex("#ff8a9a"),
                TextAnchor.MiddleLeft);
        }

        Zone(chart, "mChangeZone", PL, PT, plotW, plotH, 600 + index);
    }

    static float PctOf(Device d)
    {
        return d.Prev > 0f ? (d.Now - d.Prev) / d.Prev * 100f : 0f;
    }

    // ================================================================= 数据表

    static void BuildTableView(RectTransform wrap, Section s, int index, out GameObject view)
    {
        RectTransform root = DashUI.New("mTableView", wrap, 0f, 0f, 519.95f, CHART_WRAP_H)
            .GetComponent<RectTransform>();
        view = root.gameObject;

        UIRoundRect bg = root.gameObject.AddComponent<UIRoundRect>();
        bg.color = new Color(0.031f, 0.086f, 0.133f, 0.96f);
        bg.radius = 5f;
        bg.borderColor = new Color(0.494f, 0.804f, 0.961f, 0.2f);
        bg.borderWidth = 1f;
        bg.raycastTarget = true;

        // 标题
        TxCN(root, "mTableTitle", 14f, 9f, 320f, 24f,
            index == 0 ? "分工段电耗明细" : "分设备电耗明细", FS(10f), Hex("#eaf6ff"));
        TxCN(root, "mTableUnit", 395.95f, 9f, 110f, 24f, "单位：kWh", FS(8.5f),
            Hex("#92b4c7"), TextAnchor.MiddleRight);

        // 表格
        const float tableX = 9f;
        const float tableW = 501.95f;
        float[] cols = { 152.34f, 125.92f, 126.31f, 97.38f };
        float[] colX = new float[4];
        colX[0] = tableX;
        for (int i = 1; i < 4; i++) colX[i] = colX[i - 1] + cols[i - 1];

        const float headTop = 36f;
        const float headH = 30f;
        const float rowH = 34f;

        Img(root, "mTableHeadBg", tableX, headTop, tableW, headH,
            new Color(0.039f, 0.110f, 0.165f, 0.98f));
        HLine(root, "mTableHeadLine", tableX, headTop + headH, tableW,
            new Color(0.494f, 0.804f, 0.961f, 0.18f));

        string[] heads = { "设备 / 工段", "本月", "上月", "环比" };
        for (int c = 0; c < 4; c++)
        {
            if (c == 0)
                TxCN(root, "mTableTh" + c, colX[c] + 8f, headTop, cols[c] - 13f, headH,
                    heads[c], FS(9.5f), Hex("#92b4c7"), TextAnchor.MiddleLeft);
            else
                TxCN(root, "mTableTh" + c, colX[c] + 5f, headTop, cols[c] - 10f, headH,
                    heads[c], FS(9.5f), Hex("#92b4c7"), TextAnchor.MiddleRight);
        }

        float totalNow = 0f, totalPrev = 0f;
        int n = s.Devices.Length;
        for (int i = 0; i < n; i++)
        {
            Device d = s.Devices[i];
            totalNow += d.Now;
            totalPrev += d.Prev;
            float top = headTop + headH + i * rowH;
            float pct = PctOf(d);
            Color pctColor = pct >= 0f ? Hex("#ffcf86") : Hex("#6fe3ae");

            TxCN(root, "mTableName" + i, colX[0] + 8f, top, cols[0] - 13f, rowH,
                d.Name, FS(10f), Hex("#b6d3e3"), TextAnchor.MiddleLeft);
            TxNum(root, "mTableNow" + i, colX[1] + 5f, top, cols[1] - 10f, rowH,
                d.Now.ToString("N0", CultureInfo.InvariantCulture), FS(10.5f),
                Hex("#d8edf8"), TextAnchor.MiddleRight);
            TxNum(root, "mTablePrev" + i, colX[2] + 5f, top, cols[2] - 10f, rowH,
                d.Prev.ToString("N0", CultureInfo.InvariantCulture), FS(10.5f),
                Hex("#d8edf8"), TextAnchor.MiddleRight);
            TxNum(root, "mTablePct" + i, colX[3] + 5f, top, cols[3] - 10f, rowH,
                (pct >= 0f ? "+" : "") + pct.ToString("0.0") + "%", FS(10.5f),
                pctColor, TextAnchor.MiddleRight);

            HLine(root, "mTableRowLine" + i, tableX, top + rowH, tableW,
                new Color(0.494f, 0.804f, 0.961f, 0.08f));
        }

        // 合计
        float footTop = headTop + headH + n * rowH;
        HLine(root, "mTableFootLine", tableX, footTop, tableW,
            new Color(0.494f, 0.804f, 0.961f, 0.2f));
        float totalPct = totalPrev > 0f ? (totalNow - totalPrev) / totalPrev * 100f : 0f;
        Color totalColor = totalPct >= 0f ? Hex("#ffcf86") : Hex("#6fe3ae");

        TxCN(root, "mTableFootName", colX[0] + 8f, footTop, cols[0] - 13f, 32f,
            "合计", FS(10f), Hex("#b6d3e3"), TextAnchor.MiddleLeft);
        TxNum(root, "mTableFootNow", colX[1] + 5f, footTop, cols[1] - 10f, 32f,
            totalNow.ToString("N0", CultureInfo.InvariantCulture), FS(10.5f),
            Hex("#eaf6ff"), TextAnchor.MiddleRight, true);
        TxNum(root, "mTableFootPrev", colX[2] + 5f, footTop, cols[2] - 10f, 32f,
            totalPrev.ToString("N0", CultureInfo.InvariantCulture), FS(10.5f),
            Hex("#eaf6ff"), TextAnchor.MiddleRight, true);
        TxNum(root, "mTableFootPct", colX[3] + 5f, footTop, cols[3] - 10f, 32f,
            (totalPct >= 0f ? "+" : "") + totalPct.ToString("0.0") + "%", FS(10.5f),
            totalColor, TextAnchor.MiddleRight, true);

        // 提示
        HLine(root, "mTableHintLine", tableX, 318f, tableW,
            new Color(0.275f, 0.627f, 0.863f, 0.1f));
        TxCN(root, "mTableHint", 14f, 324f, tableW - 10f, 24f,
            "数据表视图 · 点击右上角“关闭表格”返回图表", FS(9f), Hex("#8fb0c4"));

        view.SetActive(false);
    }

    static GameObject NewFull(RectTransform parent, string name, Type graphic)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = 5;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        go.AddComponent(graphic);
        return go;
    }
}

// build trigger
