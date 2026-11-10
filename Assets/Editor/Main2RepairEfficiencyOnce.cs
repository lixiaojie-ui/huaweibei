using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 一次性脚本：把 Canvas right2 / Image2 重建为“设备维修效率”面板。
/// 上方 3 个 KPI 卡片，下方按故障类型拆分的维修时长堆叠条形图，纯图表无表格。
/// 样式对齐同列“设备健康”面板：描边式 KPI 卡、11/10 号字、同一套底色与分隔线。
/// </summary>
[InitializeOnLoad]
public static class Main2RepairEfficiencyOnce
{
    const string FlagPath = "Library/main2_repair_efficiency_v2.flag";
    const string ResultPath = "Library/main2_repair_efficiency_v2_result.txt";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    const float PAD = 14f;
    const float TILE_TOP = -10f;
    const float TILE_H = 52f;

    static readonly Color CYAN = new Color(0.235f, 0.851f, 1f);
    static readonly Color GREEN = new Color(0.259f, 0.871f, 0.620f);
    static readonly Color ORANGE = new Color(1f, 0.706f, 0.329f);
    static readonly Color PURPLE = new Color(0.706f, 0.549f, 1f);
    static readonly Color TC = new Color(0.906f, 0.949f, 0.980f, 1f);
    static readonly Color SUBT = new Color(0.486f, 0.576f, 0.647f, 1f);
    static readonly Color LINE = new Color(0.298f, 0.627f, 0.804f, 0.18f);
    static readonly Color TILELINE = new Color(0.251f, 0.667f, 0.863f, 0.14f);
    static readonly Color AXIS = new Color(0.353f, 0.647f, 0.804f, 0.16f);
    static readonly Color ROWLINE = new Color(1f, 1f, 1f, 0.05f);
    static readonly Color TRACK = new Color(1f, 1f, 1f, 0.03f);
    static readonly Color DARKTXT = new Color(0.035f, 0.086f, 0.125f, 0.90f);
    static readonly Color TAG_GREEN = new Color(0.361f, 0.910f, 0.678f);
    static readonly Color TAG_ORANGE = new Color(1f, 0.769f, 0.420f);

    static readonly string[] RowNames = { "电气故障", "机械故障", "液压故障", "传感器故障", "通讯故障" };
    static readonly float[,] SegHours =
    {
        { 0.4f, 0.7f, 1.1f, 0.3f },
        { 0.5f, 0.8f, 1.4f, 0.4f },
        { 0.3f, 0.6f, 0.9f, 0.3f },
        { 0.2f, 0.4f, 0.5f, 0.2f },
        { 0.2f, 0.5f, 0.6f, 0.2f }
    };
    static readonly Color[] SegColors = { CYAN, GREEN, ORANGE, PURPLE };

    static Main2RepairEfficiencyOnce()
    {
        if (File.Exists(FlagPath)) return;
        EditorApplication.delayCall += TryRun;
    }

    static void TryRun()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(ResultPath, "waiting: playmode");
            EditorApplication.delayCall += TryRun;
            return;
        }

        File.WriteAllText(FlagPath, "started " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        Run();
    }

    [MenuItem("大屏/重建 main2 设备维修效率")]
    public static void Run()
    {
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RectTransform right = DashUI.FindRoot("Canvas right2");
            if (right == null)
            {
                File.WriteAllText(ResultPath, "ERROR: Canvas right2 not found");
                return;
            }

            RectTransform slot = DashUI.Child(right, "Image2");
            if (slot == null) slot = DashUI.Child(right, "Image-设备维修效率");
            if (slot == null)
            {
                File.WriteAllText(ResultPath, "ERROR: Image2 not found under Canvas right2");
                return;
            }

            RectTransform titleRt = DashUI.Child(right, "Text2");
            if (titleRt != null)
            {
                Text title = titleRt.GetComponent<Text>();
                if (title != null)
                {
                    Undo.RecordObject(title, "更新设备维修效率标题");
                    title.text = "设备维修效率";
                    EditorUtility.SetDirty(title);
                }
            }

            if (slot.gameObject.name != "Image-设备维修效率")
            {
                Undo.RecordObject(slot.gameObject, "重命名设备维修效率容器");
                slot.gameObject.name = "Image-设备维修效率";
                EditorUtility.SetDirty(slot.gameObject);
            }

            DashUI.Clear(slot);
            BuildRepairEfficiency(slot);

            scene = slot.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            int texts = 0;
            foreach (Text t in slot.GetComponentsInChildren<Text>(true)) texts++;
            File.WriteAllText(ResultPath,
                "ok\nsaved=" + saved +
                "\nslot=" + slot.name +
                "\ntitle=" + (titleRt != null && titleRt.GetComponent<Text>() != null
                    ? titleRt.GetComponent<Text>().text : "<missing>") +
                "\nchildren=" + slot.childCount +
                "\ntexts=" + texts +
                "\nsize=" + slot.sizeDelta.x + "x" + slot.sizeDelta.y +
                "\nisPlaying=" + EditorApplication.isPlaying);
            Debug.Log("[main2] 设备维修效率面板重建完成，场景保存=" + saved);
        }
        catch (Exception e)
        {
            File.WriteAllText(ResultPath, "ERROR\n" + e);
            Debug.LogError("[main2] 设备维修效率面板重建失败: " + e);
        }
    }

    static void BuildRepairEfficiency(RectTransform p)
    {
        float W = p.sizeDelta.x;
        float H = p.sizeDelta.y;

        // 顶部 KPI 卡片：与“设备健康”面板一致的描边卡样式
        float tileW = (W - PAD * 2f - 14f) / 3f;
        float tx0 = PAD;
        float tx1 = PAD + tileW + 7f;
        float tx2 = PAD + (tileW + 7f) * 2f;

        KpiTile(p, tx0, TILE_TOP, tileW, TILE_H, CYAN,
            "平均修复时长", "2.4", "h", "较上月 -0.3 h", TAG_GREEN);
        KpiTile(p, tx1, TILE_TOP, tileW, TILE_H, GREEN,
            "一次修复率", "92.6", "%", "较上月 +1.8%", TAG_GREEN);
        KpiTile(p, tx2, TILE_TOP, tileW, TILE_H, ORANGE,
            "超时工单", "5", "项", "目标 ≤3 项", TAG_ORANGE);

        // 图表标题与图例
        float titleY = -76f;
        DashUI.Txt(p, "trendTitle", PAD, titleY, 260f, 14f,
            "各故障类型维修时长构成", 11, TC);

        float legendX = W - PAD - 50f * 4f;
        LegendItem(p, legendX, titleY, "响应", CYAN);
        LegendItem(p, legendX + 50f, titleY, "诊断", GREEN);
        LegendItem(p, legendX + 100f, titleY, "维修", ORANGE);
        LegendItem(p, legendX + 150f, titleY, "验收", PURPLE);

        // 图表区域
        float plotTop = -98f;
        float axisY = -356f;
        float plotH = plotTop - axisY;
        const float labelX = PAD;
        const float labelW = 70f;
        const float barX = 94f;
        float barRight = W - PAD - 52f;
        float plotW = barRight - barX;
        const float maxV = 3.4f;
        float scale = plotW / maxV;
        const float rowH = 52f;
        const float barH = 22f;

        // 竖向网格与时间刻度
        for (int t = 0; t <= 3; t++)
        {
            float gx = barX + t * scale;
            if (t > 0)
                DashUI.Img(p, "grid", gx, plotTop, 1f, plotH, ROWLINE, null);
            DashUI.Txt(p, "xLbl", gx - 14f, axisY - 6f, 28f, 12f,
                t + "h", 10, SUBT, TextAnchor.MiddleCenter);
        }

        DashUI.Img(p, "yAxis", barX - 1f, plotTop, 1f, plotH, AXIS, null);
        DashUI.Img(p, "xAxis", barX, axisY, plotW, 1f, AXIS, null);

        // 行与堆叠条
        for (int i = 0; i < RowNames.Length; i++)
        {
            float rowTop = plotTop - i * rowH;
            float barTop = rowTop - 12f;

            DashUI.Txt(p, "rowLbl", labelX, barTop, labelW, barH,
                RowNames[i], 11, TC, TextAnchor.MiddleRight);
            DashUI.Img(p, "track", barX, barTop, plotW, barH, TRACK, null);

            float sx = barX;
            float total = 0f;
            for (int s = 0; s < 4; s++)
            {
                float d = SegHours[i, s];
                float sw = d * scale;
                total += d;

                float drawW = Mathf.Max(0f, sw - 2f);
                if (drawW > 0.5f)
                {
                    DashUI.Img(p, "repSeg", sx, barTop, drawW, barH, SegColors[s], null);
                    if (drawW >= 56f)
                    {
                        DashUI.Txt(p, "segVal", sx, barTop, drawW, barH,
                            d.ToString("0.0"), 9, DARKTXT, TextAnchor.MiddleCenter);
                    }
                }
                sx += sw;
            }

            DashUI.Txt(p, "total", barRight + 6f, barTop, 46f, barH,
                total.ToString("0.0") + "<size=9><color=#66879B> h</color></size>",
                12, TC, TextAnchor.MiddleLeft, true);
        }

        // 目标线（画在条形之上，作为参考）
        float targetX = barX + 2.5f * scale;
        DashUI.SegDash(p, targetX, plotTop, targetX, axisY, 1f,
            new Color(ORANGE.r, ORANGE.g, ORANGE.b, 0.45f), 4f, 3f);
        DashUI.Txt(p, "targetLbl", targetX - 32f, plotTop + 4f, 64f, 10f,
            "目标 2.5 h", 9, ORANGE, TextAnchor.MiddleCenter);

        // 页脚
        float footLineY = -(H - 34f);
        DashUI.Img(p, "footLine", PAD, footLineY, W - PAD * 2f, 1f, LINE, null);
        DashUI.Txt(p, "footL", PAD, footLineY - 15f, W - PAD * 2f - 150f, 14f,
            "统计范围 全部设备 · 5 类故障 · 样本 128 单", 10, SUBT);
        DashUI.Txt(p, "footR", W - PAD - 150f, footLineY - 15f, 150f, 14f,
            "数据刷新 5 s", 10, SUBT, TextAnchor.MiddleRight);
    }

    static void KpiTile(RectTransform p, float x, float y, float w, float h,
                        Color accent, string key, string value, string unit,
                        string tag, Color tagColor)
    {
        DashUI.Img(p, "tileBg", x, y, w, h,
            new Color(accent.r, accent.g, accent.b, 0.078f), null);
        DashUI.Img(p, "tileT", x, y, w, 1f, TILELINE, null);
        DashUI.Img(p, "tileB", x, y + h - 1f, w, 1f, TILELINE, null);
        DashUI.Img(p, "tileL", x, y, 1f, h, TILELINE, null);
        DashUI.Img(p, "tileR", x + w - 1f, y, 1f, h, TILELINE, null);

        DashUI.Txt(p, "tileK", x + 12f, y - 4f, w - 24f, 12f, key, 11, TC);
        string suffix = unit == "%" ? "%" : " " + unit;
        DashUI.Txt(p, "tileV", x + 12f, y - 17f, w - 20f, 22f,
            value + "<size=9><color=#66879B>" + suffix + "</color></size>",
            18, TC, TextAnchor.MiddleLeft, true);
        DashUI.Txt(p, "tileTag", x + w - 112f, y - 39f, 100f, 10f,
            tag, 10, tagColor, TextAnchor.MiddleRight);
    }

    static void LegendItem(RectTransform p, float x, float y, string label, Color c)
    {
        DashUI.Dot(p, x + 3f, y - 8f, 3f, c);
        DashUI.Txt(p, "lg", x + 10f, y - 2f, 40f, 14f, label, 10, TC);
    }
}
