using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Canvas main4（质量检测）图表构建器：清空容器后按当前布局重建。</summary>
public static class QualityDashboardBuilder
{
    static readonly string[] NcBatch = { "B2610-032", "B2610-028", "B2610-041", "B2610-019", "B2610-035", "B2610-044", "B2610-011", "B2610-047" };
    static readonly string[] NcPhen = { "片剂裂片", "含量不均", "铝塑板密封不良", "浸膏水分超标", "装量差异超限", "可见异物", "微生物限度超标", "外观色差" };
    static readonly string[] NcWhy = { "压片压力波动", "混合时间不足", "热封温度偏低", "干燥参数漂移", "计量泵磨损", "灯检漏检复核中", "洁净区压差异常", "包衣喷速不稳" };
    static readonly string[] NcAct = { "返工", "返工", "返工", "报废", "返工", "复检", "报废", "复检" };

    [MenuItem("大屏/重建 main4 质量检测图表")]
    public static void Build()
    {
        var main = DashUI.FindRoot("Canvas main4");
        if (main == null) { Debug.LogError("场景中找不到 Canvas main4"); return; }
        var left = DashUI.FindRoot("Canvas left4");
        var right = DashUI.FindRoot("Canvas right4");
        if (left == null || right == null) { Debug.LogError("找不到 Canvas left4 / Canvas right4"); return; }

        RectTransform[] slots =
        {
            DashUI.Child(left, "Image1"), DashUI.Child(left, "Image2"), DashUI.Child(left, "Image3"),
            DashUI.Child(right, "Image3"), DashUI.Child(right, "Image4"), DashUI.Child(right, "Image5")
        };
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) { Debug.LogError("缺少图表容器 Image，序号 " + i); return; }
            DashUI.Clear(slots[i]);
            DashUI.ClearBg(slots[i]);
        }

        Overview(slots[0]);
        NcReport(slots[1]);
        Pareto(slots[2]);
        Workshop(slots[3]);
        Trend(slots[4]);
        Rework(slots[5]);
        Debug.Log("main4 质量检测图表重建完成");
    }

    static void Overview(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "质量总览", "QUALITY OVERVIEW");

        string[] val = { "98.4%", "1.6%", "12 批", "¥8.6万" };
        string[] key = { "批次合格率", "不合格率", "返工数量", "报废成本" };
        Color[] col = { DashUI.GREEN, DashUI.ORANGE, DashUI.CYAN, DashUI.RED };
        for (int i = 0; i < 4; i++)
        {
            float x = 12 + i * 134f, y = -46f;
            DashUI.Img(p, "kpiBg", x, y, 131, 48, DashUI.CARD, null);
            DashUI.Img(p, "kpiBar", x, y, 3, 48, col[i], null);
            DashUI.Txt(p, "kpiV", x + 11, y + 7, 115, 24, val[i], 20, col[i], TextAnchor.MiddleLeft, true);
            DashUI.Txt(p, "kpiK", x + 11, y + 31, 115, 13, key[i], 11, DashUI.SUB);
        }

        float R = 46f, cx = 148f, cy = -146f;
        float tot = 1286f;
        float[] cnt = { 1265f, 12f, 4f, 5f };
        Color[] rc = { DashUI.GREEN, DashUI.CYAN, DashUI.ORANGE, DashUI.PURPLE };
        float acc = 0f;
        for (int i = 0; i < 4; i++)
        {
            float frac = cnt[i] / tot;
            var rt = DashUI.RingFill(p, "ring", cx, cy, R, rc[i], frac);
            rt.localRotation = Quaternion.Euler(0, 0, -360f * acc);
            acc += frac;
        }
        DashUI.Txt(p, "cBig", cx - 32, cy + 1, 64, 26, "98.4%", 18, DashUI.GREEN, TextAnchor.MiddleCenter, true);
        DashUI.Txt(p, "cSub", cx - 46, cy - 18, 92, 13, "批次合格率", 11, DashUI.SUB, TextAnchor.MiddleCenter);
        DashUI.Txt(p, "cFoot", cx - 70, cy - R - 14, 140, 12, "本月检验 1,286 批", 11, DashUI.SUB, TextAnchor.MiddleCenter);

        string[] ln = { "合格放行", "返工", "报废", "待判定" };
        string[] lv = { "1265 批", "12 批", "4 批", "5 批" };
        for (int i = 0; i < 4; i++)
        {
            float y = -96 - i * 24f;
            DashUI.Dot(p, 240, y - 8, 4, rc[i]);
            DashUI.Txt(p, "lgN", 252, y, 96, 15, ln[i], 12, DashUI.TC);
            DashUI.Txt(p, "lgV", 352, y, 92, 15, lv[i], 12, rc[i], TextAnchor.MiddleLeft, true);
            DashUI.Img(p, "lgL", 232, y + 16, 226, 1, DashUI.GRID, null);
        }
    }

    static void NcReport(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "不合格样品报告", "NC REPORT");

        DashUI.Txt(p, "h1", 12, -46, 110, 16, "批次", 11, DashUI.SUB);
        DashUI.Txt(p, "h2", 126, -46, 160, 16, "现象", 11, DashUI.SUB);
        DashUI.Txt(p, "h3", 290, -46, 170, 16, "原因", 11, DashUI.SUB);
        DashUI.Txt(p, "h4", 470, -46, 72, 16, "处理", 11, DashUI.SUB, TextAnchor.MiddleCenter);
        DashUI.Img(p, "hln", 12, -64, W - 24, 1, new Color(1, 1, 1, 0.08f), null);

        float rowH = (H - 78) / 8f;
        for (int i = 0; i < 8; i++)
        {
            float y = -76 - i * rowH;
            Color tc = TagColor(NcAct[i]);
            DashUI.Txt(p, "d", 12, y, 110, 18, NcBatch[i], 13, DashUI.TC);
            DashUI.Txt(p, "p", 126, y, 160, 18, NcPhen[i], 13, DashUI.TC);
            DashUI.Txt(p, "r", 290, y, 174, 18, NcWhy[i], 13, DashUI.SUB);
            DashUI.Dot(p, 474, y - 9, 3, tc);
            DashUI.Txt(p, "s", 484, y, 66, 18, NcAct[i], 12, tc, TextAnchor.MiddleLeft, true);
            DashUI.Img(p, "ln", 12, y - rowH + 8, W - 24, 1, new Color(1, 1, 1, 0.045f), null);
        }
    }

    static Color TagColor(string s)
    {
        if (s == "返工") return DashUI.CYAN;
        if (s == "报废") return DashUI.ORANGE;
        if (s == "复检") return DashUI.PURPLE;
        return DashUI.GREEN;
    }

    static void Pareto(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "缺陷类型帕累托", "PARETO");

        float x0 = 46, xr = W - 44, pw = xr - x0;
        float yt = -82, yb = -(H - 38), ph = yt - yb;
        DashUI.Img(p, "yA", x0 - 8, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);
        DashUI.Img(p, "yB", xr + 8, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);

        for (int g = 0; g <= 40; g += 10)
        {
            float gy = yb + ph * g / 40f;
            if (g > 0) DashUI.Img(p, "gL" + g, x0, gy, pw, 1, DashUI.GRID, null);
            DashUI.Txt(p, "lL" + g, 0, gy - 7, 36, 14, g.ToString(), 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }
        for (int k = 0; k <= 4; k++)
        {
            int pv = k * 25;
            float gy = yb + ph * pv / 100f;
            DashUI.Txt(p, "lR" + pv, xr + 12, gy - 7, 40, 14, pv + "%", 11, DashUI.SUB);
        }

        string[] cat = { "含量不均", "外观瑕疵", "包装密封", "装量差异", "微生物", "其他" };
        int[] num = { 32, 24, 18, 12, 8, 6 };
        int[] cum = { 32, 56, 74, 86, 94, 100 };
        float slot = pw / 6f;
        for (int i = 0; i < 6; i++)
        {
            float bh = ph * num[i] / 40f;
            float cxb = x0 + i * slot + slot / 2f;
            DashUI.Img(p, "bar" + i, cxb, yb + bh, 26, bh, Color.white, DashUI.BarO, 0.5f, 1f, 0f);
            DashUI.Txt(p, "v" + i, cxb - 21, yb + bh - 18, 42, 13, num[i].ToString(), 11, DashUI.ORANGE, TextAnchor.MiddleCenter, true);
            DashUI.Txt(p, "c" + i, cxb - 34, yb + 6, 68, 13, cat[i], 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }
        for (int i = 0; i < 5; i++)
        {
            float xa = x0 + i * slot + slot / 2f, xb = xa + slot;
            DashUI.Seg(p, xa, yb + ph * cum[i] / 100f, xb, yb + ph * cum[i + 1] / 100f, 2, DashUI.CYAN);
        }
        for (int i = 0; i < 6; i++)
            DashUI.Dot(p, x0 + i * slot + slot / 2f, yb + ph * cum[i] / 100f, 2.6f, DashUI.CYAN);
    }

    static void Workshop(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "各车间合格率对比", "BY WORKSHOP");

        float x0 = 46, xr = W - 24, pw = xr - x0;
        float yt = -76, yb = -(H - 46), ph = yt - yb;
        DashUI.Img(p, "yA", x0 - 8, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);

        for (int g = 96; g <= 99; g++)
        {
            float gy = yb + ph * (g - 95) / 5f;
            DashUI.Img(p, "g" + g, x0, gy, pw, 1, DashUI.GRID, null);
            DashUI.Txt(p, "gl" + g, 0, gy - 7, 36, 14, g.ToString(), 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }

        string[] nm = { "无菌一", "无菌二", "固体制剂", "口服液", "提取车间", "包装车间" };
        float[] rt = { 99.2f, 98.6f, 97.8f, 98.9f, 96.5f, 99.5f };
        float slot = pw / 6f;
        for (int i = 0; i < 6; i++)
        {
            bool low = rt[i] < 98f;
            float bh = ph * (rt[i] - 95f) / 5f;
            float cxb = x0 + i * slot + slot / 2f;
            DashUI.Img(p, "bar" + i, cxb, yb + bh, 30, bh, Color.white, low ? DashUI.BarO : DashUI.BarG, 0.5f, 1f, 0f);
            DashUI.Txt(p, "v" + i, cxb - 21, yb + bh - 18, 42, 13, rt[i].ToString("0.0"), 11, low ? DashUI.ORANGE : DashUI.GREEN, TextAnchor.MiddleCenter, true);
            DashUI.Txt(p, "x" + i, cxb - 36, yb + 6, 72, 13, nm[i], 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }

        float ty = yb + ph * (98f - 95f) / 5f;
        DashUI.SegDash(p, x0, ty, xr, ty, 1.5f, DashUI.CYAN, 20, 14);
        DashUI.Txt(p, "tLbl", xr - 72, ty - 16, 66, 13, "目标98%", 11, DashUI.CYAN, TextAnchor.MiddleCenter);
    }

    static void Trend(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "批次合格率月度趋势", "PASS RATE TREND");

        float x0 = 46, x1 = W - 26;
        float yt = -80, yb = -(H - 42), ph = yt - yb;
        DashUI.Img(p, "yA", x0 - 8, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);
        DashUI.Img(p, "xA", x0, yb, x1 - x0, 1, new Color(1, 1, 1, 0.12f), null);

        float[] gv = { 97f, 98f, 99f, 99.5f };
        foreach (float g in gv)
        {
            float gy = yb + ph * (g - 97f) / 2.5f;
            if (g > 97.001f) DashUI.Img(p, "g" + g, x0, gy, x1 - x0, 1, DashUI.GRID, null);
            DashUI.Txt(p, "gl" + g, 0, gy - 7, 38, 14, g.ToString("0.#") + "%", 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }

        DashUI.Img(p, "lgG", 430, -32, 10, 10, DashUI.GREEN, null);
        DashUI.Txt(p, "lgtG", 444, -33, 74, 14, "实际合格率", 11, DashUI.SUB);
        DashUI.Img(p, "lgO", 522, -32, 10, 10, DashUI.ORANGE, null);
        DashUI.Txt(p, "lgtO", 536, -33, 66, 14, "目标98%", 11, DashUI.SUB);

        string[] mon = { "5月", "6月", "7月", "8月", "9月", "10月" };
        float[] pl = { 97.9f, 98.1f, 98.3f, 98.2f, 98.6f, 98.4f };
        float sx = (x1 - x0) / 5f;
        for (int i = 0; i < 5; i++)
            DashUI.Seg(p, x0 + i * sx, yb + ph * (pl[i] - 97f) / 2.5f, x0 + (i + 1) * sx, yb + ph * (pl[i + 1] - 97f) / 2.5f, 2, DashUI.GREEN);
        for (int i = 0; i < 6; i++)
        {
            DashUI.Dot(p, x0 + i * sx, yb + ph * (pl[i] - 97f) / 2.5f, 3, DashUI.GREEN);
            DashUI.Txt(p, "m" + i, x0 + i * sx - 22, yb + 6, 44, 13, mon[i], 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }

        float ty = yb + ph * (98f - 97f) / 2.5f;
        DashUI.SegDash(p, x0, ty, x1, ty, 1.5f, DashUI.ORANGE, 20, 14);
        DashUI.Txt(p, "tLbl", x1 - 64, ty - 16, 60, 13, "目标98%", 11, DashUI.ORANGE, TextAnchor.MiddleCenter);
    }

    static void Rework(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "返工报废与成本", "REWORK & SCRAP");

        float x0 = 46, x1 = W - 46, pw = x1 - x0;
        float yt = -84, yb = -(H - 46), ph = yt - yb;
        DashUI.Img(p, "yA", x0 - 8, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);
        DashUI.Img(p, "yB", x1 + 8, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);
        DashUI.Img(p, "xA", x0, yb, pw, 1, new Color(1, 1, 1, 0.12f), null);

        int[] gv = { 5, 10, 15, 20 };
        foreach (int g in gv)
        {
            float gy = yb + ph * g / 20f;
            if (g != 20) DashUI.Img(p, "g" + g, x0, gy, pw, 1, DashUI.GRID, null);
            DashUI.Txt(p, "gl" + g, 0, gy - 7, 38, 14, g.ToString(), 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }
        int[] rv = { 0, 7, 14 };
        foreach (int v in rv)
        {
            float gy = yb + ph * v / 14f;
            DashUI.Txt(p, "gr" + v, x1 + 12, gy - 7, 42, 14, v.ToString(), 11, DashUI.SUB);
        }

        DashUI.Img(p, "lg1", 300, -32, 10, 10, DashUI.CYAN, null);
        DashUI.Txt(p, "lgt1", 314, -33, 70, 14, "返工批数", 11, DashUI.SUB);
        DashUI.Img(p, "lg2", 392, -32, 10, 10, DashUI.ORANGE, null);
        DashUI.Txt(p, "lgt2", 406, -33, 70, 14, "报废批数", 11, DashUI.SUB);
        DashUI.Img(p, "lg3", 484, -32, 10, 10, DashUI.RED, null);
        DashUI.Txt(p, "lgt3", 498, -33, 70, 14, "报废成本", 11, DashUI.SUB);

        string[] mon = { "5月", "6月", "7月", "8月", "9月", "10月" };
        float[] rw = { 15, 11, 14, 10, 9, 12 };
        float[] sc = { 6, 5, 3, 4, 2, 4 };
        float[] ct = { 11.2f, 9.8f, 7.4f, 8.1f, 5.6f, 8.6f };
        float slot = pw / 6f;
        for (int i = 0; i < 6; i++)
        {
            float cxg = x0 + i * slot + slot / 2f;
            float bh1 = ph * rw[i] / 20f, bh2 = ph * sc[i] / 20f;
            DashUI.Img(p, "rw" + i, cxg - 6, yb + bh1, 10, bh1, Color.white, DashUI.BarV, 0.5f, 1f, 0f);
            DashUI.Img(p, "sc" + i, cxg + 6, yb + bh2, 10, bh2, Color.white, DashUI.BarO, 0.5f, 1f, 0f);
            DashUI.Txt(p, "m" + i, cxg - 24, yb + 6, 48, 13, mon[i], 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }
        for (int i = 0; i < 5; i++)
        {
            float xa = x0 + i * slot + slot / 2f, xb = xa + slot;
            DashUI.Seg(p, xa, yb + ph * ct[i] / 14f, xb, yb + ph * ct[i + 1] / 14f, 2, DashUI.RED);
        }
        for (int i = 0; i < 6; i++)
            DashUI.Dot(p, x0 + i * slot + slot / 2f, yb + ph * ct[i] / 14f, 3, DashUI.RED);
    }
}
