using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Daima;

/// <summary>
/// Canvas main2（设备管理）四块图表构建器：
///   左1 设备管理 · 状态与效率
///   左2 设备占用 · 故障报警
///   右1 设备健康 · 预测性维护
///   右2 点检保养 · 设备能耗
/// 只重建 Canvas left2 / Canvas right2 下 Image1、Image2 的子内容，并同步外层标题。
/// </summary>
public static class Main2EquipmentDashboardBuilder
{
    const float PAD = 14f;

    static readonly Color RED = new Color(1f, 0.392f, 0.439f, 1f);           // #FF6470
    static readonly Color GOOD = new Color(0.361f, 0.910f, 0.678f, 1f);      // #5CE8AD
    static readonly Color WARN = new Color(1f, 0.769f, 0.420f, 1f);          // #FFC46B
    static readonly Color BAD = new Color(1f, 0.545f, 0.584f, 1f);           // #FF8B95
    static readonly Color INFO = new Color(0.482f, 0.863f, 1f);              // #7BDCFF
    static readonly Color MUTE = new Color(0.541f, 0.655f, 0.722f, 1f);      // #8AA7B8
    static readonly Color CELL = new Color(0.561f, 0.680f, 0.753f, 1f);      // #8FADC0
    static readonly Color SUBT = new Color(0.486f, 0.576f, 0.647f, 1f);      // #7C93A5
    static readonly Color BATCH = new Color(0.812f, 0.933f, 0.984f, 1f);     // #CFEEFB
    static readonly Color TIME = new Color(0.624f, 0.765f, 0.831f, 1f);      // #9FC3D4
    static readonly Color HEAD = new Color(0.329f, 0.471f, 0.557f, 1f);      // #54788E
    static readonly Color SUBCN = new Color(0.373f, 0.506f, 0.588f, 1f);     // #5F8196
    static readonly Color SUBCN2 = new Color(0.322f, 0.475f, 0.561f, 1f);    // #52798F
    static readonly Color RADARLBL = new Color(0.490f, 0.620f, 0.700f, 1f);  // #7D9EB2
    static readonly Color LINE = new Color(0.298f, 0.627f, 0.804f, 0.18f);
    static readonly Color ROWLINE = new Color(1f, 1f, 1f, 0.05f);
    static readonly Color TRACK = new Color(1f, 1f, 1f, 0.06f);
    static readonly Color TILEBORDER = new Color(0.251f, 0.667f, 0.863f, 0.14f);
    static readonly Color RADARGRID = new Color(0.353f, 0.647f, 0.804f, 0.13f);
    static readonly Color RADARAXIS = new Color(0.353f, 0.647f, 0.804f, 0.18f);
    static readonly Color RADARTARGET = new Color(0.588f, 0.686f, 0.745f, 0.48f);
    static readonly Color AXIS = new Color(0.353f, 0.647f, 0.804f, 0.16f);
    static readonly Color AXISTXT = new Color(0.392f, 0.525f, 0.608f, 1f);  // #64869B

    static Sprite _energyBar;

    static Sprite EnergyBar
    {
        get
        {
            if (_energyBar == null) _energyBar = Resources.Load<Sprite>("bar_fill_grad");
            return _energyBar;
        }
    }

    [MenuItem("大屏/重建 main2 设备管理图表")]
    public static void Build()
    {
        RectTransform left = DashUI.FindRoot("Canvas left2");
        RectTransform right = DashUI.FindRoot("Canvas right2");
        if (left == null || right == null)
        {
            Debug.LogError("[main2] 找不到 Canvas left2 / Canvas right2");
            return;
        }

        RectTransform[] slots =
        {
            DashUI.Child(left, "Image1"),
            DashUI.Child(left, "Image2"),
            DashUI.Child(right, "Image1"),
            DashUI.Child(right, "Image2")
        };
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogError("[main2] 缺少图表容器，序号 " + i);
                return;
            }
            DashUI.Clear(slots[i]);   // 保留面板自身底色，不调用 ClearBg
            PanelBackdrop(slots[i]);
        }

        SetTitle(left, "Text1", "设备管理 · 状态与效率");
        SetTitle(left, "Text2", "设备占用 · 故障报警");
        SetTitle(right, "Text1", "设备健康 · 预测性维护");
        SetTitle(right, "Text2", "点检保养 · 设备能耗");

        StatusEfficiency(slots[0]);
        OccupancyAlarm(slots[1]);
        HealthPredict(slots[2]);
        MaintenanceEnergy(slots[3]);

        Scene scene = left.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[main2] 设备管理四块图表重建完成，场景已保存。");
    }

    static void SetTitle(RectTransform canvas, string childName, string text)
    {
        RectTransform rt = DashUI.Child(canvas, childName);
        if (rt == null) return;
        Text tx = rt.GetComponent<Text>();
        if (tx == null) return;
        Undo.RecordObject(tx, "更新 main2 标题");
        string before = tx.text;
        tx.text = text;
        EditorUtility.SetDirty(tx);
        Debug.Log("[main2] SetTitle " + canvas.name + "/" + childName + " before='" + before + "' after='" + tx.text + "'");
    }

    /// <summary>面板底色向 screen2.html 的深海军蓝卡片靠拢，提高文字可读性。</summary>
    static void PanelBackdrop(RectTransform slot)
    {
        Image bg = slot.GetComponent<Image>();
        if (bg == null) return;
        Undo.RecordObject(bg, "调整 main2 面板底色");
        bg.color = new Color(0.035f, 0.105f, 0.175f, 0.88f);
        EditorUtility.SetDirty(bg);
    }

    // ================================================================= 左1 状态与效率

    static void StatusEfficiency(RectTransform p)
    {
        float W = p.sizeDelta.x;
        float H = p.sizeDelta.y;
        float inner = W - PAD * 2f;
        float top = -12f;

        float gap = 7f;
        float tw = (inner - gap * 3f) / 4f;
        string[] val = { "24", "8", "3", "2" };
        string[] lab = { "运行", "待机", "故障", "检修" };
        Color[] col = { DashUI.GREEN, DashUI.CYAN, RED, DashUI.ORANGE };

        for (int i = 0; i < 4; i++)
        {
            float x = PAD + i * (tw + gap);
            DashUI.Img(p, "eqBg", x, top, tw, 48f, A(col[i], 0.05f), null);
            DashUI.Img(p, "eqBar", x, top, 2f, 48f, col[i], null);
            DashUI.Txt(p, "eqV", x + 11f, top - 2f, tw - 16f, 28f, val[i], 20,
                DashUI.TC, TextAnchor.MiddleLeft, true);
            float cy = top - 40f;
            DashUI.Dot(p, x + 14f, cy, 2f, col[i]);
            DashUI.Txt(p, "eqL", x + 22f, top - 33f, tw - 28f, 14f, lab[i], 9, SUBT);
        }

        float lineTop = top - 48f - 6f;
        DashUI.Txt(p, "eqLine", PAD, lineTop - 18f, inner - 110f, 18f,
            "设备占用 " + Strong("29 / 37") + " 台 · 占用率 " + Strong("78.4%") +
            " · 平均 OEE " + Strong("86.2%"), 9, SUBT);
        DashUI.Dot(p, W - PAD - 92f, lineTop - 9f, 2f, RED);
        DashUI.Txt(p, "warnChip", W - PAD - 86f, lineTop - 18f, 86f, 18f,
            "3 条报警待处理", 9, BAD);

        float bodyTop = lineTop - 18f - 3f;
        float footTop = -(H - 10f - 24f);
        float bodyH = bodyTop - footTop;
        float metricW = 188f;
        float radarW = inner - 10f - metricW;

        Radar(p, PAD, bodyTop, radarW, bodyH);
        Metrics(p, PAD + radarW + 10f, bodyTop, metricW, bodyH);

        Footer(p, footTop, W,
            "预测性维护 " + Strong("2") + " 项 · 备件齐套率 " + Strong("94.8%"),
            "数据刷新 " + Strong("5 s"));
    }

    static void Radar(RectTransform p, float x, float yTop, float w, float h)
    {
        float cx = x + w * 0.5f;
        float cy = yTop - h * 0.47f;
        float R = Mathf.Min(w * 0.36f, h * 0.30f);

        Vector2[] axis = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float rad = (90f + i * 60f) * Mathf.Deg2Rad;
            axis[i] = new Vector2(cx + R * Mathf.Cos(rad), cy + R * Mathf.Sin(rad));
        }

        // 网格
        for (int k = 1; k <= 4; k++)
        {
            float rr = R * k / 4f;
            for (int i = 0; i < 6; i++)
            {
                float a1 = (90f + i * 60f) * Mathf.Deg2Rad;
                float a2 = (90f + (i + 1) * 60f) * Mathf.Deg2Rad;
                float x1 = cx + rr * Mathf.Cos(a1), y1 = cy + rr * Mathf.Sin(a1);
                float x2 = cx + rr * Mathf.Cos(a2), y2 = cy + rr * Mathf.Sin(a2);
                DashUI.Seg(p, x1, y1, x2, y2, 1f, RADARGRID);
            }
        }
        for (int i = 0; i < 6; i++)
            DashUI.Seg(p, cx, cy, axis[i].x, axis[i].y, 1f, RADARAXIS);

        // 本月实际值面积填充
        float[] actual = { 86f, 92f, 88f, 94f, 95f, 81f };
        float[] target = { 90f, 90f, 90f, 95f, 95f, 85f };
        Vector2[] actPts = new Vector2[6];
        Vector2[] tgtPts = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float a = (90f + i * 60f) * Mathf.Deg2Rad;
            actPts[i] = new Vector2(cx + R * actual[i] / 100f * Mathf.Cos(a),
                                    cy + R * actual[i] / 100f * Mathf.Sin(a));
            tgtPts[i] = new Vector2(cx + R * target[i] / 100f * Mathf.Cos(a),
                                    cy + R * target[i] / 100f * Mathf.Sin(a));
        }

        var fillGo = DashUI.New("radarFill", p, x, yTop, w, h);
        var fill = Undo.AddComponent<UIRadarFillGraphic>(fillGo);
        fill.raycastTarget = false;
        var nPts = new List<Vector2>();
        for (int i = 0; i < 6; i++)
            nPts.Add(Norm(actPts[i], x, yTop, w, h));
        fill.SetData(nPts, Norm(new Vector2(cx, cy), x, yTop, w, h),
            A(DashUI.CYAN, 0.30f), A(DashUI.CYAN, 0.035f));

        // 目标虚线
        for (int i = 0; i < 6; i++)
        {
            Vector2 a = tgtPts[i], b = tgtPts[(i + 1) % 6];
            DashUI.SegDash(p, a.x, a.y, b.x, b.y, 1f, RADARTARGET, 5f, 4f);
        }

        // 实际线 + 节点
        for (int i = 0; i < 6; i++)
        {
            Vector2 a = actPts[i], b = actPts[(i + 1) % 6];
            DashUI.Seg(p, a.x, a.y, b.x, b.y, 1.6f, DashUI.CYAN);
        }
        for (int i = 0; i < 6; i++)
            DashUI.Dot(p, actPts[i].x, actPts[i].y, 1.9f, DashUI.CYAN);

        // 轴标签
        DashUI.Txt(p, "rL0", axis[0].x - 30f, axis[0].y + 18f, 60f, 12f, "利用率", 9, RADARLBL, TextAnchor.MiddleCenter);
        DashUI.Txt(p, "rL1", axis[1].x - 58f, axis[1].y + 6f, 50f, 12f, "稳定性", 9, RADARLBL, TextAnchor.MiddleRight);
        DashUI.Txt(p, "rL2", axis[2].x - 58f, axis[2].y + 6f, 50f, 12f, "响应", 9, RADARLBL, TextAnchor.MiddleRight);
        DashUI.Txt(p, "rL3", axis[3].x - 30f, axis[3].y - 18f, 60f, 12f, "维护", 9, RADARLBL, TextAnchor.MiddleCenter);
        DashUI.Txt(p, "rL4", axis[4].x + 8f, axis[4].y + 6f, 50f, 12f, "安全", 9, RADARLBL);
        DashUI.Txt(p, "rL5", axis[5].x + 8f, axis[5].y + 6f, 50f, 12f, "能效", 9, RADARLBL);

        // 图例
        float ly = yTop - h + 16f;
        float lx = x + w - 118f;
        DashUI.Img(p, "lgA", lx, ly, 8f, 8f, DashUI.CYAN, null);
        DashUI.Txt(p, "lgAT", lx + 12f, ly - 2f, 38f, 12f, "本月", 8, SUBT);
        DashUI.Img(p, "lgB", lx + 58f, ly, 8f, 8f, A(RADARTARGET, 0.75f), null);
        DashUI.Txt(p, "lgBT", lx + 70f, ly - 2f, 40f, 12f, "目标", 8, SUBT);
    }

    static void Metrics(RectTransform p, float x, float yTop, float w, float h)
    {
        string[] keys = { "综合效率 OEE", "设备利用率", "设备健康度", "平均无故障 MTBF", "平均故障响应" };
        string[] vals =
        {
            "86.2" + Unit("%"),
            "78.4" + Unit("%"),
            "91.6" + Unit("%"),
            "428" + Unit("h"),
            "12" + Unit("min")
        };
        float[] frac = { 0.862f, 0.784f, 0.916f, 0.71f, 0.88f };
        Color[] col = { DashUI.GREEN, DashUI.CYAN, DashUI.GREEN, DashUI.CYAN, DashUI.GREEN };
        float rh = h / 5f;

        for (int i = 0; i < 5; i++)
        {
            float rt = yTop - i * rh;
            if (i > 0) HLine(p, x, rt, w, ROWLINE);
            DashUI.Txt(p, "mK", x, rt - 17f, w - 72f, 14f, keys[i], 9, SUBT);
            DashUI.Txt(p, "mV", x + w - 72f, rt - 19f, 72f, 22f, vals[i], 15,
                DashUI.TC, TextAnchor.MiddleRight, true);
            DashUI.Img(p, "mTrack", x, rt - 44f, w, 3f, TRACK, null);
            DashUI.Img(p, "mFill", x, rt - 44f, Mathf.Max(6f, w * frac[i]), 3f, col[i], null);
        }
    }

    // ================================================================= 左2 占用与报警

    static void OccupancyAlarm(RectTransform p)
    {
        float W = p.sizeDelta.x;
        float H = p.sizeDelta.y;
        float inner = W - PAD * 2f;
        float top = -10f;

        DashUI.Txt(p, "occSum", PAD, top - 18f, inner - 160f, 18f,
            "占用 " + Strong("29 / 37") + " 台 · 空闲 " + Strong("8") + " 台 · 故障 " + Strong("3") +
            " 台 · 检修 " + Strong("2") + " 台", 9, SUBT);
        DashUI.Txt(p, "occSumR", W - PAD - 150f, top - 18f, 150f, 18f,
            "平均占用时长 " + Strong("6.4 h"), 9, SUBT, TextAnchor.MiddleRight);

        top -= 20f;
        SubTitle(p, top, W, "设备占用 · OCCUPANCY", "批次 / 起止时间");
        top -= 17f;

        float[] xs = { 0f, 132f, 188f, 286f, 434f };
        float[] ws = { 132f, 56f, 98f, 148f, 105f };
        string[] head = { "设备编号 / 名称", "占用", "批次", "起止时间", "状态" };
        top = TableHeader(p, top, W, head, xs, ws);

        string[] code = { "CNC-03", "SMT-02", "ASM-05", "CNC-01", "GRD-04", "INS-08" };
        string[] name = { "数控加工中心", "高速贴片机", "柔性装配线", "数控车床", "精密磨床", "三坐标检测台" };
        string[] batch = { "B-1106-01", "B-1106-02", "B-1106-03", "B-1105-07", "B-1105-08", "—" };
        string[] time = { "08:00-10:00", "10:00-12:00", "12:30-14:30", "14:30-16:30", "16:30-18:00", "—" };
        string[] stat = { "运行中", "待生产", "运行中", "运行中", "检修", "待机" };
        Color[] sc = { GOOD, INFO, GOOD, GOOD, WARN, MUTE };
        bool[] occupied = { true, true, true, true, true, false };

        float rowH = 28f;
        for (int i = 0; i < 6; i++)
        {
            float rt = top - i * rowH;
            DevCell(p, PAD + xs[0], rt, code[i], name[i]);
            float cy = rt - rowH * 0.5f;
            DashUI.Dot(p, PAD + xs[1] + 3f, cy, 2f, occupied[i] ? DashUI.GREEN : MUTE);
            DashUI.Txt(p, "occY", PAD + xs[1] + 10f, rt, ws[1] - 12f, rowH,
                occupied[i] ? "是" : "否", 9, occupied[i] ? GOOD : MUTE);
            DashUI.Txt(p, "occB", PAD + xs[2], rt, ws[2] - 4f, rowH, batch[i], 9,
                i == 5 ? SUBCN2 : BATCH);
            DashUI.Txt(p, "occT", PAD + xs[3], rt, ws[3] - 2f, rowH, time[i], 9,
                i == 5 ? SUBCN2 : TIME);
            Pill(p, PAD + xs[4] + 2f, rt, rowH, 62f, stat[i], sc[i]);
            HLine(p, PAD, rt - rowH, inner, ROWLINE);
        }

        top -= 6 * rowH;
        top -= 3f;
        SubTitle(p, top, W, "故障报警 · ALARM", "3 待处理 / 1 已完成");
        top -= 17f;

        float[] axs = { 0f, 84f, 300f, 362f, 424f };
        float[] aws = { 84f, 216f, 62f, 62f, 115f };
        string[] ahead = { "设备", "故障原因", "发生", "持续", "状态" };
        top = TableHeader(p, top, W, ahead, axs, aws);

        string[] acode = { "CNC-02", "HYD-04", "WLD-05", "AGV-07" };
        string[] area = { "主轴温度过高 · 78.6°C", "液压油位偏低 · 18.2%", "焊接电流波动 · ±8.4%", "电池 SOC 低于 15% · 已换电" };
        string[] atime = { "15:04", "14:38", "13:52", "12:26" };
        string[] dur = { "42 min", "1h 08m", "1h 54m", "已完成" };
        string[] astat = { "处理中", "待处理", "处理中", "已完成" };
        Color[] asc = { WARN, BAD, WARN, GOOD };

        for (int i = 0; i < 4; i++)
        {
            float rt = top - i * rowH;
            DashUI.Txt(p, "aCode", PAD + axs[0], rt, aws[0] - 4f, rowH, acode[i], 9, BATCH);
            DashUI.Txt(p, "aWhy", PAD + axs[1], rt, aws[1] - 4f, rowH, area[i], 9, CELL);
            DashUI.Txt(p, "aTime", PAD + axs[2], rt, aws[2] - 2f, rowH, atime[i], 9, TIME);
            DashUI.Txt(p, "aDur", PAD + axs[3], rt, aws[3] - 2f, rowH, dur[i], 9, TIME);
            Pill(p, PAD + axs[4] + 2f, rt, rowH, 62f, astat[i], asc[i]);
            HLine(p, PAD, rt - rowH, inner, ROWLINE);
        }

        Footer(p, -(H - 10f - 20f), W,
            "今日报警 " + Strong("12") + " · 已完成 " + Strong("8") + " · 平均响应 " + Strong("12 min"),
            "OEE 损失 " + Strong("1.8%"));
    }

    // ================================================================= 右1 健康与预测维护

    static void HealthPredict(RectTransform p)
    {
        float W = p.sizeDelta.x;
        float inner = W - PAD * 2f;
        float top = -10f;

        float gap = 7f;
        float tw = (inner - gap * 3f) / 4f;
        string[] lab = { "综合健康度", "高风险设备", "预测工单", "备件齐套率" };
        string[] val = { "91.6" + Unit("%"), "3" + Unit("台"), "7" + Unit("项"), "94.8" + Unit("%") };
        string[] tag = { "较上周 +0.8", "48h 内处理", "本周待排", "缺口 2 项" };
        Color[] col = { DashUI.GREEN, RED, DashUI.ORANGE, DashUI.CYAN };
        Color[] tagc = { GOOD, WARN, WARN, GOOD };

        for (int i = 0; i < 4; i++)
        {
            float x = PAD + i * (tw + gap);
            DashUI.Img(p, "tileBg", x, top, tw, 52f, A(col[i], 0.035f), null);
            Outline(p, x, top, tw, 52f, TILEBORDER);
            DashUI.Dot(p, x + 11f, top - 11f, 2f, col[i]);
            DashUI.Txt(p, "tileK", x + 17f, top - 6f, tw - 22f, 12f, lab[i], 9, SUBT);
            DashUI.Txt(p, "tileV", x + 8f, top - 17f, tw - 14f, 22f, val[i], 18,
                DashUI.TC, TextAnchor.MiddleLeft, true);
            DashUI.Txt(p, "tileTag", x + tw - 96f, top - 39f, 90f, 10f, tag[i], 8,
                tagc[i], TextAnchor.MiddleRight);
        }

        float trendTop = top - 52f - 8f;
        HealthTrend(p, trendTop, W, inner);

        float subTop = trendTop - 136f - 2f;
        SubTitle(p, subTop, W, "预测性维护 · PREDICTIVE MAINTENANCE", "未来 7 天");
        float headerTop = subTop - 17f;

        float[] xs = { 0f, 124f, 292f, 370f, 444f };
        float[] ws = { 124f, 168f, 78f, 74f, 95f };
        string[] head = { "设备", "预测异常", "剩余寿命", "建议", "风险" };
        float rowTop = TableHeader(p, headerTop, W, head, xs, ws);

        string[] code = { "CNC-02", "HYD-04", "SMT-02", "ASM-05", "GRD-04" };
        string[] name = { "数控加工中心", "液压站", "高速贴片机", "柔性装配线", "精密磨床" };
        string[] issue = { "主轴轴承磨损", "液压油清洁度下降", "吸嘴真空压力衰减", "伺服电机温升异常", "砂轮剩余寿命不足" };
        string[] life = { "68 h", "42 h", "96 h", "128 h", "36 h" };
        string[] due = { "11-08", "11-07", "11-10", "11-12", "11-07" };
        string[] risk = { "高", "高", "中", "中", "高" };
        Color[] rc = { BAD, BAD, WARN, WARN, BAD };

        float rowH = 28f;
        for (int i = 0; i < 5; i++)
        {
            float rt = rowTop - i * rowH;
            DevCell(p, PAD + xs[0], rt, code[i], name[i]);
            DashUI.Txt(p, "hIssue", PAD + xs[1], rt, ws[1] - 4f, rowH, issue[i], 9, CELL);
            DashUI.Txt(p, "hLife", PAD + xs[2], rt, ws[2] - 2f, rowH, life[i], 9, TIME);
            DashUI.Txt(p, "hDue", PAD + xs[3], rt, ws[3] - 2f, rowH, due[i], 9, TIME);
            Pill(p, PAD + xs[4] + 2f, rt, rowH, 56f, risk[i], rc[i]);
            HLine(p, PAD, rt - rowH, inner, ROWLINE);
        }

        Footer(p, -(p.sizeDelta.y - 10f - 24f), W,
            "健康模型覆盖率 " + Strong("92.4%") + " · 本月避免停机 " + Strong("18.6 h"),
            "预测准确率 " + Strong("91.2%"));
    }

    static void HealthTrend(RectTransform p, float top, float W, float inner)
    {
        DashUI.Txt(p, "trendTitle", PAD, top - 12f, 220f, 12f, "近 7 日综合健康度 · %", 9, SUBCN);

        float x0 = PAD + 12f;
        float x1 = W - PAD - 38f;
        float chartTop = top - 22f;
        float chartBottom = top - 136f + 17f;
        float chartH = chartTop - chartBottom;
        float chartW = x1 - x0;

        string[] dates = { "10-31", "11-01", "11-02", "11-03", "11-04", "11-05", "11-06" };
        float[] values = { 90.2f, 90.8f, 90.5f, 91.1f, 90.9f, 91.4f, 91.6f };

        var pxPts = new List<Vector2>();
        for (int i = 0; i < values.Length; i++)
        {
            float px = x0 + chartW * i / (values.Length - 1f);
            float py = chartTop - (values[i] - 88f) / 6f * chartH;
            pxPts.Add(new Vector2(px, py));
        }
        List<Vector2> smooth = Smooth(pxPts, 10);

        var areaGo = DashUI.New("trendArea", p, x0, chartTop, chartW, chartH);
        var area = Undo.AddComponent<UIAreaGraphic>(areaGo);
        area.raycastTarget = false;
        var areaN = new List<Vector2>();
        for (int i = 0; i < smooth.Count; i++) areaN.Add(Norm(smooth[i], x0, chartTop, chartW, chartH));
        area.SetData(areaN, A(DashUI.GREEN, 0.26f), A(DashUI.GREEN, 0.01f));

        // 曲线光晕（粗线打底，再叠细线）
        for (int i = 0; i < smooth.Count - 1; i++)
            DashUI.Seg(p, smooth[i].x, smooth[i].y, smooth[i + 1].x, smooth[i + 1].y,
                5f, A(DashUI.GREEN, 0.08f));
        for (int i = 0; i < smooth.Count - 1; i++)
            DashUI.Seg(p, smooth[i].x, smooth[i].y, smooth[i + 1].x, smooth[i + 1].y,
                1.8f, DashUI.GREEN);

        // 92% 目标线
        float ty = chartTop - (92f - 88f) / 6f * chartH;
        DashUI.SegDash(p, x0, ty, x1, ty, 1f, A(DashUI.ORANGE, 0.55f), 5f, 4f);
        DashUI.Txt(p, "targetLbl", x1 - 70f, ty + 13f, 70f, 12f, "目标 92", 8,
            WARN, TextAnchor.MiddleRight);

        // 横轴
        DashedBottom(p, x0, x1, chartBottom);
        for (int i = 0; i < dates.Length; i++)
        {
            float px = x0 + chartW * i / (dates.Length - 1f);
            float lx = Mathf.Clamp(px - 20f, PAD, W - PAD - 40f);
            DashUI.Txt(p, "xLbl", lx, chartBottom - 13f, 40f, 12f, dates[i], 8,
                AXISTXT, TextAnchor.MiddleCenter);
        }

        // 末点数值
        Vector2 last = pxPts[pxPts.Count - 1];
        DashUI.Txt(p, "endLbl", x1 + 4f, last.y - 7f, 34f, 14f, "91.6%", 9,
            DashUI.GREEN, TextAnchor.MiddleLeft, true);
    }

    // ================================================================= 右2 点检与能耗

    static void MaintenanceEnergy(RectTransform p)
    {
        float W = p.sizeDelta.x;
        float H = p.sizeDelta.y;
        float inner = W - PAD * 2f;
        float top = -10f;

        DashUI.Img(p, "stripBg", PAD, top, inner, 34f, A(DashUI.CYAN, 0.025f), null);
        Outline(p, PAD, top, inner, 34f, A(DashUI.CYAN, 0.09f));
        string[] strip =
        {
            "今日点检 " + Lit("26", 12, new Color(0.749f, 0.914f, 0.973f)) + " 项",
            "已完成 " + Lit("23", 12, new Color(0.749f, 0.914f, 0.973f)) + " 项",
            "保养到期 " + Lit("3", 12, WARN) + " 台",
            "点检异常 " + Lit("2", 12, BAD) + " 项"
        };
        float cellW = inner / 4f;
        for (int i = 0; i < 4; i++)
        {
            DashUI.Txt(p, "strip", PAD + i * cellW, top, cellW, 34f, strip[i], 9,
                SUBT, TextAnchor.MiddleCenter);
        }

        top -= 38f;
        SubTitle(p, top, W, "点检保养计划 · MAINTENANCE PLAN", "6 项 / 今日");
        top -= 17f;

        float[] xs = { 0f, 124f, 292f, 360f, 428f };
        float[] ws = { 124f, 168f, 68f, 68f, 111f };
        string[] head = { "设备", "点检 / 保养项目", "计划", "负责人", "状态" };
        float rowTop = TableHeader(p, top, W, head, xs, ws);

        string[] code = { "CNC-03", "SMT-02", "ASM-05", "HYD-04", "GRD-04", "WLD-05" };
        string[] item = { "主轴润滑检查", "吸嘴清洁校准", "皮带张力点检", "液压油更换", "砂轮更换保养", "焊枪喷嘴清理" };
        string[] plan = { "08:30", "09:00", "10:30", "13:00", "15:30", "16:00" };
        string[] owner = { "张伟", "李娜", "王强", "赵磊", "陈晨", "周敏" };
        string[] stat = { "已完成", "已完成", "进行中", "待执行", "待执行", "待执行" };
        Color[] sc = { GOOD, GOOD, INFO, WARN, WARN, MUTE };

        float rowH = 26f;
        for (int i = 0; i < 6; i++)
        {
            float rt = rowTop - i * rowH;
            DashUI.Txt(p, "mCode", PAD + xs[0], rt, ws[0] - 4f, rowH, code[i], 9, BATCH);
            DashUI.Txt(p, "mItem", PAD + xs[1], rt, ws[1] - 4f, rowH, item[i], 9, CELL);
            DashUI.Txt(p, "mPlan", PAD + xs[2], rt, ws[2] - 2f, rowH, plan[i], 9, TIME);
            DashUI.Txt(p, "mOwner", PAD + xs[3], rt, ws[3] - 2f, rowH, owner[i], 9, CELL);
            Pill(p, PAD + xs[4] + 2f, rt, rowH, 58f, stat[i], sc[i]);
            HLine(p, PAD, rt - rowH, inner, ROWLINE);
        }

        float energyTop = rowTop - 6f * rowH;
        EnergyRank(p, energyTop, W, inner);

        Footer(p, -(H - 10f - 20f), W,
            "点检完成率 " + Strong("88.5%") + " · 保养准时率 " + Strong("96.2%"),
            "今日设备能耗 " + Strong("1,284 kWh"));
    }

    static void EnergyRank(RectTransform p, float top, float W, float inner)
    {
        top -= 2f;
        DashUI.Txt(p, "energyTitle", PAD, top - 12f, 220f, 12f, "设备能耗 TOP5 · kWh", 9, SUBCN);

        string[] code = { "CNC-03", "SMT-02", "ASM-05", "WLD-05", "HYD-04" };
        int[] value = { 286, 242, 208, 176, 154 };
        float rowH = 23f;
        float labelW = 48f;
        float trackX = PAD + 50f;
        float trackW = inner - 50f - 46f;
        float barTop = top - 20f;

        for (int i = 0; i < 5; i++)
        {
            float rt = barTop - i * rowH;
            DashUI.Txt(p, "eCode", PAD, rt, labelW, rowH, code[i], 9, new Color(0.514f, 0.639f, 0.718f));
            DashUI.Img(p, "eTrack", trackX, rt - 7f, trackW, 9f, A(DashUI.CYAN, 0.05f), null);
            float bw = trackW * value[i] / 320f;
            DashUI.Img(p, "eFill", trackX, rt - 7f, bw, 9f,
                EnergyBar != null ? Color.white : new Color(0.235f, 0.851f, 1f), EnergyBar);
            DashUI.Txt(p, "eVal", trackX + bw + 6f, rt, 42f, rowH, value[i].ToString(), 9,
                new Color(0.624f, 0.847f, 0.914f));
        }
    }

    // ================================================================= 公共绘制工具

    static void SubTitle(RectTransform p, float top, float W, string left, string right)
    {
        DashUI.Txt(p, "subL", PAD, top - 16f, W - PAD * 2f - 150f, 16f, left, 9, SUBCN);
        DashUI.Txt(p, "subR", W - PAD - 150f, top - 16f, 150f, 16f, right, 8, SUBCN2,
            TextAnchor.MiddleRight);
    }

    static float TableHeader(RectTransform p, float top, float W, string[] cols, float[] xs, float[] ws)
    {
        float h = 16f;
        for (int i = 0; i < cols.Length; i++)
            DashUI.Txt(p, "th", PAD + xs[i], top - h, ws[i] - 4f, h, cols[i], 9, HEAD);
        HLine(p, PAD, top - h, W - PAD * 2f, LINE);
        return top - h - 1f;
    }

    static void DevCell(RectTransform p, float x, float rowTop, string name, string sub)
    {
        DashUI.Txt(p, "dev", x, rowTop - 3f, 150f, 13f, name, 10, BATCH, TextAnchor.MiddleLeft, true);
        DashUI.Txt(p, "devSub", x, rowTop - 16f, 150f, 11f, sub, 8, SUBT);
    }

    static void Pill(RectTransform p, float x, float rowTop, float rowH, float w, string label, Color color)
    {
        float h = 16f;
        float cy = rowTop - rowH * 0.5f;
        float top = cy + h * 0.5f;
        DashUI.Img(p, "pillBg", x, top, w, h, A(color, 0.10f), DashUI.BarH);
        DashUI.Dot(p, x + 8f, cy, 1.8f, color);
        DashUI.Txt(p, "pillTx", x + 14f, top, w - 16f, h, label, 9, color);
    }

    static void Footer(RectTransform p, float top, float W, string left, string right)
    {
        HLine(p, PAD, top, W - PAD * 2f, LINE);
        DashUI.Txt(p, "footL", PAD, top - 20f, W - PAD * 2f - 210f, 20f, left, 9, SUBT);
        DashUI.Txt(p, "footR", W - PAD - 210f, top - 20f, 210f, 20f, right, 9, SUBT,
            TextAnchor.MiddleRight);
    }

    static void Outline(RectTransform p, float x, float top, float w, float h, Color c)
    {
        HLine(p, x, top, w, c);
        HLine(p, x, top - h + 1f, w, c);
        DashUI.Img(p, "vL", x, top, 1f, h, c, null);
        DashUI.Img(p, "vR", x + w - 1f, top, 1f, h, c, null);
    }

    static void HLine(RectTransform p, float x, float y, float w, Color c)
    {
        DashUI.Img(p, "line", x, y, w, 1f, c, null);
    }

    static void DashedBottom(RectTransform p, float x0, float x1, float y)
    {
        DashUI.Img(p, "xAxis", x0, y, x1 - x0, 1f, AXIS, null);
    }

    static Color A(Color c, float a)
    {
        c.a = a;
        return c;
    }

    static string Strong(string s)
    {
        return "<color=#A8CFDF>" + s + "</color>";
    }

    static string Unit(string s)
    {
        return "<size=9><color=#66879B>" + s + "</color></size>";
    }

    static string Lit(string s, int size, Color c)
    {
        return "<size=" + size + "><color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color></size>";
    }

    static Vector2 Norm(Vector2 px, float ox, float oyTop, float w, float h)
    {
        return new Vector2((px.x - ox) / w, 1f - (oyTop - px.y) / h);
    }

    static List<Vector2> Smooth(List<Vector2> pts, int steps)
    {
        var outPts = new List<Vector2>();
        if (pts == null || pts.Count < 2) return outPts;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 p0 = i == 0 ? pts[i] : pts[i - 1];
            Vector2 p1 = pts[i];
            Vector2 p2 = pts[i + 1];
            Vector2 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];
            for (int s = 0; s < steps; s++)
                outPts.Add(CatmullRom(p0, p1, p2, p3, s / (float)steps));
        }
        outPts.Add(pts[pts.Count - 1]);
        return outPts;
    }

    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1 + (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
