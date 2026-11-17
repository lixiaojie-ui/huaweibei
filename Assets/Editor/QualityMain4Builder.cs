using System.Collections.Generic;
using Daima;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 质量检测页（Canvas main4）：左 3 图 + 右 2 图 + 1 个滚动检测记录表。
/// 5 张卡片内容互不重复：合格率环、工段横向条形、7 日趋势、缺陷构成环、滚动明细表。
/// 全部由 uGUI 矢量图元（UIRoundRect / UIRingGraphic / UILineGraphic）组成，表格不贴任何背景图。
/// 菜单位置：Tools/质量检测
/// </summary>
public static class QualityMain4Builder
{
    const string BuiltKey = "QualityMain4_Build_v12";

    // ------------------------------------------------------------------ 配色
    static readonly Color Ink = new Color(0.933f, 0.973f, 1f, 1f);
    static readonly Color Muted = new Color(0.659f, 0.792f, 0.855f, 1f);
    static readonly Color Dim = new Color(0.510f, 0.635f, 0.706f, 1f);
    static readonly Color Cyan = new Color(0.318f, 0.847f, 0.965f, 1f);
    static readonly Color Blue = new Color(0.239f, 0.510f, 0.859f, 1f);
    static readonly Color Teal = new Color(0.212f, 0.898f, 0.729f, 1f);
    static readonly Color Warn = new Color(0.961f, 0.757f, 0.376f, 1f);
    static readonly Color Red = new Color(1f, 0.490f, 0.537f, 1f);
    static readonly Color Track = new Color(0.749f, 0.878f, 0.949f, 0.16f);
    static readonly Color GridLine = new Color(0.494f, 0.804f, 0.961f, 0.14f);
    static readonly Color PillFill = new Color(0.078f, 0.259f, 0.380f, 0.55f);
    static readonly Color CardBg = new Color(0.067f, 0.196f, 0.325f, 0.280f);

    // 全局字号放大系数（1080p 大屏上原字号偏小）
    const float TextScale = 1.18f;

    // 页面栅格（1920x1080，自上而下）
    const float LeftX = 12f, LeftW = 556f;
    const float RightX = 1352f, RightW = 566f;
    const float HeaderH = 86f;

    static Font _cn, _num, _en;

    static Font CN
    {
        get
        {
            if (_cn == null)
            {
                try { _cn = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (_cn == null) { try { _cn = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                if (_cn == null) _cn = DashUI.F;
            }
            return _cn;
        }
    }

    static Font NUM
    {
        get
        {
            if (_num == null) _num = AssetDatabase.LoadAssetAtPath<Font>("Assets/fonts/Tektur-Regular.ttf");
            return _num != null ? _num : CN;
        }
    }

    static Font EN
    {
        get
        {
            if (_en == null) _en = AssetDatabase.LoadAssetAtPath<Font>("Assets/fonts/BigShoulders-Bold.ttf");
            return _en != null ? _en : NUM;
        }
    }

    // ------------------------------------------------------------------ 入口

    // 已关闭自动重建：场景里的界面由手工维护，避免覆盖手动调整。

    [MenuItem("Tools/质量检测/Main4 生成质量检测界面", false, 30)]
    public static void BuildMenu()
    {
        if (Build())
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[QD4] 质量检测界面已重建并保存。");
        }
        else
        {
            Debug.LogWarning("[QD4] 未找到 Canvas main4 / Canvas left1 / Canvas right1，或卡片数量不足。");
        }
    }

    [MenuItem("Tools/质量检测/Main4 清除质量检测界面", false, 31)]
    public static void ClearMenu()
    {
        Transform main4 = FindMain4();
        if (main4 == null) return;

        for (int i = 0; i < main4.childCount; i++)
        {
            Transform panel = main4.GetChild(i);
            var kill = new List<GameObject>();
            for (int j = 0; j < panel.childCount; j++)
            {
                Transform card = panel.GetChild(j);
                if (!IsCard(card)) continue;
                Transform old = card.Find("__QD");
                if (old != null) kill.Add(old.gameObject);
            }
            for (int k = 0; k < kill.Count; k++) Object.DestroyImmediate(kill[k]);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[QD4] 已清除质量检测界面内容（保留卡片容器）。");
    }

    // ------------------------------------------------------------------ 构建

    static bool Build()
    {
        Transform main4 = FindMain4();
        if (main4 == null) return false;

        RectTransform left = main4.Find("Canvas left1") as RectTransform;
        RectTransform right = main4.Find("Canvas right1") as RectTransform;
        if (left == null || right == null) return false;

        List<RectTransform> lc = Cards(left);
        List<RectTransform> rc = Cards(right);
        if (lc.Count < 3 || rc.Count < 2)
        {
            Debug.LogWarning("[QD4] 左侧卡片 " + lc.Count + " 张，右侧卡片 " + rc.Count + " 张，数量不足。");
            return false;
        }

        CleanPanel(left);
        CleanPanel(right);

        // 左列 3 图
        BuildYieldCard(lc[0], left, "质量总览", "QUALITY OVERVIEW", "近 24 小时");
        BuildRadarCard(lc[1], left, "检测项能力雷达", "INSPECTION CAPABILITY RADAR", "近 6 批均值");
        BuildRollingTrendCard(lc[2], left, "实时批次检测趋势", "LIVE INSPECTION TREND", "每 2 秒刷新");

        // 右列 2 图（上面是滚动明细表）
        BuildTableCard(rc[0], right, RightX, HeaderH, RightW, 609f, "实时检测记录", "LIVE INSPECTION LOG", "自动滚动");
        BuildMatrixCard(rc[1], right, RightX, 707f, RightW, 348f, "工段 × 检测项 合格率矩阵", "QUALITY MATRIX", "近 6 批");

        BuildHeader(left, true);
        BuildHeader(right, false);

        SessionState.SetBool("QD4Built", true);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return true;
    }

    static Transform FindMain4()
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i].name != "Canvas main4") continue;
            if (!all[i].scene.IsValid()) continue;
            return all[i].transform;
        }
        return null;
    }

    static bool IsCard(Transform t)
    {
        if (t == null) return false;
        if (t.gameObject.GetComponent<Image>() == null) return false;
        return t.name.StartsWith("Image-") || t.name.StartsWith("Card-");
    }

    static List<RectTransform> Cards(RectTransform panel)
    {
        var list = new List<RectTransform>();
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform t = panel.GetChild(i);
            if (!IsCard(t)) continue;
            list.Add(t as RectTransform);
        }
        list.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
        return list;
    }

    /// <summary>删掉旧页面的装饰图 / 旧标题，保证新界面干净。</summary>
    static void CleanPanel(RectTransform panel)
    {
        var kill = new List<GameObject>();
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform t = panel.GetChild(i);
            string n = t.name;
            if (n.StartsWith("logo") || n.StartsWith("Image logo") || n.StartsWith("Text") ||
                n == "订单明细" || n == "AGV实时调度")
                kill.Add(t.gameObject);
        }
        for (int i = 0; i < kill.Count; i++) Object.DestroyImmediate(kill[i]);
    }

    // ------------------------------------------------------------------ 卡片外壳

    /// <summary>把卡片放到屏幕坐标（x, top, w, h），清空旧内容并画出统一外壳。</summary>
    static RectTransform Frame(RectTransform card, RectTransform canvas,
                               float x, float top, float w, float h,
                               string title, string en, string pill, Color pillColor)
    {
        float canvasH = canvas.rect.height > 1f ? canvas.rect.height : 1080f;
        Vector2 center = canvas.anchoredPosition;

        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(w, h);
        card.localScale = Vector3.one;
        card.anchoredPosition = new Vector2(x + w * 0.5f - center.x,
                                            canvasH - (top + h * 0.5f) - center.y);

        // 卡片底色与 Canvas main1「Image-订单明细」保持一致：半透明深蓝底 + 直角，不加边框/渐变
        Image self = card.GetComponent<Image>();
        if (self != null)
        {
            if (self.sprite == null)
                self.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            self.color = CardBg;
            self.type = Image.Type.Sliced;
            self.raycastTarget = false;
        }

        for (int i = card.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(card.GetChild(i).gameObject);

        card.name = "Card-" + title;

        RectTransform root = Nd(card, "__QD", 0f, 0f, w, h);

        UIRoundRect accent = RR(root, "qdAccent", 14f, 15f, 3f, 14f, Cyan, 1.5f, Color.clear, 0f);
        accent.gradient = true;
        accent.verticalGradient = true;
        accent.color2 = new Color(0.184f, 0.827f, 0.961f, 0.15f);
        accent.Refresh();

        Text t1 = Tx(root, "qdTitle", title, 15, Ink, 24f, 9f, 300f, 22f, TextAnchor.MiddleLeft, CN, true);
        UILetterSpacing ls = t1.gameObject.AddComponent<UILetterSpacing>();
        ls.SetSpacing(0.6f, false);

        Tx(root, "qdTitleEn", en, 9, new Color(0.659f, 0.792f, 0.855f, 0.92f), 24f, 27f, 340f, 12f,
           TextAnchor.MiddleLeft, EN);

        if (!string.IsNullOrEmpty(pill))
        {
            float pw = PillWidth(pill, 9) + 22f;
            Pill(root, "qdPill", pill, w - 14f - pw, 13f, pw, 22f, pillColor);
        }

        HLine(root, "qdDivider", 14f, 42f, w - 28f, new Color(0.494f, 0.804f, 0.961f, 0.14f));
        return root;
    }

    // ------------------------------------------------------------------ 页面标题

    static RectTransform ScreenLayer(RectTransform panel)
    {
        Transform old = panel.Find("__QDHeader");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        float canvasH = panel.rect.height > 1f ? panel.rect.height : 1080f;
        RectTransform layer = Nd(panel, "__QDHeader", 0f, 0f, 1920f, 1080f);
        layer.anchorMin = new Vector2(0.5f, 0.5f);
        layer.anchorMax = new Vector2(0.5f, 0.5f);
        layer.pivot = new Vector2(0.5f, 0.5f);
        layer.sizeDelta = new Vector2(1920f, 1080f);
        layer.anchoredPosition = new Vector2(960f - panel.anchoredPosition.x,
                                             canvasH * 0.5f - panel.anchoredPosition.y);
        return layer;
    }

    static void BuildHeader(RectTransform panel, bool isLeft)
    {
        RectTransform head = ScreenLayer(panel);

        if (isLeft)
        {
            UIRoundRect accent = RR(head, "qdPageAccent", LeftX, 20f, 3f, 26f, Cyan, 1.5f, Color.clear, 0f);
            accent.gradient = true;
            accent.verticalGradient = true;
            accent.color2 = new Color(0.184f, 0.827f, 0.961f, 0.10f);
            accent.Refresh();

            Text title = Tx(head, "qdPageTitle", "质量检测", 20, Ink, LeftX + 12f, 12f, 260f, 28f,
                            TextAnchor.MiddleLeft, CN, true);
            UILetterSpacing ls = title.gameObject.AddComponent<UILetterSpacing>();
            ls.SetSpacing(2.2f, false);

            Tx(head, "qdPageEn", "QUALITY INSPECTION  /   PROCESS DATA", 9,
               new Color(0.659f, 0.792f, 0.855f, 0.9f), LeftX + 12f, 41f, 380f, 12f,
               TextAnchor.MiddleLeft, EN);

            float pw = 112f;
            Pill(head, "qdPagePill", "在线 · 24 项检测", LeftX + LeftW - pw, 20f, pw, 26f, Teal);
        }
        else
        {
            Dot(head, "qdLiveDot", RightX + 5f, 33f, 3f, Teal);
            Tx(head, "qdLiveTx", "实时监测中", 10, Ink, RightX + 14f, 22f, 90f, 20f, TextAnchor.MiddleLeft, CN);
            Tx(head, "qdLiveSub", "早班 08:00-16:00  ·  检测点 41", 9, Muted, RightX + 88f, 23f, 240f, 18f,
               TextAnchor.MiddleLeft, CN);

            Text upd = Tx(head, "qdUpdate", "", 9, Muted, RightX + RightW - 160f, 23f, 160f, 18f,
                          TextAnchor.MiddleRight, CN);
            upd.text = "<color=#7FD8FF>数据更新</color>   11:42:18";
        }
    }

    // ------------------------------------------------------------------ 左 1：质量总览

    static void BuildYieldCard(RectTransform card, RectTransform canvas, string title, string en, string pill)
    {
        RectTransform r = Frame(card, canvas, LeftX, HeaderH, LeftW, 315f, title, en, pill, Teal);
        const float w = LeftW;

        float size = 132f, rx = 18f, ry = 56f;
        Ring(r, "qdRingTrack", rx, ry, size, new float[] { 1f }, new Color[] { Track }, 0.13f, 90f, 360f, 0f);
        Ring(r, "qdRingValue", rx, ry, size, new float[] { 98.6f, 1.4f },
             new Color[] { Teal, new Color(0f, 0f, 0f, 0f) }, 0.13f, 90f, 360f, 0f);

        float cx = rx + size * 0.5f, cy = ry + size * 0.5f;
        Tx(r, "qdRingValueTx", "98.6%", 24, Teal, cx - 62f, cy - 20f, 124f, 28f, TextAnchor.MiddleCenter, NUM, true);
        Tx(r, "qdRingLabel", "一次合格率", 8, Muted, cx - 60f, cy + 8f, 120f, 12f, TextAnchor.MiddleCenter, CN);
        Tx(r, "qdRingSub", "目标 ≥98.0%", 8, new Color(0.659f, 0.792f, 0.855f, 0.75f),
           cx - 60f, cy + 22f, 120f, 12f, TextAnchor.MiddleCenter, CN);

        string[] labels = { "检测批次", "合格批次", "不良批次", "复检放行" };
        string[] values = { "128", "126", "2", "2" };
        string[] units = { "批", "批", "批", "批" };
        string[] deltas = { "较昨日 +6", "占 98.4%", "较昨日 -1", "全部合格" };
        Color[] vColors = { Ink, Teal, Warn, Ink };

        float ox = 170f, oy = 54f, cw = 180f, chh = 56f;
        for (int i = 0; i < 4; i++)
        {
            int col = i % 2, row = i / 2;
            float x = ox + col * (cw + 8f);
            float y = oy + row * (chh + 6f);

            UIRoundRect cell = RR(r, "qdKpiBg" + i, x, y, cw, chh, new Color(0.078f, 0.259f, 0.380f, 0.32f),
                                  2f, new Color(0.318f, 0.847f, 0.965f, 0.14f), 1f);
            cell.Refresh();

            Tx(r, "qdKpiLabel" + i, labels[i], 9, Muted, x + 10f, y + 7f, cw - 20f, 13f, TextAnchor.MiddleLeft, CN);
            Tx(r, "qdKpiValue" + i, values[i], 19, vColors[i], x + 10f, y + 20f, 80f, 26f,
               TextAnchor.MiddleLeft, NUM, true);
            Tx(r, "qdKpiUnit" + i, units[i], 8, Muted, x + 12f + TextW(values[i], 19), y + 30f, 20f, 12f,
               TextAnchor.MiddleLeft, CN);
            Tx(r, "qdKpiDelta" + i, deltas[i], 8, new Color(0.659f, 0.792f, 0.855f, 0.92f),
               x + 10f, y + 40f, cw - 20f, 12f, TextAnchor.MiddleLeft, CN);
        }

        HLine(r, "qdYieldDiv", 14f, 190f, w - 28f, GridLine);

        string[] keys = { "成品水分", "混合均匀度 CV", "包装袋重偏差" };
        string[] vals = { "11.8", "4.6", "0.6" };
        string[] units2 = { "%", "%", "%" };
        string[] notes = { "标准 ≤12.5%", "标准 ≤5.0%", "规格 25±0.1kg" };
        float[] used = { 0.94f, 0.92f, 0.60f };
        string[] margins = { "余量 0.7pct", "余量 0.4pct", "余量 0.4pct" };

        float colW = (w - 28f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            float x = 14f + colW * i;
            if (i > 0) Img(r, "qdYieldSep" + i, x - 6f, 198f, 1f, 58f, GridLine);

            Tx(r, "qdKeyLabel" + i, keys[i], 9, Muted, x, 196f, colW - 12f, 13f, TextAnchor.MiddleLeft, CN);
            Tx(r, "qdKeyValue" + i, vals[i], 19, Ink, x, 210f, 70f, 26f, TextAnchor.MiddleLeft, NUM, true);
            Tx(r, "qdKeyUnit" + i, units2[i], 9, Muted, x + TextW(vals[i], 19) + 3f, 220f, 20f, 12f,
               TextAnchor.MiddleLeft, CN);
            Tx(r, "qdKeyNote" + i, notes[i], 8, Dim, x, 238f, colW - 12f, 12f, TextAnchor.MiddleLeft, CN);

            UIRoundRect track = RR(r, "qdKeyTrack" + i, x, 256f, colW - 18f, 5f, Track, 2.5f, Color.clear, 0f);
            track.Refresh();
            UIRoundRect fill = RR(r, "qdKeyFill" + i, x, 256f, (colW - 18f) * used[i], 5f,
                                  used[i] > 0.93f ? Warn : Teal, 2.5f, Color.clear, 0f);
            fill.Refresh();

            Tx(r, "qdKeyMargin" + i, margins[i], 8, Dim, x, 264f, colW - 12f, 12f, TextAnchor.MiddleLeft, CN);
            Dot(r, "qdKeyDot" + i, x + colW - 32f, 220f, 2.5f, Teal);
            Tx(r, "qdKeyFlag" + i, "达标", 8, Teal, x + colW - 28f, 212f, 30f, 12f, TextAnchor.MiddleLeft, CN);
        }

        HLine(r, "qdYieldDiv2", 14f, 278f, w - 28f, GridLine);
        Text sum = Tx(r, "qdYieldSum",
            "近 24 小时检测 <color=#EAF6FF>12,480</color> 项  ·  复检 <color=#F5BF5E>428</color> 项  ·  客户端零投诉",
            9, Muted, 14f, 286f, w - 28f, 14f, TextAnchor.MiddleLeft, CN);
        sum.supportRichText = true;
    }

    // ------------------------------------------------------------------ 左 2：检测项能力雷达

    static void BuildRadarCard(RectTransform card, RectTransform canvas, string title, string en, string pill)
    {
        RectTransform r = Frame(card, canvas, LeftX, 413f, LeftW, 315f, title, en, pill, Cyan);

        string[] axes = { "水分控制", "混合均匀度", "粉碎粒度", "粉尘浓度", "包装袋重", "温度控制" };
        float[] score = { 94f, 92f, 97f, 88f, 96f, 90f };
        string[] actual = { "11.8%", "4.6%CV", "1.82mm", "2.4mg", "25.02kg", "92℃" };

        const float size = 176f, px = 34f, py = 70f;
        float cxp = px + size * 0.5f, cyp = py + size * 0.5f;
        float radius = size * 0.46f;

        RectTransform plot = Nd(r, "qdRadarPlot", px, py, size, size);

        for (int ring = 1; ring <= 3; ring++)
        {
            var ringPts = new List<Vector2>();
            float k = ring / 3f;
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Deg2Rad * (90f - i * 60f);
                ringPts.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.46f * k, 0.5f + Mathf.Sin(a) * 0.46f * k));
            }
            ringPts.Add(ringPts[0]);
            AddLine(plot, "qdRadarGrid" + ring, ringPts, 1f, 0f,
                    new Color(0.494f, 0.804f, 0.961f, ring == 3 ? 0.28f : 0.12f));
        }

        for (int i = 0; i < 6; i++)
        {
            RectTransform ax = Nd(r, "qdRadarAxis" + i, cxp, cyp, 1f, radius);
            ax.pivot = new Vector2(0.5f, 0f);
            ax.anchoredPosition = new Vector2(cxp, -cyp);
            ax.localRotation = Quaternion.Euler(0f, 0f, -i * 60f);
            ax.gameObject.AddComponent<CanvasRenderer>();
            var im = ax.gameObject.AddComponent<Image>();
            im.color = new Color(0.494f, 0.804f, 0.961f, 0.10f);
            im.raycastTarget = false;
        }

        var poly = new List<Vector2>();
        for (int i = 0; i < 6; i++)
        {
            float a = Mathf.Deg2Rad * (90f - i * 60f);
            float k = score[i] / 100f * 0.46f;
            poly.Add(new Vector2(0.5f + Mathf.Cos(a) * k, 0.5f + Mathf.Sin(a) * k));
        }

        RectTransform fillRt = Nd(plot, "qdRadarFill", 0f, 0f, size, size);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillRt.gameObject.AddComponent<CanvasRenderer>();
        var fill = fillRt.gameObject.AddComponent<UIRadarFillGraphic>();
        fill.color = Color.white;
        fill.raycastTarget = false;
        fill.SetData(poly, new Vector2(0.5f, 0.5f),
                     new Color(0.318f, 0.847f, 0.965f, 0.40f),
                     new Color(0.212f, 0.898f, 0.729f, 0.12f));

        var edge = new List<Vector2>(poly);
        edge.Add(poly[0]);
        AddLine(plot, "qdRadarEdge", edge, 2f, 4f, Teal);

        for (int i = 0; i < 6; i++)
        {
            float a = Mathf.Deg2Rad * (90f - i * 60f);
            float lx = cxp + Mathf.Cos(a) * (radius + 16f);
            float ly = cyp - Mathf.Sin(a) * (radius + 16f);
            Tx(r, "qdRadarLabel" + i, axes[i], 8, Muted, lx - 36f, ly - 8f, 72f, 16f, TextAnchor.MiddleCenter, CN);
        }

        // 右侧指标明细
        float lx0 = 262f, rowStep = 33f;
        for (int i = 0; i < 6; i++)
        {
            float y = 74f + i * rowStep;
            Color sc = score[i] >= 95f ? Teal : (score[i] >= 90f ? Warn : Red);

            Dot(r, "qdRadarDot" + i, lx0 + 3f, y + 8f, 2.5f, sc);
            Tx(r, "qdRadarName" + i, axes[i], 9, Ink, lx0 + 12f, y, 96f, 17f, TextAnchor.MiddleLeft, CN);
            Tx(r, "qdRadarActual" + i, actual[i], 9, Muted, lx0 + 108f, y, 70f, 17f, TextAnchor.MiddleRight, CN);
            Text sv = Tx(r, "qdRadarScore" + i, score[i].ToString("0"), 12, sc,
                         lx0 + 186f, y - 1f, 26f, 19f, TextAnchor.MiddleRight, NUM, true);
            sv.horizontalOverflow = HorizontalWrapMode.Overflow;
            Tx(r, "qdRadarScoreUnit" + i, "分", 8, Dim, lx0 + 214f, y + 2f, 14f, 14f, TextAnchor.MiddleLeft, CN);

            if (i < 5) Img(r, "qdRadarRowLine" + i, lx0, y + 26f, 268f, 1f, new Color(0.494f, 0.804f, 0.961f, 0.06f));
        }

        HLine(r, "qdRadarDiv", 14f, 272f, LeftW - 28f, GridLine);
        Text foot = Tx(r, "qdRadarFoot",
            "综合质量指数 <color=#35E0C0>92.8</color>  ·  短板：粉尘浓度 <color=#F5BF5E>88 分</color>  ·  环比 +1.6",
            9, Muted, 14f, 280f, LeftW - 28f, 14f, TextAnchor.MiddleLeft, CN);
        foot.supportRichText = true;
    }

    // ------------------------------------------------------------------ 左 3：实时滚动趋势

    static void BuildRollingTrendCard(RectTransform card, RectTransform canvas, string title, string en, string pill)
    {
        RectTransform r = Frame(card, canvas, LeftX, 740f, LeftW, 315f, title, en, pill, Cyan);
        const float w = LeftW;

        Img(r, "qdRtLgBar", 14f, 48f, 8f, 8f, Blue);
        Tx(r, "qdRtLgBarTx", "检测批次", 8, Muted, 26f, 44f, 80f, 14f, TextAnchor.MiddleLeft, CN);
        Img(r, "qdRtLgLine", 96f, 51f, 14f, 2f, Teal);
        Tx(r, "qdRtLgLineTx", "一次合格率", 8, Muted, 114f, 44f, 90f, 14f, TextAnchor.MiddleLeft, CN);

        Tx(r, "qdRtNowLabel", "当前批次", 8, Dim, w - 186f, 44f, 60f, 14f, TextAnchor.MiddleRight, CN);
        Text nowBatch = Tx(r, "qdRtNow", "76", 13, Ink, w - 122f, 41f, 40f, 18f, TextAnchor.MiddleRight, NUM, true);
        Text nowRate = Tx(r, "qdRtNowRate", "99.00%", 10, Teal, w - 78f, 43f, 64f, 16f, TextAnchor.MiddleRight, NUM, true);

        const int count = 14;
        const float px0 = 46f, px1 = 512f, pyTop = 64f, plotH = 178f, maxBatch = 80f;
        const float barW = 14f;
        float plotW = px1 - px0;
        float baseline = pyTop + plotH;
        float slot = plotW / count;

        float[] batches = { 58f, 61f, 64f, 60f, 66f, 69f, 65f, 71f, 68f, 72f, 70f, 74f, 71f, 76f };
        float[] rates = { 98.10f, 98.40f, 98.20f, 98.60f, 98.50f, 98.80f, 98.60f,
                          98.90f, 98.70f, 99.00f, 98.80f, 99.10f, 98.90f, 99.00f };

        for (int i = 0; i <= 4; i++)
        {
            float value = maxBatch * i / 4f;
            float y = baseline - plotH * (value / maxBatch);
            Img(r, "qdRtGrid" + i, px0, y, plotW, 1f, GridLine);
            Tx(r, "qdRtTick" + i, ((int)value).ToString(), 8, Dim, 10f, y - 7f, 30f, 14f, TextAnchor.MiddleRight, NUM);
        }

        for (int i = 0; i <= 2; i++)
        {
            float value = 96f + 2f * i;
            float y = baseline - plotH * ((value - 96f) / 4f);
            Tx(r, "qdRtRate" + i, value.ToString("0.0"), 8, Dim, px1 + 8f, y - 7f, 34f, 14f, TextAnchor.MiddleLeft, NUM);
        }

        var barList = new List<RectTransform>();
        var labelList = new List<Text>();

        for (int i = 0; i < count; i++)
        {
            float cx = px0 + slot * (i + 0.5f);
            float h = plotH * (batches[i] / maxBatch);

            RectTransform bar = Nd(r, "qdRtBar" + i, cx - barW * 0.5f, baseline - h, barW, h);
            bar.pivot = new Vector2(0f, 0f);
            bar.anchoredPosition = new Vector2(cx - barW * 0.5f, -baseline);

            var rr = bar.gameObject.AddComponent<UIRoundRect>();
            rr.color = new Color(0.560f, 0.918f, 0.988f, 0.95f);
            rr.color2 = new Color(0.239f, 0.580f, 0.898f, 0.95f);
            rr.gradient = true;
            rr.verticalGradient = true;
            rr.radius = 3f;
            rr.raycastTarget = false;
            rr.Refresh();
            barList.Add(bar);

            RectTransform lb = Nd(bar, "qdRtVal" + i, 0f, 0f, 44f, 13f);
            lb.anchorMin = new Vector2(0.5f, 1f);
            lb.anchorMax = new Vector2(0.5f, 1f);
            lb.pivot = new Vector2(0.5f, 0f);
            lb.anchoredPosition = new Vector2(0f, 3f);
            var lt = lb.gameObject.AddComponent<Text>();
            lt.font = NUM;
            lt.fontSize = 11;
            lt.text = batches[i].ToString("0");
            lt.color = Ink;
            lt.alignment = TextAnchor.LowerCenter;
            lt.raycastTarget = false;
            lt.horizontalOverflow = HorizontalWrapMode.Overflow;
            lt.verticalOverflow = VerticalWrapMode.Overflow;
            labelList.Add(lt);
        }

        RectTransform lineRt = Nd(r, "qdRtLine", px0, pyTop, plotW, plotH);
        lineRt.gameObject.AddComponent<CanvasRenderer>();
        var line = lineRt.gameObject.AddComponent<UILineGraphic>();
        line.color = Teal;
        line.raycastTarget = false;
        var pts = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
            pts.Add(new Vector2((i + 0.5f) / count, Mathf.Clamp01((rates[i] - 96f) / 4f)));
        line.SetData(pts, 2f, 4f, Teal);

        HLine(r, "qdRtDiv", 14f, 276f, w - 28f, GridLine);
        Text foot = Tx(r, "qdRtFoot",
            "采样间隔 5 分钟  ·  近 70 分钟共 <color=#EAF6FF>952</color> 批  ·  预警阈值 <color=#F5BF5E>≤60 批</color>  ·  合格率下线 97.0%",
            9, Muted, 14f, 284f, w - 28f, 14f, TextAnchor.MiddleLeft, CN);
        foot.supportRichText = true;

        var trend = r.gameObject.AddComponent<QDRollingTrend>();
        trend.interval = 2f;
        trend.maxBatch = maxBatch;
        trend.plotHeight = plotH;
        trend.barWidth = barW;
        trend.bars = barList.ToArray();
        trend.barLabels = labelList.ToArray();
        trend.rateLine = line;
        trend.currentBatch = nowBatch;
        trend.currentRate = nowRate;
        trend.initBatches = batches;
        trend.initRates = rates;
    }

    // ------------------------------------------------------------------ 右 1：滚动检测记录表

    static readonly string[][] Rows =
    {
        new[] { "11:42:18", "B1116-08", "烘干包装", "天然气烘干机", "出料水分", "11.6%", "合格" },
        new[] { "11:40:55", "B1116-08", "烘干包装", "包装机器人", "袋重", "25.02kg", "合格" },
        new[] { "11:39:12", "B1116-08", "烘干包装", "固体发酵包装线", "封口温度", "168℃", "合格" },
        new[] { "11:37:40", "B1116-08", "二混混合", "二混混合机", "混合均匀度", "4.6%", "合格" },
        new[] { "11:36:05", "B1116-07", "二混混合", "二混配料秤", "配料误差", "0.12%", "合格" },
        new[] { "11:34:22", "B1116-07", "二混混合", "二混刮板机", "链条温度", "46℃", "合格" },
        new[] { "11:32:47", "B1116-07", "一混发酵", "一混发酵床", "床层温度", "38.5℃", "合格" },
        new[] { "11:31:09", "B1116-07", "一混发酵", "液体发酵罐", "pH 值", "6.82", "合格" },
        new[] { "11:29:36", "B1116-07", "一混发酵", "接种系统", "接种量", "2.4%", "合格" },
        new[] { "11:27:58", "B1116-06", "一混发酵", "一混翻料电机", "运行电流", "42.6A", "合格" },
        new[] { "11:26:14", "B1116-06", "原料前处理", "一混粉碎机", "粉碎粒度", "1.82mm", "合格" },
        new[] { "11:24:39", "B1116-06", "原料前处理", "永磁筒", "铁杂清理", "0 件", "合格" },
        new[] { "11:23:02", "B1116-06", "原料前处理", "清理筛", "堵料报警", "无", "合格" },
        new[] { "11:21:25", "B1116-06", "原料前处理", "料筒仓", "仓内料位", "68%", "合格" },
        new[] { "11:19:48", "B1116-05", "原料前处理", "原料库扬尘仪", "粉尘浓度", "2.4mg", "合格" },
        new[] { "11:18:06", "B1116-05", "动力环保", "除尘风机", "风压", "1.62kPa", "合格" },
        new[] { "11:16:33", "B1116-05", "动力环保", "扬尘检测仪", "PM10", "0.21mg", "合格" },
        new[] { "11:14:59", "B1116-05", "动力环保", "天然气表", "瞬时流量", "18.4m3", "合格" },
        new[] { "11:13:21", "B1116-05", "动力环保", "配电室", "功率因数", "0.94", "合格" },
        new[] { "11:11:44", "B1116-04", "烘干包装", "天然气烘干机", "进风温度", "92℃", "合格" },
        new[] { "11:10:07", "B1116-04", "烘干包装", "固体发酵包装线", "袋重", "25.14kg", "复检" },
        new[] { "11:08:30", "B1116-04", "烘干包装", "包装机器人", "夹爪状态", "正常", "合格" },
        new[] { "11:06:52", "B1116-04", "二混混合", "二混混合机", "混合时间", "182s", "合格" },
        new[] { "11:05:15", "B1116-04", "二混混合", "预混料生产线", "配料精度", "0.18%", "合格" },
        new[] { "11:03:38", "B1116-03", "一混发酵", "一混发酵床", "发酵天数", "4.5d", "合格" },
        new[] { "11:02:01", "B1116-03", "一混发酵", "液体发酵罐", "溶氧", "32%", "合格" },
        new[] { "11:00:24", "B1116-03", "原料前处理", "预混料粉碎机", "粉尘浓度", "3.8mg", "复检" },
        new[] { "10:58:47", "B1116-03", "原料前处理", "一混提升仓", "跑偏开关", "正常", "合格" },
    };

    static void BuildTableCard(RectTransform card, RectTransform canvas,
                               float x, float top, float w, float h,
                               string title, string en, string pill)
    {
        RectTransform r = Frame(card, canvas, x, top, w, h, title, en, pill, Cyan);

        float innerX = 14f;
        float innerW = w - 28f;
        float tableTop = 52f;
        float headerH = 24f;
        float bodyTop = tableTop + headerH + 4f;
        float bodyH = h - bodyTop - 20f;

        float[] colX = { 0f, 56f, 152f, 224f, 328f, 416f, 474f };
        float[] colW = { 56f, 96f, 72f, 104f, 88f, 58f, 64f };
        string[] heads = { "时间", "批次", "工段", "设备", "检测项", "实测", "判定" };

        RectTransform head = Nd(r, "qdTableHead", innerX, tableTop, innerW, headerH);
        for (int i = 0; i < heads.Length; i++)
        {
            TextAnchor anchor = i >= 5 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            Tx(head, "qdTh" + i, heads[i], 8, new Color(0.659f, 0.792f, 0.855f, 0.9f),
               colX[i], 0f, colW[i], headerH, anchor, CN);
        }
        HLine(r, "qdTableHeadLine", innerX, tableTop + headerH, innerW,
              new Color(0.494f, 0.804f, 0.961f, 0.22f));

        // 滚动区：无背景图，只用细分割线
        RectTransform scrollRt = Nd(r, "qdTableScroll", innerX, bodyTop, innerW, bodyH);
        var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 18f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;

        RectTransform viewport = Nd(scrollRt, "qdTableViewport", 0f, 0f, innerW, bodyH);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.sizeDelta = Vector2.zero;
        viewport.anchoredPosition = Vector2.zero;
        viewport.gameObject.AddComponent<RectMask2D>();

        float rowH = 26f;
        float contentH = Rows.Length * rowH;
        RectTransform content = Nd(viewport, "qdTableContent", 0f, 0f, innerW, contentH);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, contentH);
        content.anchoredPosition = Vector2.zero;

        for (int i = 0; i < Rows.Length; i++)
        {
            string[] row = Rows[i];
            float y = i * rowH;

            string state = row[6];
            Color sc = state == "合格" ? Teal : (state == "复检" ? Warn : Red);

            for (int c = 0; c < 7; c++)
            {
                Color color = Ink;
                Font font = CN;
                TextAnchor anchor = c >= 5 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                bool bold = false;

                if (c == 0) { color = new Color(0.659f, 0.792f, 0.855f, 0.95f); font = NUM; }
                if (c == 1) { color = new Color(0.749f, 0.878f, 0.949f, 0.95f); font = NUM; }
                if (c == 2 || c == 4) color = new Color(0.749f, 0.878f, 0.949f, 0.88f);
                if (c == 5) bold = true;
                if (c == 6) color = sc;

                Tx(content, "qdTd" + i + "_" + c, row[c], 9, color,
                   colX[c], y, colW[c], rowH, anchor, font, bold);
            }

            if (i < Rows.Length - 1)
                Img(content, "qdTrLine" + i, 0f, y + rowH - 1f, innerW, 1f,
                    new Color(0.494f, 0.804f, 0.961f, 0.055f));
        }

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.verticalNormalizedPosition = 1f;

        TableAutoScroll auto = scrollRt.gameObject.AddComponent<TableAutoScroll>();
        auto.scrollRect = scroll;
        auto.pixelsPerSecond = 16f;
        auto.bottomPause = 3f;
        auto.topPause = 1f;

        HLine(r, "qdTableFootLine", innerX, h - 20f, innerW, GridLine);
        Text foot = Tx(r, "qdTableFoot",
            "共 128 批  ·  合格 <color=#35E0C0>126</color>  ·  复检 <color=#F5BF5E>2</color>  ·  不合格 <color=#FF7A84>0</color>",
            8, Muted, innerX, h - 18f, innerW, 12f, TextAnchor.MiddleLeft, CN);
        foot.supportRichText = true;
    }

    // ------------------------------------------------------------------ 右 2：工段 × 检测项矩阵

    static void BuildMatrixCard(RectTransform card, RectTransform canvas,
                                float x, float top, float w, float h,
                                string title, string en, string pill)
    {
        RectTransform r = Frame(card, canvas, x, top, w, h, title, en, pill, Cyan);

        string[] rows = { "原料前处理", "一混配料发酵", "二混配料混合", "烘干包装码垛", "动力控制环保" };
        string[] cols = { "水分", "均匀度", "粒度", "粉尘", "袋重", "温度" };
        float[,] data =
        {
            { 99.6f, 99.2f, 98.8f, 99.4f, 99.1f, 99.5f },
            { 98.9f, 98.6f, 98.4f, 99.0f, 98.8f, 99.2f },
            { 99.2f, 99.4f, 99.0f, 98.9f, 99.3f, 99.1f },
            { 96.4f, 95.8f, 97.2f, 96.9f, 95.2f, 96.6f },
            { 99.8f, 99.6f, 99.4f, 99.7f, 99.5f, 99.9f },
        };

        const float labelW = 100f, cellW = 66f, cellH = 34f, gap = 2f;
        float gx = 14f + labelW + 6f;

        for (int c = 0; c < 6; c++)
            Tx(r, "qdMxHead" + c, cols[c], 8, Muted, gx + c * (cellW + gap), 50f, cellW, 14f,
               TextAnchor.MiddleCenter, CN);

        for (int i = 0; i < 5; i++)
        {
            float y = 68f + i * 40f;
            Tx(r, "qdMxRow" + i, rows[i], 9, Ink, 14f, y + 8f, labelW, 18f, TextAnchor.MiddleLeft, CN);

            for (int c = 0; c < 6; c++)
            {
                float v = data[i, c];
                Color fillColor = v >= 99f ? new Color(0.212f, 0.898f, 0.729f, 0.34f)
                                : v >= 97f ? new Color(0.318f, 0.847f, 0.965f, 0.28f)
                                : v >= 96f ? new Color(0.961f, 0.757f, 0.376f, 0.32f)
                                           : new Color(1f, 0.490f, 0.537f, 0.34f);
                Color textColor = v >= 97f ? Ink : (v >= 96f ? Warn : Red);

                UIRoundRect cell = RR(r, "qdMxCell" + i + "_" + c, gx + c * (cellW + gap), y,
                                      cellW, cellH, fillColor, 3f, Color.clear, 0f);
                cell.Refresh();

                Tx(r, "qdMxTxt" + i + "_" + c, v.ToString("0.0"), 10, textColor,
                   gx + c * (cellW + gap), y, cellW, cellH, TextAnchor.MiddleCenter, NUM);
            }
        }

        float lgY = 274f;
        string[] legend = { "≥99.0", "97.0-99.0", "96.0-97.0", "<96.0" };
        Color[] legendColors =
        {
            new Color(0.212f, 0.898f, 0.729f, 0.60f),
            new Color(0.318f, 0.847f, 0.965f, 0.55f),
            new Color(0.961f, 0.757f, 0.376f, 0.60f),
            new Color(1f, 0.490f, 0.537f, 0.60f)
        };
        float lgx = 14f;
        for (int i = 0; i < legend.Length; i++)
        {
            UIRoundRect chip = RR(r, "qdMxLg" + i, lgx, lgY + 3f, 9f, 9f, legendColors[i], 2f, Color.clear, 0f);
            chip.Refresh();
            Tx(r, "qdMxLgTx" + i, legend[i], 8, Muted, lgx + 13f, lgY, 56f, 15f, TextAnchor.MiddleLeft, NUM);
            lgx += 96f;
        }

        HLine(r, "qdMxDiv", 14f, 292f, w - 28f, GridLine);
        Text foot = Tx(r, "qdMxFoot",
            "矩阵均值 <color=#EAF6FF>98.5%</color>  ·  低于 97% 单元格 <color=#FF7A84>4 个</color>（烘干包装码垛）  ·  较上周 +0.3pct",
            9, Muted, 14f, 300f, w - 28f, 14f, TextAnchor.MiddleLeft, CN);
        foot.supportRichText = true;
    }

    static UILineGraphic AddLine(Transform p, string n, List<Vector2> pts, float thickness, float dot, Color color)
    {
        RectTransform rt = Nd(p, n, 0f, 0f, 100f, 100f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.gameObject.AddComponent<CanvasRenderer>();
        var line = rt.gameObject.AddComponent<UILineGraphic>();
        line.color = color;
        line.raycastTarget = false;
        line.SetData(pts, thickness, dot, color);
        return line;
    }

    // ------------------------------------------------------------------ 图元

    static RectTransform Nd(Transform parent, string name, float x, float top, float w, float h)
    {
        return DashUI.New(name, parent as RectTransform, x, -top, w, h).GetComponent<RectTransform>();
    }

    static RectTransform Img(Transform p, string n, float x, float top, float w, float h, Color c)
    {
        return DashUI.Img(p as RectTransform, n, x, -top, w, h, c, null);
    }

    static void HLine(Transform p, string n, float x, float top, float w, Color c)
    {
        Img(p, n, x, top, w, 1f, c);
    }

    static void Dot(Transform p, string n, float cx, float top, float r, Color c)
    {
        Img(p, n, cx - r, top - r, r * 2f, r * 2f, c);
    }

    static Text Tx(Transform p, string n, string s, int fs, Color c,
                   float x, float top, float w, float h,
                   TextAnchor anchor = TextAnchor.MiddleLeft, Font font = null, bool bold = false)
    {
        RectTransform rt = Nd(p, n, x, top, w, h);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font != null ? font : CN;
        t.fontSize = Mathf.RoundToInt(fs * TextScale);
        t.text = s;
        t.color = c;
        t.alignment = anchor;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        t.raycastTarget = false;
        return t;
    }

    static float TextW(string s, int size)
    {
        Font f = NUM;
        if (string.IsNullOrEmpty(s) || f == null) return 0f;
        if (f.dynamic)
        {
            try { f.RequestCharactersInTexture(s, size, FontStyle.Normal); } catch { }
        }
        float width = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            CharacterInfo info;
            if (f.GetCharacterInfo(s[i], out info, size)) width += info.advance;
            else width += size * 0.55f;
        }
        return width;
    }

    static float PillWidth(string s, int fs)
    {
        float width = 0f;
        for (int i = 0; i < s.Length; i++)
            width += s[i] < 128 ? fs * 0.56f : fs * 1.02f;
        return width;
    }

    static void Pill(Transform p, string n, string text, float x, float top, float w, float h, Color accent)
    {
        UIRoundRect bg = RR(p, n, x, top, w, h, PillFill, h * 0.5f,
                            new Color(accent.r, accent.g, accent.b, 0.45f), 1f);
        bg.Refresh();
        Tx(p, n + "Tx", text, 9, accent, x, top, w, h, TextAnchor.MiddleCenter, CN);
    }

    static UIRoundRect RR(Transform p, string n, float x, float top, float w, float h,
                          Color fill, float radius, Color border, float borderW)
    {
        RectTransform rt = Nd(p, n, x, top, w, h);
        var rr = rt.gameObject.AddComponent<UIRoundRect>();
        rr.color = fill;
        rr.radius = radius;
        rr.borderColor = border;
        rr.borderWidth = borderW;
        rr.raycastTarget = false;
        return rr;
    }

    static UIRingGraphic Ring(Transform p, string n, float x, float top, float size,
                              float[] values, Color[] colors, float widthRatio,
                              float start, float sweep, float gap)
    {
        RectTransform rt = Nd(p, n, x, top, size, size);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var ring = rt.gameObject.AddComponent<UIRingGraphic>();
        ring.SetData(values, colors, widthRatio, start, sweep, gap);
        ring.raycastTarget = false;
        return ring;
    }
}
