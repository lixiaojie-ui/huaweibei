using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Daima;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Canvas main5（安环能耗）构建器：按 安环能耗.html 的最终静态样式重建
/// 左侧 3 张卡片、顶栏 / 页脚、右侧 6 个工段页面与导航。
/// 运行时动画 / 悬停 / tooltip / 表格由 Main5Dashboard 负责。
/// </summary>
public static partial class Main5SafetyEnergyBuilder
{
    const float PAGE_W = 555.962f;
    const float PAGE_H = 941.5189f;
    const float PAGE_X = 668.9439f;
    const float PAGE_Y = -11.1897f;

    const float LEFT_W = 565.7588f;
    const float CARD2_TOP = 430.43f;
    const float CARD3_TOP = 734.63f;
    const float CARD_H = 249.6257f;
    const float CARD3_H = 281.38f;

    // 右侧面板内部（页面坐标）
    const float PANEL_PAD = 18f;
    const float PANEL_INNER_W = 519.95f;
    const float HEAD_BOTTOM = 62f;
    const float FAULT_TOP = 70f;
    const float FAULT_H = 58f;
    const float STATS_TOP = 136f;
    const float STATS_H = 160f;
    const float CHART_HEAD_TOP = 300f;
    const float CHART_WRAP_TOP = 330f;
    const float CHART_WRAP_H = 554f;
    const float WRAP_MAIN_H = 252f;
    const float WRAP_MIX_TOP = 258f;
    const float WRAP_MIX_H = 96f;
    const float WRAP_CHANGE_TOP = 346f;
    const float WRAP_CHANGE_H = 208f;

    // 图表绘图区（相对各图表容器）
    const float MAIN_GRID_L = 100f, MAIN_GRID_R = 48f, MAIN_GRID_T = 32f, MAIN_GRID_B = 26f;
    const float CHANGE_GRID_L = 102f, CHANGE_GRID_R = 54f, CHANGE_GRID_T = 6f, CHANGE_GRID_B = 6f;

    // 左 02 折线绘图区（相对卡片）
    const float ENERGY_PLOT_L = 58f, ENERGY_PLOT_T = 63f;
    const float ENERGY_PLOT_W = 467.75f, ENERGY_PLOT_H = 125.63f;

    static Color Hex(string h)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(h, out c)) return c;
        return Color.white;
    }

    static readonly Color INK = Hex("#eaf6ff");
    static readonly Color MUTED = Hex("#9dbccd");
    static readonly Color MUTED2 = Hex("#bed9e8");
    static readonly Color ACCENT = Hex("#4fc9f0");
    static readonly Color ACCENT2 = Hex("#7fd8ff");
    static readonly Color GREEN_TEXT = Hex("#6fe3ae");
    static readonly Color GREEN_DOT = Hex("#46d69b");
    static readonly Color DANGER = Hex("#ff6b7e");
    static readonly Color WARN = Hex("#ffb13b");
    static readonly Color SERIES1 = Hex("#3987e5");
    static readonly Color SERIES2 = Hex("#a9c6d8");

    static readonly Color[] MIX_COLORS =
    {
        Hex("#2b5c80"), Hex("#2f7ca4"), Hex("#2f9abb"),
        Hex("#4cbcd6"), Hex("#7fe2ff"), Hex("#245f7d")
    };

    static readonly string[] NAV_LABELS =
    {
        "全厂总览", "原料与前处理", "一混配料发酵",
        "二混配料混合", "烘干包装码垛", "动力控制环保"
    };

    static readonly string[] ENERGY_DAYS =
    {
        "11-04", "11-05", "11-06", "11-07", "11-08", "11-09", "11-10"
    };

    static readonly float[] ENERGY_VALUES =
    {
        17980f, 18640f, 18220f, 19060f, 19340f, 18760f, 18920f
    };

    static readonly Section[] Sections =
    {
        new Section("全厂总览", "全厂总览", "统计口径：本月 1-10 日 · 5 大工段",
            new[]
            {
                new Stat("设备总数", "31", "台"),
                new Stat("运行设备", "28", "台"),
                new Stat("本月用电", "187,000", "kWh"),
                new Stat("本月天然气", "38,600", "m³"),
                new Stat("粉尘 TSP", "0.62", "mg/m³"),
                new Stat("厂界噪声", "56", "dB(A)")
            },
            new[]
            {
                new Device("原料前处理", 48500, 46200),
                new Device("一混发酵", 40200, 38200),
                new Device("二混混合", 38000, 36400),
                new Device("烘干包装", 27400, 26100),
                new Device("动力环保", 32900, 31400)
            }),

        new Section("原料与前处理", "原料与前处理段", "2 套集尘罩 + 3 套脉冲布袋",
            new[]
            {
                new Stat("设备总数", "8", "台"),
                new Stat("运行设备", "7", "台"),
                new Stat("本月用电", "48,500", "kWh"),
                new Stat("粉尘 TSP", "0.72", "mg/m³"),
                new Stat("设备噪声", "76", "dB(A)"),
                new Stat("除尘设施", "3", "套")
            },
            new[]
            {
                new Device("一混粉碎机", 18600, 17900),
                new Device("预混料粉碎机", 12400, 11800),
                new Device("一混提升仓", 8200, 8000),
                new Device("清理筛", 4600, 4400),
                new Device("料筒仓", 3800, 3600),
                new Device("永磁筒", 900, 850)
            }),

        new Section("一混配料发酵", "一混配料与发酵段", "配料仓密闭 + 1 套脉冲布袋",
            new[]
            {
                new Stat("设备总数", "7", "台"),
                new Stat("运行设备", "6", "台"),
                new Stat("本月用电", "40,200", "kWh"),
                new Stat("发酵温度", "38.6", "℃"),
                new Stat("臭气浓度", "12", "无量纲"),
                new Stat("设备噪声", "72", "dB(A)")
            },
            new[]
            {
                new Device("一混翻料电机", 16800, 15900),
                new Device("液体发酵罐", 9600, 9200),
                new Device("一混发酵床", 6400, 6100),
                new Device("一混配料秤", 3200, 3000),
                new Device("一混料筒仓", 2400, 2300),
                new Device("接种系统", 1800, 1700)
            }),

        new Section("二混配料混合", "二混配料与混合段", "混合粉尘密闭收集回用",
            new[]
            {
                new Stat("设备总数", "5", "台"),
                new Stat("运行设备", "5", "台"),
                new Stat("本月用电", "38,000", "kWh"),
                new Stat("粉尘 TSP", "0.58", "mg/m³"),
                new Stat("设备噪声", "78", "dB(A)"),
                new Stat("混合均匀度", "96.2", "%")
            },
            new[]
            {
                new Device("二混混合机", 14200, 13600),
                new Device("预混料生产线", 11600, 11100),
                new Device("二混刮板机", 5400, 5200),
                new Device("二混配料秤", 3800, 3600),
                new Device("二混料筒仓", 3000, 2900)
            }),

        new Section("烘干包装码垛", "烘干、包装与码垛段", "天然气烘干 · 厌氧发酵异味控制",
            new[]
            {
                new Stat("设备总数", "5", "台"),
                new Stat("运行设备", "4", "台"),
                new Stat("本月用电", "27,400", "kWh"),
                new Stat("本月天然气", "38,600", "m³"),
                new Stat("排风温度", "68", "℃"),
                new Stat("设备噪声", "74", "dB(A)")
            },
            new[]
            {
                new Device("天然气烘干机", 9800, 9400),
                new Device("固体发酵包装线", 7600, 7200),
                new Device("包装机器人", 4200, 4000),
                new Device("机器人码垛", 3600, 3400),
                new Device("成品库", 2200, 2100)
            }),

        new Section("动力控制环保", "动力、控制与环保段", "除尘风机与环保设施集中控制",
            new[]
            {
                new Stat("设备总数", "6", "台"),
                new Stat("运行设备", "6", "台"),
                new Stat("本月用电", "32,900", "kWh"),
                new Stat("除尘效率", "99.2", "%"),
                new Stat("滤袋压差", "1.6", "kPa"),
                new Stat("设备噪声", "82", "dB(A)")
            },
            new[]
            {
                new Device("1# 投料除尘", 3800, 3600),
                new Device("2# 投料除尘", 3600, 3500),
                new Device("配料仓除尘", 4200, 4000),
                new Device("原料库除尘", 2900, 2800),
                new Device("成品区通风", 5600, 5300),
                new Device("除尘风机", 12800, 12200)
            })
    };

    [MenuItem("大屏/重建 main5 安环能耗")]
    public static void Build()
    {
        EnsureSceneLoaded();

        RectTransform left = DashUI.FindRoot("Canvas left5");
        RectTransform right = DashUI.FindRoot("Canvas right5");
        RectTransform main5 = DashUI.FindRoot("Canvas main5");
        if (left == null || right == null)
            throw new InvalidOperationException("找不到 Canvas left5 / Canvas right5，请先打开 SampleScene。");

        RectTransform l1 = DashUI.Child(left, "Image1");
        RectTransform l2 = DashUI.Child(left, "Image2");
        RectTransform l3 = DashUI.Child(left, "Image3");
        if (l1 == null || l2 == null || l3 == null)
            throw new InvalidOperationException("Canvas left5 下缺少 Image1 / Image2 / Image3。");

        // 左侧三张卡片
        CanvasGroup left1Group, left2Group, left3Group;
        RectTransform left1Rect, left2Rect, left3Rect;
        RectTransform energyMask, energyPointer, energyZone;

        var c1 = PrepareLeftCard(l1, out left1Group, out left1Rect);
        var c2 = PrepareLeftCard(l2, out left2Group, out left2Rect);
        var c3 = PrepareLeftCard(l3, out left3Group, out left3Rect);
        RectTransform[] bulletFills;
        BuildLeftSafety(c1, out bulletFills);
        BuildLeftEnergy(c2, out energyMask, out energyPointer, out energyZone);
        BuildLeftFacilities(c3);

        // 顶栏 / 页脚
        CanvasGroup headerGroup, footerGroup;
        RectTransform headerRect, footerRect;
        BuildChrome(left, out headerGroup, out footerGroup, out headerRect, out footerRect);

        // 右侧 6 个页面
        RectTransform basePage = DashUI.Child(right, "Image1");
        if (basePage == null)
            throw new InvalidOperationException("Canvas right5 下缺少 Image1。");

        RectTransform[] pages = new RectTransform[6];
        pages[0] = basePage;
        for (int i = 1; i < pages.Length; i++)
        {
            RectTransform page = DashUI.Child(right, "Image" + (i + 1));
            if (page == null) page = DuplicatePage(basePage, right, "Image" + (i + 1));
            pages[i] = page;
        }

        CanvasGroup[] pageGroups = new CanvasGroup[6];
        CanvasGroup[] statsGroups = new CanvasGroup[6];
        GameObject[] tableViews = new GameObject[6];
        UIRoundRect[] tableBtns = new UIRoundRect[6];
        Text[] tableBtnLabels = new Text[6];

        for (int i = 0; i < pages.Length; i++)
        {
            UnpackIfNeeded(pages[i].gameObject);
            RectTransform content = PreparePage(pages[i], out pageGroups[i]);
            BuildPanelPage(content, Sections[i], i,
                out statsGroups[i], out tableViews[i], out tableBtns[i], out tableBtnLabels[i]);
        }

        // 右侧导航（替换旧按钮组）
        UIRoundRect[] navBg;
        Image[] navAccent;
        Text[] navIdx;
        Text[] navLabels;
        RectTransform[] navRects;
        CanvasGroup[] navGroups;
        BuildNav(right, out navBg, out navAccent, out navIdx, out navLabels, out navRects, out navGroups);

        // tooltip
        Main5Tooltip leftTip = BuildTooltip(left, "Main5TooltipLeft", 1f);
        Main5Tooltip rightTip = BuildTooltip(right, "Main5TooltipRight", 1.5f);
        leftTip.canvasRect = left;
        rightTip.canvasRect = right;
        leftTip.Hide();
        rightTip.Hide();

        // 运行时控制器
        Main5Dashboard dash = WireDashboard(main5, pages, pageGroups, statsGroups,
            navBg, navAccent, navIdx, navLabels, navRects, navGroups,
            tableBtns, tableBtnLabels, tableViews,
            leftTip, rightTip, left, right,
            new[] { left1Group, left2Group, left3Group },
            new[] { left1Rect, left2Rect, left3Rect },
            bulletFills, energyMask, energyPointer, energyZone,
            headerGroup, footerGroup, headerRect, footerRect);

        // 预制体
        for (int i = 0; i < pages.Length; i++)
            SavePrefab(pages[i].gameObject, "Assets/Prefabs/Image" + (i + 1) + ".prefab");
        for (int i = 0; i < navRects.Length; i++)
            SavePrefab(navRects[i].gameObject, "Assets/Prefabs/button" + (i + 1) + ".prefab");

        for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(i == 0);
        if (navRects.Length > 0) navRects[0].parent.SetAsLastSibling();

        Scene scene = right.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[main5] 安环能耗页重建完成（HTML 静态样式 + 运行时入场动画）" + (dash != null ? "" : "；未找到 Canvas main5，运行时控制器未挂载"));
    }

    static void EnsureSceneLoaded()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && !string.IsNullOrEmpty(active.name) && active.isLoaded &&
            active.path == "Assets/Scenes/SampleScene.unity")
            return;

        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
    }

    // ================================================================= 左侧卡片

    static RectTransform PrepareLeftCard(RectTransform card, out CanvasGroup group, out RectTransform contentRect)
    {
        StripChildren(card, true);

        Image bg = card.GetComponent<Image>();
        if (bg != null)
        {
            Undo.RecordObject(bg, "main5 卡片透明");
            Color c = bg.color;
            c.a = 0f;
            bg.color = c;
            bg.raycastTarget = false;
            EditorUtility.SetDirty(bg);
        }

        RectTransform content = EnsureContent(card);
        group = content.GetComponent<CanvasGroup>();
        contentRect = content;
        return content;
    }

    static RectTransform PreparePage(RectTransform page, out CanvasGroup contentGroup)
    {
        page.anchorMin = new Vector2(0.5f, 0.5f);
        page.anchorMax = new Vector2(0.5f, 0.5f);
        page.pivot = new Vector2(0.5f, 0.5f);
        page.anchoredPosition = new Vector2(PAGE_X, PAGE_Y);
        page.sizeDelta = new Vector2(PAGE_W, PAGE_H);

        StripChildren(page, true);

        Image bg = page.GetComponent<Image>();
        if (bg != null)
        {
            Undo.RecordObject(bg, "main5 页面底色");
            bg.color = new Color(0.0549f, 0.1804f, 0.298f, 0f);
            bg.raycastTarget = false;
            EditorUtility.SetDirty(bg);
        }

        RectTransform content = EnsureContent(page);

        // 玻璃面板底 + 发光描边（按参考图）
        RectTransform glassRt = DashUI.New("mPanelGlass", page, 0f, 0f, PAGE_W, PAGE_H)
            .GetComponent<RectTransform>();
        UIRoundRect glass = glassRt.gameObject.AddComponent<UIRoundRect>();
        glass.color = new Color(0.016f, 0.047f, 0.086f, 0.72f);
        glass.color2 = new Color(0.102f, 0.290f, 0.361f, 0.60f);
        glass.gradient = true;
        glass.verticalGradient = true;
        glass.radius = 6f;
        glass.raycastTarget = false;
        glass.Refresh();
        glassRt.SetSiblingIndex(0);

        RectTransform edgeRt = DashUI.New("mPanelEdge", page, 0f, 0f, PAGE_W, PAGE_H)
            .GetComponent<RectTransform>();
        UIRoundRect edge = edgeRt.gameObject.AddComponent<UIRoundRect>();
        edge.color = new Color(0f, 0f, 0f, 0f);
        edge.radius = 6f;
        edge.borderColor = new Color(0.435f, 0.827f, 1f, 0.28f);
        edge.borderWidth = 1f;
        edge.raycastTarget = false;
        edge.Refresh();
        edgeRt.SetSiblingIndex(page.childCount - 1);

        // 顶边流光（参考 HTML panel::after）
        float glowW = (PAGE_W - 32f) * 0.5f;
        UIRoundRect glowL = RR(page, "mPanelGlowL", 16f, 0f, glowW, 1f,
            new Color(0.149f, 0.886f, 1f, 0f), 0f, Color.clear, 0f);
        glowL.gradient = true;
        glowL.verticalGradient = false;
        glowL.color2 = new Color(0.149f, 0.886f, 1f, 0.55f);
        glowL.raycastTarget = false;
        glowL.Refresh();
        UIRoundRect glowR = RR(page, "mPanelGlowR", 16f + glowW, 0f, glowW, 1f,
            new Color(0.180f, 0.902f, 0.659f, 0.40f), 0f, Color.clear, 0f);
        glowR.gradient = true;
        glowR.verticalGradient = false;
        glowR.color2 = new Color(0.180f, 0.902f, 0.659f, 0f);
        glowR.raycastTarget = false;
        glowR.Refresh();

        Img(page, "mPanelCornerH", 0f, 0f, 72f, 2f, new Color(0.435f, 0.827f, 1f, 0.55f));
        Img(page, "mPanelCornerV", 0f, 0f, 2f, 72f, new Color(0.435f, 0.827f, 1f, 0.55f));

        CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();
        if (pageGroup == null) pageGroup = Undo.AddComponent<CanvasGroup>(page.gameObject);
        pageGroup.alpha = 1f;

        contentGroup = content.GetComponent<CanvasGroup>();
        return content;
    }

    static RectTransform DuplicatePage(RectTransform source, RectTransform parent, string name)
    {
        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
        clone.name = name;
        clone.SetActive(true);
        Undo.RegisterCreatedObjectUndo(clone, "复制 main5 页面");

        RectTransform rt = clone.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(PAGE_X, PAGE_Y);
        rt.sizeDelta = new Vector2(PAGE_W, PAGE_H);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        return rt;
    }

    static RectTransform EnsureContent(RectTransform parent)
    {
        RectTransform content = DashUI.Child(parent, "Content");
        if (content == null)
        {
            var go = new GameObject("Content", typeof(RectTransform));
            go.layer = 5;
            content = go.GetComponent<RectTransform>();
            content.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "创建 main5 内容层");
        }
        else
        {
            DashUI.Clear(content);
        }

        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        content.localScale = Vector3.one;
        content.localRotation = Quaternion.identity;
        content.SetAsLastSibling();

        CanvasGroup group = content.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(content.gameObject);
        group.alpha = 1f;
        return content;
    }

    static void StripChildren(RectTransform parent, bool keepFrame)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (keepFrame && IsFrame(child.name)) continue;
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    static bool IsFrame(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name.IndexOf("frame", StringComparison.OrdinalIgnoreCase) >= 0 || name.StartsWith("框");
    }

    // ================================================================= 左 01 安环态势

    static void BuildLeftSafety(RectTransform p, out RectTransform[] bulletFills)
    {
        const float innerX = 16f;
        const float innerW = 533.75f;

        CardHead(p, "01", "SAFETY & ENV.", "实时");

        // 4 个安全格
        float tileW = 127.44f;
        float tileH = 58.39f;
        float gap = 8f;
        string[] labels = { "粉尘 TSP", "厂界噪声", "臭气浓度", "安全隐患" };
        string[] values = { "0.62", "56", "12", "0" };
        string[] units = { "mg/m³", "dB(A)", "无量纲", "项" };
        string[] states = { "达标", "达标", "达标", "正常" };

        for (int i = 0; i < 4; i++)
        {
            float x = innerX + i * (tileW + gap);
            const float top = 37f;
            RR(p, "mTile", x, top, tileW, tileH, new Color(0, 0, 0, 0), 3f,
                new Color(0.494f, 0.804f, 0.961f, 0.18f), 1f);

            TxCN(p, "mTileK", x + 10f, top + 7.5f, tileW - 20f, 14f, labels[i], FS(10.5f),
                Hex("#b6d3e3"), TextAnchor.MiddleLeft);
            TxCN(p, "mTileOk", x + 10f, top + 8f, tileW - 20f, 13f, states[i], FS(9.5f),
                GREEN_TEXT, TextAnchor.MiddleRight);
            ValueUnit(p, "mTileV", x + 10f, top + 24.5f, tileW - 20f, 27f,
                values[i], units[i], 24, FS(9.5f), Hex("#f2fbff"), Hex("#92b4c7"));
        }

        TxCN(p, "mSafetyNote", innerX, 102f, innerW, 13f,
            "占标率 · 虚线为 80% 预警阈", FS(9.5f), Hex("#8fb0c4"), TextAnchor.MiddleLeft);

        // 三条占比
        string[] names = { "粉尘", "噪声", "臭气" };
        float[] pct = { 0.62f, 0.93f, 0.60f };
        bool[] warn = { false, true, false };
        bulletFills = new RectTransform[3];

        for (int i = 0; i < 3; i++)
        {
            float rowTop = 120.42f + i * 32.08f;
            TxCN(p, "mBulletK", innerX, rowTop + 4f, 52f, 16f, names[i], FS(11.5f),
                Hex("#b6d3e3"), TextAnchor.MiddleLeft);

            const float trackX = 75f;
            const float trackW = 423.75f;
            RR(p, "mBulletTrack", trackX, rowTop + 9.5f, trackW, 5f,
                new Color(0.588f, 0.804f, 0.922f, 0.16f), 3f, Color.clear, 0f);

            Color fillA = warn[i] ? new Color(1f, 0.694f, 0.231f, 0.28f) : new Color(0.435f, 0.827f, 1f, 0.28f);
            Color fillB = warn[i] ? WARN : Hex("#6fd3ff");
            UIRoundRect fill = RR(p, "mBulletFill" + i, trackX, rowTop + 9.5f,
                trackW * pct[i], 5f, fillA, 3f, Color.clear, 0f);
            fill.gradient = true;
            fill.color2 = fillB;
            fill.Refresh();
            bulletFills[i] = fill.rectTransform;

            Img(p, "mBulletLimit", trackX + trackW * 0.8f, rowTop + 6.5f, 1f, 11f,
                new Color(1f, 0.694f, 0.231f, 0.65f));

            TxNum(p, "mBulletV", 505.75f, rowTop + 3.5f, 44f, 17f,
                Mathf.RoundToInt(pct[i] * 100f) + "%", FS(12.5f),
                warn[i] ? Hex("#ffcf86") : Hex("#e6f6ff"), TextAnchor.MiddleRight);
        }

        // 页脚
        CardFoot(p, 218.63f);
        TxRich(p, "mFoot1", innerX, 226.63f, 90f, 16f,
            "本月用水 <color=#E6F6FF>126</color> t", FS(9.5f), Hex("#92b4c7"));
        TxRich(p, "mFoot2", 95.61f, 226.63f, 76f, 16f,
            "废水 <color=#E6F6FF>0</color> 外排", FS(9.5f), Hex("#92b4c7"));
        TxRich(p, "mFoot3", 160.53f, 226.63f, 170f, 16f,
            "固废合规处置 <color=#E6F6FF>100%</color>", FS(9.5f), Hex("#92b4c7"));
    }

    // ================================================================= 左 02 能耗趋势

    static void BuildLeftEnergy(RectTransform p, out RectTransform mask,
        out RectTransform pointer, out RectTransform zone)
    {
        CardHead(p, "02", "ENERGY TREND", "近 7 日");

        BuildEnergyChart(p, out mask, out pointer, out zone);

        CardFoot(p, 218.62f);
        TxRich(p, "mFoot1", 16f, 226.62f, 120f, 16f,
            "本月累计 <color=#E6F6FF>187,000</color> kWh", FS(9.5f), Hex("#92b4c7"));
        TxRich(p, "mFoot2", 134.82f, 226.62f, 66f, 16f,
            "环比 <color=#E6F6FF>+4.9%</color>", FS(9.5f), Hex("#92b4c7"));
        TxRich(p, "mFoot3", 198.14f, 226.62f, 160f, 16f,
            "吨料电耗 <color=#E6F6FF>12.6</color> kWh/t", FS(9.5f), Hex("#92b4c7"));
    }

    // ================================================================= 左 03 环保设施

    static void BuildLeftFacilities(RectTransform p)
    {
        CardHead(p, "03", "FACILITIES", "6 套在线");

        string[] names =
        {
            "1# 投料口集尘罩 + 脉冲布袋",
            "2# 投料口集尘罩 + 脉冲布袋",
            "配料仓密闭 + 脉冲布袋",
            "原料库密闭 + 除尘",
            "发酵废气 LDPE 热封 + 通风",
            "三级化粪池"
        };
        string[] states = { "运行", "运行", "运行", "运行", "正常", "正常" };
        string[] metrics = { "1.5 kPa", "1.6 kPa", "1.4 kPa", "8,600 m³/h", "6 次/h", "0 外排" };

        for (int i = 0; i < 6; i++)
        {
            float top = 39f + i * 34.276f;
            if (i < 5)
                RRDashedRow(p, 16f, top + 34f, 533.75f, new Color(0.494f, 0.804f, 0.961f, 0.12f));

            RRC(p, "mFacDot", 16f + 3f, top + 16.5f, 6f, GREEN_DOT);

            TxCN(p, "mFacName", 16f + 13f, top + 9f, 420.14f, 15f, names[i], FS(11.5f),
                Hex("#d8edf8"), TextAnchor.MiddleLeft);

            RR(p, "mFacTag", 16f + 440.14f, top + 6.5f, 32.61f, 20f,
                new Color(0, 0, 0, 0), 2f, new Color(0.275f, 0.839f, 0.608f, 0.32f), 1f);
            TxCN(p, "mFacTagTx", 16f + 440.14f, top + 7f, 32.61f, 19f, states[i], 10,
                GREEN_TEXT, TextAnchor.MiddleCenter);

            TxNum(p, "mFacMeta", 16f + 479.75f, top + 9.5f, 54f, 14f, metrics[i], FS(11f),
                Hex("#a8c8da"), TextAnchor.MiddleRight);
        }

        CardFoot(p, 250.37f);
        TxRich(p, "mFoot1", 16f, 258.37f, 130f, 16f,
            "设施运行率 <color=#E6F6FF>100%</color>", FS(9.5f), Hex("#92b4c7"));
        TxRich(p, "mFoot2", 104.47f, 258.37f, 110f, 16f,
            "今日超标 <color=#E6F6FF>0</color> 次", FS(9.5f), Hex("#92b4c7"));
    }

    // ================================================================= 卡片公共

    static void CardHead(RectTransform p, string index, string sub, string note)
    {
        TxDisplay(p, "mIdx", 16f, 14f, 24f, 15f, index, 13, Hex("#4fc9f0").A(0.8f), TextAnchor.MiddleLeft, 1f);
        TxDisplay(p, "mSub", 35.9f, 14.5f, 200f, 15f, sub, FS(10.5f), Hex("#9fc4d8"), TextAnchor.MiddleLeft, 2f);
        TxCN(p, "mNote", 16f, 14f, 533.75f, 15f, note, FS(11f), Hex("#a8c8da"), TextAnchor.MiddleRight);
    }

    static void CardFoot(RectTransform p, float top)
    {
        Img(p, "mFootLine", 16f, top, 533.75f, 1f, new Color(0.494f, 0.804f, 0.961f, 0.16f));
    }

    // ================================================================= 顶栏 / 页脚

    static void BuildChrome(RectTransform canvas, out CanvasGroup headerGroup,
        out CanvasGroup footerGroup, out RectTransform headerRect, out RectTransform footerRect)
    {
        RectTransform chrome = DashUI.Child(canvas, "Main5Chrome");
        if (chrome == null)
        {
            var go = new GameObject("Main5Chrome", typeof(RectTransform));
            go.layer = 5;
            chrome = go.GetComponent<RectTransform>();
            chrome.SetParent(canvas, false);
            Undo.RegisterCreatedObjectUndo(go, "创建 main5 顶栏");
        }
        else DashUI.Clear(chrome);

        chrome.anchorMin = Vector2.zero;
        chrome.anchorMax = Vector2.one;
        chrome.pivot = new Vector2(0.5f, 0.5f);
        chrome.anchoredPosition = Vector2.zero;
        chrome.sizeDelta = Vector2.zero;

        // ---------- header 36,22 1848x84 ----------
        RectTransform header = DashUI.New("mHeader", chrome, 36f, -22f, 1848f, 84f).GetComponent<RectTransform>();
        headerGroup = header.gameObject.AddComponent<CanvasGroup>();
        headerRect = header;

        // 左标签
        RR(header, "mTagL", 0f, 26f, 81f, 24f, new Color(0, 0, 0, 0), 3f,
            new Color(0.122f, 0.435f, 0.698f, 0.24f), 1f);
        RRC(header, "mLiveDot", 13f, 38f, 6f, Hex("#0f8c72"));
        TxCN(header, "mTagLText", 23f, 27f, 52f, 22f, "安环在线", FS(10.5f), Hex("#235f88"), TextAnchor.MiddleLeft);
        TxCN(header, "mHeadLeft", 95f, 26f, 360f, 24f, "31 台设备 · 5 大工段", FS(11.5f), Hex("#40566a"), TextAnchor.MiddleLeft);

        // 中间标题
        Text h1 = TxCN(header, "mTitle", 770.67f, 9.5f, 306.64f, 40f, "安环能耗", 30, INK, TextAnchor.MiddleCenter, true);
        UILetterSpacing ls = h1.gameObject.AddComponent<UILetterSpacing>();
        ls.SetSpacing(12f, true);
        UIGradientText grad = h1.gameObject.AddComponent<UIGradientText>();
        grad.SetColors(Hex("#2f86b8"), Hex("#23618c"), Hex("#1b3a52"), 0.52f);

        Text en = TxDisplay(header, "mTitleEn", 770.67f, 53.5f, 306.64f, 13f,
            "SAFETY · ENVIRONMENT · ENERGY", 11, new Color(0.22f, 0.337f, 0.416f, 0.88f),
            TextAnchor.MiddleCenter, 6f);
        UILetterSpacing enLs = en.gameObject.AddComponent<UILetterSpacing>();
        enLs.SetSpacing(6f, true);
        Shadow enShadow = en.gameObject.AddComponent<Shadow>();
        enShadow.effectColor = new Color(1f, 1f, 1f, 0.55f);
        enShadow.effectDistance = new Vector2(0f, -1f);

        // 右侧
        TxCN(header, "mHeadRight", 1000f, 26f, 768f, 24f, "数据更新 2026-11-10 11:20", FS(11.5f),
            Hex("#40566a"), TextAnchor.MiddleRight);
        RR(header, "mTagR", 1780f, 26f, 68f, 24f, new Color(0, 0, 0, 0), 3f,
            new Color(0.122f, 0.435f, 0.698f, 0.24f), 1f);
        TxCN(header, "mTagRText", 1780f, 27f, 68f, 22f, "实时监测", FS(10.5f), Hex("#235f88"), TextAnchor.MiddleCenter);

        // ---------- footer 36,1036 1848x30 ----------
        RectTransform footer = DashUI.New("mFooter", chrome, 36f, -1036f, 1848f, 30f).GetComponent<RectTransform>();
        footerGroup = footer.gameObject.AddComponent<CanvasGroup>();
        footerRect = footer;

        Img(footer, "mFootLine", 0f, 0f, 1848f, 1f, new Color(0.549f, 0.784f, 0.941f, 0.16f));
        TxCN(footer, "mFooterL", 0f, 7f, 1200f, 20f,
            "数据来源：环保在线监测 + 能源计量平台", FS(9.5f),
            new Color(0.769f, 0.871f, 0.933f, 0.78f), TextAnchor.MiddleLeft);
        TxCN(footer, "mFooterR", 648f, 7f, 1200f, 20f,
            "统计周期 2026-11-01 至 2026-11-10", FS(9.5f),
            new Color(0.769f, 0.871f, 0.933f, 0.78f), TextAnchor.MiddleRight);
    }

    // ================================================================= 工具

    static int FS(float v) { return Mathf.FloorToInt(v + 0.5f); }

    /// <summary>右侧页面 / tooltip 构建时的字号倍数；左侧卡片与顶栏保持 1。</summary>
    static float fontScale = 1f;

    static int FontPx(int fs) { return Mathf.Max(1, Mathf.RoundToInt(fs * fontScale)); }

    static Color A(this Color c, float a) { c.a = a; return c; }

    static Font CN { get { return DashUI.F; } }

    static Font _numFont;
    static Font NUM
    {
        get
        {
            if (_numFont == null) _numFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/fonts/Tektur-Regular.ttf");
            return _numFont != null ? _numFont : DashUI.F;
        }
    }

    static Font _displayFont;
    static Font DISPLAY
    {
        get
        {
            if (_displayFont == null) _displayFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/fonts/BigShoulders-Bold.ttf");
            return _displayFont != null ? _displayFont : DashUI.F;
        }
    }

    static Text MakeText(RectTransform p, string name, float x, float top, float w, float h,
                         string text, int fs, Color c, Font font, TextAnchor align, bool bold)
    {
        GameObject go = DashUI.New(name, p, x, -top, w, h);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = FontPx(fs);
        t.text = text;
        t.color = c;
        t.alignment = align;
        t.raycastTarget = false;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        return t;
    }

    static Text TxCN(RectTransform p, string name, float x, float top, float w, float h,
                     string text, int fs, Color c, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
    {
        return MakeText(p, name, x, top, w, h, text, fs, c, CN, align, bold);
    }

    static Text TxNum(RectTransform p, string name, float x, float top, float w, float h,
                      string text, int fs, Color c, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
    {
        return MakeText(p, name, x, top, w, h, text, fs, c, NUM, align, bold);
    }

    static Text TxDisplay(RectTransform p, string name, float x, float top, float w, float h,
                          string text, int fs, Color c, TextAnchor align = TextAnchor.MiddleLeft,
                          float letterSpacing = 0f, bool bold = false)
    {
        Text t = MakeText(p, name, x, top, w, h, text, fs, c, DISPLAY, align, bold);
        if (letterSpacing > 0.01f)
        {
            UILetterSpacing ls = t.gameObject.AddComponent<UILetterSpacing>();
            ls.SetSpacing(letterSpacing, align == TextAnchor.MiddleCenter);
        }
        return t;
    }

    static Text TxRich(RectTransform p, string name, float x, float top, float w, float h,
                       string text, int fs, Color c, TextAnchor align = TextAnchor.MiddleLeft)
    {
        return TxCN(p, name, x, top, w, h, text, fs, c, align);
    }

    /// <summary>数字（Tektur）+ 单位（CN 小字）自动排版。</summary>
    static Text ValueUnit(RectTransform p, string name, float x, float top, float w, float h,
                          string number, string unit, int numFs, int unitFs, Color numColor, Color unitColor,
                          TextAnchor align = TextAnchor.MiddleLeft)
    {
        Text num = TxNum(p, name, x, top, w, h, number, numFs, numColor, align);
        if (string.IsNullOrEmpty(unit)) return num;

        int uFs = FontPx(unitFs);
        float gap = 3f * fontScale;
        float numW = TextWidth(number, NUM, numFs);
        float unitW = TextWidth(unit, CN, unitFs);
        float ux = align == TextAnchor.MiddleRight ? x + w - numW - unitW - gap : x + numW + gap;
        TxCN(p, name + "U", ux, top + Mathf.Max(0f, h - uFs - 4f * fontScale), unitW + 6f * fontScale,
            uFs + 4f, unit, unitFs, unitColor, TextAnchor.MiddleLeft);
        return num;
    }

    static float TextWidth(string text, Font font, int size)
    {
        if (string.IsNullOrEmpty(text) || font == null) return 0f;

        size = FontPx(size);
        float width = 0f;
        if (font.dynamic)
        {
            try { font.RequestCharactersInTexture(text, size, FontStyle.Normal); } catch { }
        }

        for (int i = 0; i < text.Length; i++)
        {
            CharacterInfo info;
            if (font.GetCharacterInfo(text[i], out info, size)) width += info.advance;
            else width += size * (text[i] < 128 ? 0.55f : 1f);
        }
        return width;
    }

    static UIRoundRect RR(RectTransform p, string name, float x, float top, float w, float h,
                          Color fill, float radius, Color border, float borderW)
    {
        GameObject go = DashUI.New(name, p, x, -top, w, h);
        UIRoundRect rr = go.AddComponent<UIRoundRect>();
        rr.color = fill;
        rr.radius = radius;
        rr.borderColor = border;
        rr.borderWidth = borderW;
        rr.raycastTarget = false;
        return rr;
    }

    static UIRoundRect RRC(RectTransform p, string name, float cx, float cy, float d, Color fill)
    {
        GameObject go = DashUI.New(name, p, cx, -cy, d, d, 0.5f, 0.5f);
        UIRoundRect rr = go.AddComponent<UIRoundRect>();
        rr.color = fill;
        rr.radius = d * 0.5f;
        rr.raycastTarget = false;
        return rr;
    }

    static RectTransform Img(RectTransform p, string name, float x, float top, float w, float h, Color c)
    {
        return DashUI.Img(p, name, x, -top, w, h, c, null);
    }

    static RectTransform Zone(RectTransform p, string name, float x, float top, float w, float h, int zoneId)
    {
        RectTransform rt = DashUI.Img(p, name, x, -top, w, h, new Color(0, 0, 0, 0), null);
        Image im = rt.GetComponent<Image>();
        im.raycastTarget = true;
        Main5PointerRelay relay = rt.gameObject.AddComponent<Main5PointerRelay>();
        relay.zone = zoneId;
        return rt;
    }

    static void HLine(RectTransform p, string name, float x, float top, float w, Color c)
    {
        Img(p, name, x, top, w, 1f, c);
    }

    static void DashH(RectTransform p, string name, float x, float top, float w, Color c)
    {
        float cursor = x;
        while (cursor < x + w - 0.5f)
        {
            float seg = Mathf.Min(4f, x + w - cursor);
            Img(p, name, cursor, top, seg, 1f, c);
            cursor += 6f;
        }
    }

    static void DashV(RectTransform p, string name, float x, float top, float h, Color c)
    {
        float cursor = top;
        while (cursor < top + h - 0.5f)
        {
            float seg = Mathf.Min(4f, top + h - cursor);
            Img(p, name, x, cursor, 1f, seg, c);
            cursor += 6f;
        }
    }

    static void RRDashedRow(RectTransform p, float x, float top, float w, Color c)
    {
        float cursor = x;
        while (cursor < x + w - 0.5f)
        {
            float seg = Mathf.Min(4f, x + w - cursor);
            Img(p, "mFacDash", cursor, top, seg, 1f, c);
            cursor += 7f;
        }
    }

    static void SavePrefab(GameObject go, string path)
    {
        bool active = go.activeSelf;
        go.SetActive(true);
        UnpackIfNeeded(go);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        go.SetActive(active);
        Debug.Log("[main5] 预制体 " + path);
    }

    static void UnpackIfNeeded(GameObject go)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go)) return;

        GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (root == go)
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot,
                InteractionMode.AutomatedAction);
    }

    // ================================================================= 数据

    public sealed class Stat
    {
        public readonly string Label;
        public readonly string Value;
        public readonly string Unit;

        public Stat(string label, string value, string unit)
        {
            Label = label;
            Value = value;
            Unit = unit;
        }
    }

    public sealed class Device
    {
        public readonly string Name;
        public readonly float Now;
        public readonly float Prev;

        public Device(string name, float now, float prev)
        {
            Name = name;
            Now = now;
            Prev = prev;
        }
    }

    public sealed class Section
    {
        public readonly string Nav;
        public readonly string Full;
        public readonly string Note;
        public readonly Stat[] Stats;
        public readonly Device[] Devices;

        public Section(string nav, string full, string note, Stat[] stats, Device[] devices)
        {
            Nav = nav;
            Full = full;
            Note = note;
            Stats = stats;
            Devices = devices;
        }
    }
}

/// <summary>存在 Library/main5_build_request.txt 时自动构建一次；结果写入 Library/main5_build_result.txt。</summary>
[InitializeOnLoad]
static class Main5SafetyEnergyAutoRunner
{
    const string ResultPath = "Library/main5_build_result.txt";
    const string RequestPath = "Library/main5_build_request.txt";

    static Main5SafetyEnergyAutoRunner()
    {
        EditorApplication.delayCall += TryRun;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!File.Exists(RequestPath)) return;
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TryRun;
    }

    static void TryRun()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[main5] 编辑器处于播放模式，自动构建已跳过。");
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        try
        {
            File.Delete(RequestPath);
            Main5SafetyEnergyBuilder.Build();
            File.WriteAllText(ResultPath, "OK " + DateTime.Now.ToString("s"));
        }
        catch (Exception ex)
        {
            File.WriteAllText(ResultPath, "FAIL\n" + ex);
            Debug.LogException(ex);
        }
    }
}
