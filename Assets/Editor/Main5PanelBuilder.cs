using System;
using System.Collections.Generic;
using System.Globalization;
using Daima;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>main5 面板 / 导航 / tooltip / 运行时接线。</summary>
public static partial class Main5SafetyEnergyBuilder
{
    // ================================================================= 右侧页面

    static void BuildPanelPage(RectTransform content, Section s, int index,
        out CanvasGroup statsGroup, out GameObject tableView,
        out UIRoundRect tableBtn, out Text tableBtnLabel)
    {
        fontScale = 1.5f;

        const float W = PANEL_INNER_W;
        const float X = PANEL_PAD;
        const float RIGHT = X + W;

        // ---------- 页头 ----------
        Text title = TxCN(content, "mPanelTitle", X, 10f, 190f, 40f, "工段安环能耗", 19,
            INK, TextAnchor.MiddleLeft, true);
        UILetterSpacing titleLs = title.gameObject.AddComponent<UILetterSpacing>();
        titleLs.SetSpacing(2.4f, false);

        TxDisplay(content, "mPanelEn", 208f, 18f, 200f, 24f, "SECTION STATISTICS",
            FS(10f), new Color(0.435f, 0.827f, 1f, 0.9f), TextAnchor.MiddleLeft, 1.5f);

        float tagW = Mathf.Max(64f, TextWidth(s.Full, CN, FS(11f)) + 20f);
        float tagX = RIGHT - tagW;
        RR(content, "mPanelTag", tagX, 12f, tagW, 30f, new Color(0.055f, 0.180f, 0.259f, 0.55f), 4f,
            new Color(0.310f, 0.788f, 0.941f, 0.45f), 1f);
        TxCN(content, "mPanelTagText", tagX, 12f, tagW, 30f, s.Full, FS(11f),
            Hex("#7fd8ff"), TextAnchor.MiddleCenter);

        HLine(content, "mPanelHeadLine", X, HEAD_BOTTOM, W,
            new Color(0.494f, 0.804f, 0.961f, 0.16f));

        // ---------- 故障条 ----------
        UIRoundRect faultBg = RR(content, "mFault", X, FAULT_TOP, W, FAULT_H,
            new Color(1f, 0.329f, 0.439f, 0.14f), 4f,
            new Color(1f, 0.420f, 0.494f, 0.36f), 1f);
        faultBg.gradient = true;
        faultBg.verticalGradient = false;
        faultBg.color2 = new Color(1f, 0.329f, 0.439f, 0.035f);
        faultBg.Refresh();
        Img(content, "mFaultLeftBar", X, FAULT_TOP, 2f, FAULT_H, DANGER);

        RectTransform iconRoot = DashUI.New("mFaultIcon", content, 30f, -(FAULT_TOP + 5f), 20f, 20f)
            .GetComponent<RectTransform>();
        CanvasGroup iconGroup = iconRoot.gameObject.AddComponent<CanvasGroup>();
        UIRoundRect iconBg = RR(iconRoot, "mFaultIconBg", 0f, 0f, 20f, 20f,
            DANGER, 10f, Color.clear, 0f);
        TxNum(iconRoot, "mFaultIconMark", 0f, 0f, 20f, 20f, "!", FS(11f), Hex("#2a0710"),
            TextAnchor.MiddleCenter, true);
        if (iconGroup == null) { }

        RR(content, "mFaultTag", 60f, FAULT_TOP + 8f, 100f, 20f, Hex("#b4233a"), 2f,
            Color.clear, 0f);
        TxCN(content, "mFaultTagText", 60f, FAULT_TOP + 8f, 100f, 20f, "全厂故障报警",
            FS(9.5f), Color.white, TextAnchor.MiddleCenter, true);

        TxCN(content, "mFaultDevice", 168f, FAULT_TOP + 7.5f, 120f, 22f, "2# 投料除尘",
            FS(10.5f), Hex("#ffb3be"), TextAnchor.MiddleLeft, true);

        RR(content, "mFaultStatus", 292f, FAULT_TOP + 8f, 48f, 20f, new Color(0, 0, 0, 0), 2f,
            new Color(1f, 0.420f, 0.494f, 0.5f), 1f);
        TxCN(content, "mFaultStatusText", 292f, FAULT_TOP + 8f, 48f, 20f, "未处理",
            FS(8.5f), Hex("#ff93a2"), TextAnchor.MiddleCenter);

        TxNum(content, "mFaultTime", RIGHT - 70f, FAULT_TOP + 8f, 70f, 20f, "11:06", FS(8.5f),
            Hex("#92b4c7"), TextAnchor.MiddleRight);
        TxCN(content, "mFaultDesc", 60f, FAULT_TOP + 34f, W - 68f, 20f,
            "风机轴承温度 82 ℃（阈值 75 ℃），设备仍运行，建议检查润滑与冷却",
            FS(9.5f), Hex("#dcc4c9"), TextAnchor.MiddleLeft);

        // ---------- 六项指标 ----------
        RectTransform stats = DashUI.New("mStats", content, X, -STATS_TOP, W, STATS_H)
            .GetComponent<RectTransform>();
        statsGroup = stats.gameObject.AddComponent<CanvasGroup>();
        statsGroup.alpha = 1f;

        const float itemW = 163.98f;
        const float colGap = 14f;

        for (int i = 0; i < s.Stats.Length && i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;
            float itemX = col * (itemW + colGap);
            float itemTop = row * 78f;

            UIRoundRect accent = RR(stats, "mStatAccent" + i, itemX, itemTop + 2f,
                2.5f, 72f, ACCENT, 0f, Color.clear, 0f);
            accent.gradient = true;
            accent.verticalGradient = true;
            accent.color2 = new Color(0.310f, 0.788f, 0.941f, 0.05f);
            accent.Refresh();

            TxCN(stats, "mStatK" + i, itemX + 10f, itemTop, itemW - 10f, 20f,
                s.Stats[i].Label, FS(10.5f), Hex("#9dbccd"));
            ValueUnit(stats, "mStatV" + i, itemX + 10f, itemTop + 20f, itemW - 6f, 40f,
                s.Stats[i].Value, s.Stats[i].Unit, 20, FS(9.5f),
                Hex("#f2fbff"), Hex("#92b4c7"));
        }

        HLine(content, "mStatsLine", X, STATS_TOP + STATS_H, W,
            new Color(0.494f, 0.804f, 0.961f, 0.12f));

        // ---------- 图表标题 ----------
        TxRich(content, "mChartCaption", X, CHART_HEAD_TOP + 4.5f, 420f, 26f,
            "<color=#EAF6FF><b>主要设备电耗</b></color>（kWh）· 本月 / 上月",
            FS(11f), Hex("#b6d3e3"));

        RectTransform btnRoot = DashUI.New("mTableBtn", content, RIGHT - 72f, -CHART_HEAD_TOP,
            72f, 30f).GetComponent<RectTransform>();
        tableBtn = btnRoot.gameObject.AddComponent<UIRoundRect>();
        tableBtn.color = new Color(0.055f, 0.180f, 0.259f, 0.50f);
        tableBtn.radius = 4f;
        tableBtn.borderColor = new Color(0.310f, 0.788f, 0.941f, 0.40f);
        tableBtn.borderWidth = 1f;
        tableBtn.raycastTarget = true;
        Main5PointerRelay relay = btnRoot.gameObject.AddComponent<Main5PointerRelay>();
        relay.zone = 200 + index;

        tableBtnLabel = TxCN(btnRoot, "mTableBtnLabel", 0f, 0f, 72f, 30f,
            "数据表", FS(10f), Hex("#7fd8ff"), TextAnchor.MiddleCenter);

        // ---------- 图表区 ----------
        RectTransform wrap = DashUI.New("mChartWrap", content, X, -CHART_WRAP_TOP,
            W, CHART_WRAP_H).GetComponent<RectTransform>();

        BuildMainChart(wrap, s, index);
        BuildMixBlock(wrap, s, index);
        BuildChangeBlock(wrap, s, index);

        BuildTableView(wrap, s, index, out tableView);
        if (tableView != null) tableView.transform.SetAsLastSibling();

        // ---------- 页脚 ----------
        HLine(content, "mPanelFootLine", X, 894f, W,
            new Color(0.494f, 0.804f, 0.961f, 0.12f));
        TxCN(content, "mPanelNote", X, 900.51f, 330f, 20f, s.Note, FS(9.5f),
            Hex("#8fb0c4"));
        TxCN(content, "mPanelFootRight", RIGHT - 180f, 900.51f, 180f, 20f,
            "电力计量 · 本月累计", FS(9.5f),
            new Color(0.490f, 0.647f, 0.745f, 0.9f), TextAnchor.MiddleRight);

        fontScale = 1f;
    }

    // ================================================================= 右侧导航

    static void BuildNav(RectTransform right, out UIRoundRect[] navBg, out Image[] navAccent,
        out Text[] navIdx, out Text[] navLabels, out RectTransform[] navRects,
        out CanvasGroup[] navGroups)
    {
        RectTransform nav = DashUI.Child(right, "SectionButtons");
        if (nav == null)
        {
            var go = new GameObject("SectionButtons", typeof(RectTransform));
            go.layer = 5;
            nav = go.GetComponent<RectTransform>();
            nav.SetParent(right, false);
            Undo.RegisterCreatedObjectUndo(go, "创建 main5 导航");
        }

        foreach (MonoBehaviour mb in nav.GetComponents<MonoBehaviour>())
        {
            string tn = mb.GetType().Name;
            if (tn == "SectionPageSwitcher" || tn == "SectionTabButton")
                Undo.DestroyObjectImmediate(mb);
        }

        StripChildren(nav, false);

        nav.anchorMin = new Vector2(0f, 1f);
        nav.anchorMax = new Vector2(0f, 1f);
        nav.pivot = new Vector2(0f, 1f);
        nav.anchoredPosition = new Vector2(1203f, -80.41f);
        nav.sizeDelta = new Vector2(132f, 941.52f);

        int n = NAV_LABELS.Length;
        navBg = new UIRoundRect[n];
        navAccent = new Image[n];
        navIdx = new Text[n];
        navLabels = new Text[n];
        navRects = new RectTransform[n];
        navGroups = new CanvasGroup[n];

        const float firstTop = 247.26f;
        const float step = 67f;

        for (int i = 0; i < n; i++)
        {
            RectTransform item = DashUI.New("button" + (i + 1), nav, 0f,
                -(firstTop + i * step), 132f, 58f).GetComponent<RectTransform>();
            navRects[i] = item;

            CanvasGroup cg = item.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            navGroups[i] = cg;

            UIRoundRect bg = item.gameObject.AddComponent<UIRoundRect>();
            bg.color = new Color(0, 0, 0, 0);
            bg.color2 = new Color(0, 0, 0, 0);
            bg.gradient = true;
            bg.radius = 6f;
            bg.borderColor = new Color(0.494f, 0.804f, 0.961f, 0.20f);
            bg.borderWidth = 1f;
            bg.raycastTarget = true;
            navBg[i] = bg;

            Main5PointerRelay relay = item.gameObject.AddComponent<Main5PointerRelay>();
            relay.zone = 100 + i;

            navAccent[i] = Img(item, "navAccent", 0f, 13f, 2f, 32f,
                new Color(0.310f, 0.788f, 0.941f, 0.32f)).GetComponent<Image>();

            navIdx[i] = TxDisplay(item, "navIdx", 11f, 20.5f, 18f, 17f,
                (i + 1).ToString("00"), 14, new Color(0.310f, 0.788f, 0.941f, 0.68f),
                TextAnchor.MiddleLeft, 0.5f);

            navLabels[i] = TxCN(item, "navLabel", 36f, 21.5f, 86f, 15f,
                NAV_LABELS[i], FS(12f), Hex("#bed9e8"));
        }

        nav.SetAsLastSibling();
    }

    // ================================================================= tooltip

    static Main5Tooltip BuildTooltip(RectTransform canvas, string name, float textScale)
    {
        fontScale = textScale;

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = 5;
        RectTransform root = go.GetComponent<RectTransform>();
        root.SetParent(canvas, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(150f * textScale, 60f * textScale);
        Undo.RegisterCreatedObjectUndo(go, "创建 main5 tooltip");

        UIRoundRect bg = DashUI.New("bg", root, 0f, 0f, 150f * textScale, 60f * textScale)
            .GetComponent<RectTransform>().gameObject.AddComponent<UIRoundRect>();
        bg.color = new Color(0.027f, 0.078f, 0.125f, 0.96f);
        bg.radius = 6f * textScale;
        bg.borderColor = new Color(0.494f, 0.804f, 0.961f, 0.35f);
        bg.borderWidth = 1f;
        bg.raycastTarget = false;

        Main5Tooltip tip = go.AddComponent<Main5Tooltip>();
        tip.root = root;
        tip.background = bg;
        tip.uiScale = textScale;
        tip.title = TxCN(root, "title", 11f * textScale, 8f * textScale,
            110f * textScale, 18f * textScale, "", FS(11f), Hex("#eaf6ff"));

        tip.rowLabels = new Text[4];
        tip.rowValues = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            tip.rowLabels[i] = TxCN(root, "rowK" + i, 11f * textScale, 28f * textScale,
                70f * textScale, 19f * textScale, "", FS(10.5f), Hex("#a9c9db"));
            tip.rowValues[i] = TxNum(root, "rowV" + i, 80f * textScale, 28f * textScale,
                60f * textScale, 19f * textScale, "", FS(10.5f), Hex("#eaf6ff"));
        }

        fontScale = 1f;
        go.SetActive(false);
        return tip;
    }

    // ================================================================= 运行时接线

    static Main5Dashboard WireDashboard(RectTransform main5,
        RectTransform[] pages, CanvasGroup[] pageGroups, CanvasGroup[] statsGroups,
        UIRoundRect[] navBg, Image[] navAccent, Text[] navIdx, Text[] navLabels,
        RectTransform[] navRects, CanvasGroup[] navGroups,
        UIRoundRect[] tableBtns, Text[] tableBtnLabels, GameObject[] tableViews,
        Main5Tooltip leftTip, Main5Tooltip rightTip,
        RectTransform leftCanvas, RectTransform rightCanvas,
        CanvasGroup[] leftContents, RectTransform[] leftContentRects,
        RectTransform[] bulletFills, RectTransform energyMask,
        RectTransform energyPointer, RectTransform energyZone,
        CanvasGroup headerGroup, CanvasGroup footerGroup,
        RectTransform headerRect, RectTransform footerRect)
    {
        if (main5 == null) return null;

        Main5Dashboard dash = main5.GetComponent<Main5Dashboard>();
        if (dash == null) dash = Undo.AddComponent<Main5Dashboard>(main5.gameObject);

        dash.pageRoots = ToGOs(pages);
        dash.pageContents = pageGroups;
        dash.statsGroups = statsGroups;

        dash.navBg = navBg;
        dash.navAccent = navAccent;
        dash.navIdx = navIdx;
        dash.navLabels = navLabels;
        dash.navRects = navRects;
        dash.navGroups = navGroups;

        dash.tableBtns = tableBtns;
        dash.tableBtnLabels = tableBtnLabels;
        dash.tableViews = tableViews;

        dash.tooltipLeft = leftTip;
        dash.tooltipRight = rightTip;
        dash.leftCanvasRect = leftCanvas;
        dash.rightCanvasRect = rightCanvas;

        dash.leftContents = leftContents;
        dash.leftContentRects = leftContentRects;
        dash.bulletFills = bulletFills;
        dash.energyMask = energyMask;
        dash.energyPointer = energyPointer;
        dash.energyZone = energyZone;

        dash.headerGroup = headerGroup;
        dash.footerGroup = footerGroup;
        dash.headerRect = headerRect;
        dash.footerRect = footerRect;

        dash.pageData = new Main5PageData[Sections.Length];
        for (int i = 0; i < Sections.Length; i++)
        {
            Section s = Sections[i];
            var data = new Main5PageData();
            data.devices = new Main5DeviceInfo[s.Devices.Length];
            for (int k = 0; k < s.Devices.Length; k++)
            {
                data.devices[k] = new Main5DeviceInfo();
                data.devices[k].name = s.Devices[k].Name;
                data.devices[k].now = s.Devices[k].Now;
                data.devices[k].prev = s.Devices[k].Prev;
            }
            dash.pageData[i] = data;
        }

        dash.energy = new Main5EnergyData();
        dash.energy.days = ENERGY_DAYS;
        dash.energy.values = ENERGY_VALUES;
        dash.activeIndex = 0;

        EditorUtility.SetDirty(dash);
        return dash;
    }

    static GameObject[] ToGOs(RectTransform[] rects)
    {
        GameObject[] result = new GameObject[rects.Length];
        for (int i = 0; i < rects.Length; i++)
            result[i] = rects[i] != null ? rects[i].gameObject : null;
        return result;
    }
}
