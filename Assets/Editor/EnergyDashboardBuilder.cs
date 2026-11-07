using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Canvas main5（安环能耗）图表构建器：清空容器后按当前布局重建。</summary>
public static class EnergyDashboardBuilder
{
    [MenuItem("大屏/重建 main5 安环能耗图表")]
    public static void Build()
    {
        var main = DashUI.FindRoot("Canvas main5");
        if (main == null) { Debug.LogError("场景中找不到 Canvas main5"); return; }
        var left = DashUI.FindRoot("Canvas left5");
        var right = DashUI.FindRoot("Canvas right5");
        if (left == null || right == null) { Debug.LogError("找不到 Canvas left5 / Canvas right5"); return; }

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
        Top6(slots[1]);
        LoadCurve(slots[2]);
        Emission(slots[3]);
        HeatMap(slots[4]);
        Funnel(slots[5]);
        Debug.Log("main5 安环能耗图表重建完成");
    }

    static void Overview(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "能耗总览", "ENERGY OVERVIEW");

        string[] nm = { "电 kWh", "水 t", "气 m3" };
        string[] val = { "4,860", "320", "1,850" };
        int[] pct = { 95, 91, 92 };
        Color[] col = { DashUI.CYAN, DashUI.GREEN, DashUI.ORANGE };

        float R = 42f, cy = -126f;
        for (int i = 0; i < 3; i++)
        {
            float cx = 101 + i * 179f;
            DashUI.RingFill(p, "gBg", cx, cy, R, new Color(1, 1, 1, 0.08f), 1f);
            DashUI.RingFill(p, "gFg", cx, cy, R, col[i], pct[i] / 100f);
            DashUI.Txt(p, "gV", cx - 46, cy - 14, 92, 28, val[i], 20, col[i], TextAnchor.MiddleCenter, true);
            DashUI.Txt(p, "gN", cx - 56, cy - R - 18, 112, 14, nm[i], 11, DashUI.SUB, TextAnchor.MiddleCenter);
            DashUI.Txt(p, "gR", cx - 56, cy - R - 34, 112, 14, "占日均 " + pct[i] + "%", 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }
    }

    static void Top6(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "重点设备能耗 TOP6", "TOP CONSUMERS");

        string[] dn = { "灭菌柜", "冻干机", "空调净化机组", "制粒线", "空压站", "纯化水制备" };
        int[] dv = { 720, 640, 480, 390, 310, 260 };
        float rowH = (H - 56) / 6f, bmax = W - 124 - 92;
        for (int i = 0; i < 6; i++)
        {
            float cy = -56 - i * rowH - rowH / 2f;
            DashUI.Txt(p, "n", 12, cy - 10, 104, 20, dn[i], 12, DashUI.TC);
            float bw = bmax * dv[i] / 720f;
            DashUI.Img(p, "bar", 124, cy - 6, bw, 12, Color.white, DashUI.BarH);
            DashUI.Txt(p, "v", 124 + bw + 8, cy - 10, 78, 20, dv[i] + " kWh", 11, DashUI.CYAN);
        }
    }

    static void LoadCurve(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "24h 电负荷曲线", "LOAD CURVE");

        float x0 = 46, x1 = W - 20, pw = x1 - x0;
        float yt = -70, yb = -(H - 42), ph = yt - yb;
        DashUI.Img(p, "yA", x0 - 8, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);
        DashUI.Img(p, "xA", x0, yb, pw, 1, new Color(1, 1, 1, 0.12f), null);

        for (int g = 0; g <= 400; g += 100)
        {
            float gy = yb + ph * g / 400f;
            if (g > 0) DashUI.Img(p, "g" + g, x0, gy, pw, 1, DashUI.GRID, null);
            DashUI.Txt(p, "gl" + g, 0, gy - 8, 38, 16, g.ToString(), 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }

        float step = pw / 23f;
        DashUI.Img(p, "pkBg", x0 + 8 * step, yt, 10 * step, ph, new Color(0.259f, 0.871f, 0.620f, 0.05f), null);
        DashUI.Txt(p, "pkL", x0 + 8 * step + 6, yt + 4, 90, 15, "生产高峰", 11, DashUI.GREEN);

        DashUI.Img(p, "lgA", 356, -32, 12, 8, DashUI.CYAN, null);
        DashUI.Txt(p, "lgAt", 372, -35, 66, 16, "今日负荷", 11, DashUI.SUB);
        DashUI.Img(p, "lgB", 446, -32, 12, 8, new Color(0.486f, 0.576f, 0.647f, 1f), null);
        DashUI.Txt(p, "lgBt", 462, -35, 66, 16, "昨日同期", 11, DashUI.SUB);

        int[] ld = { 120, 110, 105, 100, 100, 115, 180, 260, 310, 335, 340, 320, 300, 330, 345, 338, 320, 290, 260, 230, 190, 160, 140, 125 };
        int[] ly = { 118, 112, 108, 102, 103, 118, 175, 255, 300, 322, 330, 310, 295, 318, 336, 330, 310, 282, 255, 224, 186, 158, 138, 122 };
        for (int i = 0; i < 23; i++)
        {
            DashUI.Seg(p, x0 + i * step, yb + ph * ld[i] / 400f, x0 + (i + 1) * step, yb + ph * ld[i + 1] / 400f, 2, DashUI.CYAN);
            DashUI.Seg(p, x0 + i * step, yb + ph * ly[i] / 400f, x0 + (i + 1) * step, yb + ph * ly[i + 1] / 400f, 1.5f, new Color(0.486f, 0.576f, 0.647f, 0.8f));
        }
        for (int h = 0; h <= 23; h += 4)
            DashUI.Txt(p, "h" + h, x0 + h * step - 22, yb + 6, 44, 16, h + "时", 11, DashUI.SUB, TextAnchor.MiddleCenter);
    }

    static void Emission(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "废水废气排放监测", "EMISSION");

        string[] nm = { "COD mg/L", "氨氮 mg/L", "VOCs mg/m3", "粉尘 mg/m3" };
        string[] v1 = { "42", "6.8", "48", "3.2" };
        string[] v2 = { "80", "15", "60", "10" };
        float[] pct = { 52.5f, 45.3f, 80f, 32f };

        float ex = 118, ew = W - ex - 104;
        float yt = -58, yb = -(H - 34), ph = yt - yb;
        DashUI.Img(p, "yA", ex - 6, yt, 1, ph, new Color(1, 1, 1, 0.14f), null);

        for (int g = 0; g <= 100; g += 20)
        {
            float gx = ex + ew * g / 100f;
            if (g > 0) DashUI.Img(p, "g" + g, gx, yt, 1, ph, DashUI.GRID, null);
            DashUI.Txt(p, "gl" + g, gx - 20, yt - 24, 40, 16, g + "%", 11, DashUI.SUB, TextAnchor.MiddleCenter);
        }

        float rowH = (H - 64) / 4f;
        for (int i = 0; i < 4; i++)
        {
            float cy = -64 - i * rowH - rowH / 2f;
            bool warn = pct[i] >= 80f;
            DashUI.Txt(p, "n", 12, cy - 10, 100, 20, nm[i], 12, DashUI.TC);
            DashUI.Img(p, "bg", ex, cy - 7, ew, 14, new Color(1, 1, 1, 0.05f), DashUI.BarH);
            float bw = ew * pct[i] / 100f;
            DashUI.Img(p, "bar", ex, cy - 7, bw, 14, Color.white, DashUI.BarH);
            DashUI.Txt(p, "v", ex + bw + 10, cy - 10, 96, 20, v1[i] + " / " + v2[i], 11, warn ? DashUI.ORANGE : DashUI.CYAN);
        }
    }

    static void HeatMap(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "气体 / 粉尘监测热力", "SENSOR HEAT");

        string[] pts = { "提取车间", "污水站", "锅炉房", "粉尘间", "危废库" };
        int[] slots = { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22 };
        int[][] heat =
        {
            new[] { 22, 18, 15, 20, 35, 48, 52, 55, 50, 44, 38, 26 },
            new[] { 30, 26, 24, 28, 40, 52, 58, 60, 55, 48, 42, 34 },
            new[] { 35, 30, 28, 33, 55, 68, 78, 72, 64, 52, 45, 38 },
            new[] { 18, 15, 14, 16, 30, 42, 48, 46, 40, 32, 25, 20 },
            new[] { 12, 10, 10, 11, 15, 20, 24, 22, 18, 16, 14, 12 }
        };

        float hx = 104, hxe = W - 14, cw = (hxe - hx) / 12f;
        float hy = -52, hye = -(H - 20), chh = (hy - hye) / 5f;

        for (int j = 0; j < 12; j++)
            DashUI.Txt(p, "sx" + j, hx + j * cw - 4, hy + 6, 44, 16, slots[j] + "时", 10, DashUI.SUB, TextAnchor.MiddleCenter);

        for (int i = 0; i < 5; i++)
        {
            DashUI.Txt(p, "sy" + i, 6, hy - (i + 0.5f) * chh - 9, 94, 18, pts[i], 11, DashUI.TC, TextAnchor.MiddleCenter);
            for (int j = 0; j < 12; j++)
            {
                int v = heat[i][j];
                DashUI.Img(p, "cell" + i + "-" + j, hx + j * cw, hy - i * chh, cw - 3, chh - 3, Heat(v), null);
                DashUI.Txt(p, "cv" + i + "-" + j, hx + j * cw, hy - i * chh - (chh - 3) / 2f + 3, cw - 3, 18, v.ToString(), 10, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
            }
        }
    }

    static Color Heat(float v)
    {
        v = Mathf.Clamp(v, 0f, 100f);
        float q0, q1, q2;
        if (v < 38f)
        {
            float u = v / 38f;
            q0 = 10 + (20 - 10) * u; q1 = 42 + (144 - 42) * u; q2 = 58 + (216 - 58) * u;
        }
        else if (v < 70f)
        {
            float u = (v - 38f) / 32f;
            q0 = 20 + (60 - 20) * u; q1 = 144 + (217 - 144) * u; q2 = 216 + (255 - 216) * u;
        }
        else
        {
            float u = (v - 70f) / 30f;
            q0 = 60 + (255 - 60) * u; q1 = 217 + (180 - 217) * u; q2 = 255 + (84 - 255) * u;
        }
        return new Color(q0 / 255f, q1 / 255f, q2 / 255f, 1f);
    }

    static void Funnel(RectTransform p)
    {
        float W = p.sizeDelta.x, H = p.sizeDelta.y;
        DashUI.Title(p, "安全隐患整改漏斗", "HAZARD FUNNEL");

        string[] lbl = { "发现隐患 32", "已整改 26", "已验收 24" };
        int[] num = { 32, 26, 24 };
        Color[] col = { DashUI.ORANGE, DashUI.CYAN, DashUI.GREEN };

        float maxW = 390, fh = 40, gap = 10, cx = W / 2f;
        for (int i = 0; i < 3; i++)
        {
            float w = maxW * num[i] / 32f;
            float y = -56 - i * (fh + gap);
            DashUI.Img(p, "fn" + i, cx, y, w, fh, col[i], null, 0.5f, 1f, 0f);
            DashUI.Txt(p, "ft" + i, cx - w / 2f, y + 9, w, 24, lbl[i], 13, new Color(0.031f, 0.07f, 0.12f, 1f), TextAnchor.MiddleCenter, true);
            if (i > 0)
            {
                int ratio = Mathf.RoundToInt(num[i] / (float)num[i - 1] * 100f);
                DashUI.Txt(p, "fr" + i, cx + maxW / 2f + 12, y + 10, 60, 20, ratio + "%", 12, col[i], TextAnchor.MiddleLeft, true);
            }
        }
        DashUI.Txt(p, "note", 12, H - 26, 260, 16, "整改率 81% · 验收率 92%", 11, DashUI.SUB);
    }
}
