#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Main4QC;

namespace Main4QC.EditorTools
{
    /// <summary>
    /// 只重建场景中名为 "Canvas main4" 的节点内容，其它对象一律不碰。
    /// 菜单：Tools/质量检测/Main4 重建巡诊台
    /// </summary>
    public static class Main4SceneBuilder
    {
        const string CanvasName = "Canvas main4";
        const string PrefKey = "Main4_QC_Tour_v1";
        const string FontPath = "Assets/fonts/kniuheiti-medium SDF.asset";

        static TMP_FontAsset _font;

        static Color Cyan { get { return Main4Controller.Cyan; } }
        static Color Green { get { return Main4Controller.Green; } }
        static Color Orange { get { return Main4Controller.Orange; } }
        static Color Red { get { return Main4Controller.Red; } }
        static Color Ink { get { return Main4Controller.Ink; } }
        static Color Dim { get { return Main4Controller.Dim; } }
        static Color Deep { get { return new Color(0.039f, 0.086f, 0.157f, 1f); } }
        static Color PanelBg { get { return new Color(0.043f, 0.098f, 0.173f, 0.88f); } }

        [MenuItem("Tools/质量检测/Main4 重建巡诊台")]
        public static void BuildMenu()
        {
            if (Build()) Debug.Log("[Main4] 巡诊台构建完成");
        }

        [MenuItem("Tools/质量检测/Main4 清空子节点")]
        public static void ClearMenu()
        {
            var canvas = FindCanvas();
            if (canvas == null) return;
            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Clear main4");
            Clear(canvas);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(PrefKey, false)) return;
                var canvas = FindCanvas();
                if (canvas == null) return;
                if (canvas.transform.Find("TopBar") != null) { EditorPrefs.SetBool(PrefKey, true); return; }
                if (Build())
                {
                    EditorPrefs.SetBool(PrefKey, true);
                    Debug.Log("[Main4] 检测到 Canvas main4，已自动构建巡诊台");
                }
            };
        }

        static Canvas FindCanvas()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var c in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (c.name == CanvasName) return c;
                }
            }
            return null;
        }

        static void Clear(Canvas canvas)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
            }
            var old = canvas.GetComponent<Main4Controller>();
            if (old != null) Object.DestroyImmediate(old);
        }

        // ---------------- 基础构建工具 ----------------
        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static RectTransform Rect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            return rt;
        }

        static RectTransform Stretch(GameObject go, float pad = 0f)
        {
            return Rect(go, Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));
        }

        static Image Img(Transform p, string name, Color c, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = NewUI(name, p);
            Rect(go, aMin, aMax, offMin, offMax);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        static Image Fill(Transform p, string name, Color c)
        {
            var go = NewUI(name, p);
            Stretch(go);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI Txt(Transform p, string name, string s, float size, Color c,
            TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = NewUI(name, p);
            Rect(go, aMin, aMax, offMin, offMax);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;

                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (_font == null)
                {
                    string p2 = AssetDatabase.GUIDToAssetPath("b8af04fa9d222c7469c10c88cb06b548");
                    if (!string.IsNullOrEmpty(p2))
                    {
                        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p2);
                    }
                }
                if (_font == null)
                {
                    Debug.LogError("[Main4] 未找到中文 TMP 字体：" + FontPath);
                    return TMP_Settings.defaultFontAsset;
                }
                return _font;
            }
        }

        static void ForceFont(Transform root)
        {
            var font = Font;
            if (font == null) return;
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.font != font) t.font = font;
            }
        }

        static Button MakeButton(Image img, Color hover)
        {
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = hover;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = colors;
            b.targetGraphic = img;
            return b;
        }

        // ---------------- 主构建 ----------------
        static bool Build()
        {
            var canvas = FindCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[Main4] 场景中找不到 " + CanvasName);
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Rebuild main4");

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Clear(canvas);
            var root = (RectTransform)canvas.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // 背景
            var bg = Fill(root, "BG", new Color(0.039f, 0.086f, 0.157f, 0.94f));
            AddGrid(bg.transform);
            Img(root, "BG_Glow", new Color(0f, 0.898f, 1f, 0.05f), new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.92f),
                Vector2.zero, Vector2.zero);

            var connectors = NewUI("Connectors", root);
            Stretch(connectors);

            // 四个区域
            var topBar = NewUI("TopBar", root);
            Rect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -72f), Vector2.zero);

            var leftPanel = NewUI("LeftPanel", root);
            Rect(leftPanel, new Vector2(0f, 0f), new Vector2(0.30f, 1f), new Vector2(0f, 56f), new Vector2(-8f, -72f));

            var center = NewUI("CenterStage", root);
            Rect(center, new Vector2(0.30f, 0f), new Vector2(0.70f, 1f), new Vector2(8f, 56f), new Vector2(-8f, -72f));

            var rightPanel = NewUI("RightPanel", root);
            Rect(rightPanel, new Vector2(0.70f, 0f), new Vector2(1f, 1f), new Vector2(8f, 56f), new Vector2(0f, -72f));

            var bottomBar = NewUI("BottomBar", root);
            Rect(bottomBar, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 56f));

            var overlay = NewUI("Overlay", root);
            Stretch(overlay);

            BuildTopBar(topBar.transform);

            var ctrl = canvas.gameObject.GetComponent<Main4Controller>();
            if (ctrl == null) ctrl = canvas.gameObject.AddComponent<Main4Controller>();

            ctrl.cards = BuildCards(leftPanel.transform, connectors.transform, ctrl);
            ctrl.nodes = BuildNodes(center.transform, ctrl);
            BuildStageExtras(center.transform, ctrl);
            BuildRight(rightPanel.transform, ctrl);
            BuildBottom(bottomBar.transform, ctrl);
            BuildOverlay(overlay.transform, ctrl);

            ctrl.stage = (RectTransform)center.transform;
            ctrl.zoomRoot = (RectTransform)center.transform.Find("ZoomRoot");
            ctrl.stageGlobal = center.transform.Find("ZoomRoot/StageGlobal").gameObject;
            ctrl.stageDetail = center.transform.Find("ZoomRoot/StageDetail").gameObject;

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            return true;
        }

        static void AddGrid(Transform p)
        {
            for (int i = 1; i < 16; i++)
            {
                float x = i / 16f;
                Img(p, "GV" + i, new Color(0f, 0.898f, 1f, 0.035f), new Vector2(x, 0f), new Vector2(x, 1f),
                    new Vector2(0f, 0f), new Vector2(1f, 0f));
            }
            for (int i = 1; i < 9; i++)
            {
                float y = i / 9f;
                Img(p, "GH" + i, new Color(0f, 0.898f, 1f, 0.035f), new Vector2(0f, y), new Vector2(1f, y),
                    new Vector2(0f, 0f), new Vector2(0f, 1f));
            }
        }

        // ---------------- 顶栏 ----------------
        static void BuildTopBar(Transform p)
        {
            Img(p, "Bar", new Color(0.031f, 0.073f, 0.133f, 0.92f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(p, "BarLine", new Color(0f, 0.898f, 1f, 0.35f), new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 1f));

            Txt(p, "Title", "AeroVision 智能视觉检测系统", 22f, Ink, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0.36f, 1f), new Vector2(24f, 8f), Vector2.zero);
            Txt(p, "Version", "V3.2", 14f, Cyan, TextAlignmentOptions.Left,
                new Vector2(0.255f, 0f), new Vector2(0.33f, 1f), new Vector2(0f, 8f), Vector2.zero);
            Txt(p, "Sub", "生物发酵饲料车间 · 在线质量检测", 14f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0.36f, 1f), new Vector2(24f, -20f), new Vector2(0f, -8f));

            string[] nav = { "首页", "检测任务", "历史数据", "系统设置", "帮助文档" };
            for (int i = 0; i < nav.Length; i++)
            {
                float x0 = 0.38f + i * 0.075f;
                var t = Txt(p, "Nav" + i, nav[i], 16f, i == 1 ? Cyan : Dim, TextAlignmentOptions.Center,
                    new Vector2(x0, 0f), new Vector2(x0 + 0.07f, 1f), Vector2.zero, Vector2.zero);
                if (i == 1)
                {
                    Img(p, "NavMark", new Color(0f, 0.898f, 1f, 0.85f), new Vector2(x0 + 0.022f, 0f), new Vector2(x0 + 0.048f, 0f),
                        new Vector2(0f, 6f), new Vector2(0f, 9f));
                    t.fontStyle = FontStyles.Bold;
                }
            }

            Img(p, "Avatar", new Color(0f, 0.898f, 1f, 0.28f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-190f, -16f), new Vector2(-158f, 16f));
            Txt(p, "AvatarTxt", "张", 15f, Ink, TextAlignmentOptions.Center,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-190f, -16f), new Vector2(-158f, 16f));
            Txt(p, "Op", "操作员 · 张工", 14f, Dim, TextAlignmentOptions.Left,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, -14f), new Vector2(-64f, 14f));

            Img(p, "Bell", new Color(1f, 1f, 1f, 0.10f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-56f, -16f), new Vector2(-24f, 16f));
            Txt(p, "BellTxt", "告警", 13f, Orange, TextAlignmentOptions.Center,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-56f, -16f), new Vector2(-24f, 16f));
            Img(p, "BellDot", Orange, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-28f, 10f), new Vector2(-22f, 16f));
        }

        // ---------------- 左侧卡片 ----------------
        static Main4CardView[] BuildCards(Transform panel, Transform connectors, Main4Controller ctrl)
        {
            var list = new List<Main4CardView>();
            string[] names = { "配料精度", "混合均匀度", "发酵活性", "成品水分", "金属/铁杂", "包装合规", "能耗与工况" };

            Txt(panel, "TrackLabel", "检测项轨道 / 7 项", 14f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -32f), new Vector2(-16f, -6f));

            float top = 42f;
            float h = 112f;
            float gap = 12f;

            for (int i = 0; i < 7; i++)
            {
                var cardGo = NewUI("Card" + i, panel);
                var rt = (RectTransform)cardGo.transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.offsetMin = new Vector2(14f, 0f);
                rt.offsetMax = new Vector2(-14f, 0f);
                rt.anchoredPosition = new Vector2(0f, -(top + i * (h + gap)));
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);

                var view = new Main4CardView();
                view.root = rt;
                view.bg = Fill(rt, "Bg", new Color(0.043f, 0.11f, 0.18f, 0.78f));
                view.accent = Img(rt, "Accent", new Color(0f, 0.898f, 1f, 0.22f), new Vector2(0f, 0f), new Vector2(0f, 1f),
                    Vector2.zero, new Vector2(3f, 0f));

                Txt(rt, "No", "0" + (i + 1), 14f, new Color(0f, 0.898f, 1f, 0.6f), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -30f), new Vector2(70f, -10f));

                view.name = Txt(rt, "Name", names[i], 20f, Ink, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(46f, -32f), new Vector2(-90f, -6f));

                view.dot = Img(rt, "Dot", Green, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-24f, -26f), new Vector2(-16f, -18f));

                view.badge = Txt(rt, "Badge", "合格", 14f, Green, TextAlignmentOptions.Right,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-86f, -30f), new Vector2(-28f, -10f));

                view.value = Txt(rt, "Value", "--", 34f, Ink, TextAlignmentOptions.Left,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 34f), new Vector2(170f, 84f));
                view.unit = Txt(rt, "Unit", "", 14f, Dim, TextAlignmentOptions.Left,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(150f, 40f), new Vector2(260f, 66f));

                var track = Img(rt, "TolTrack", new Color(1f, 1f, 1f, 0.10f), new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(14f, 20f), new Vector2(-14f, 24f));
                view.band = Img(track.transform, "Band", new Color(0f, 0.898f, 1f, 0.25f), new Vector2(0.3f, 0f), new Vector2(0.62f, 1f),
                    Vector2.zero, Vector2.zero);
                view.marker = Img(track.transform, "Marker", Ink, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                    new Vector2(-1f, -3f), new Vector2(1f, 3f));

                view.button = MakeButton(view.bg, new Color(1.25f, 1.25f, 1.25f, 1f));
                view.bg.raycastTarget = true;

                var line = Img(connectors, "Line" + i, new Color(0f, 0.898f, 1f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                line.rectTransform.sizeDelta = new Vector2(120f, 1f);
                view.connector = line;

                list.Add(view);
            }
            return list.ToArray();
        }

        // ---------------- 中央：工位链 ----------------
        static Main4NodeView[] BuildNodes(Transform center, Main4Controller ctrl)
        {
            var zoom = NewUI("ZoomRoot", center);
            Stretch(zoom);

            var global = NewUI("StageGlobal", zoom.transform);
            Stretch(global);

            // 设备模型占位
            var ph = Img(global.transform, "ModelPlaceholder", new Color(0f, 0.898f, 1f, 0.05f),
                new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.66f), Vector2.zero, Vector2.zero);
            Txt(ph.transform, "PhText", "设备三维模型占位区\n待接入 3D 模型 / RenderTexture", 16f,
                new Color(0.56f, 0.71f, 0.80f, 0.75f), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var detail = NewUI("StageDetail", zoom.transform);
            Stretch(detail);
            detail.SetActive(false);

            string[] names = { "原料库", "清理筛", "粉碎机", "配料秤", "发酵床", "发酵罐", "混合机", "烘干机", "包装线", "码垛" };
            var list = new List<Main4NodeView>();

            for (int i = 0; i < 10; i++)
            {
                bool top = i < 5;
                int col = top ? i : 9 - i;
                float x0 = 0.03f + col * 0.193f;
                float y0 = top ? 0.76f : 0.06f;

                var nodeGo = NewUI("Node" + i, global.transform);
                var rt = (RectTransform)nodeGo.transform;
                rt.anchorMin = new Vector2(x0, y0);
                rt.anchorMax = new Vector2(x0 + 0.17f, y0 + 0.14f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var view = new Main4NodeView();
                view.root = rt;
                view.bg = Fill(rt, "Bg", new Color(0.055f, 0.16f, 0.25f, 0.9f));
                view.glow = Img(rt, "Glow", new Color(0f, 0.898f, 1f, 0.3f), new Vector2(-0.12f, -0.15f), new Vector2(1.12f, 1.15f),
                    Vector2.zero, Vector2.zero);
                view.glow.transform.SetAsFirstSibling();
                view.marker = Img(rt, "Marker", Green, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-5f, -10f), new Vector2(5f, 0f));
                view.marker.gameObject.SetActive(false);

                Txt(rt, "Icon", "◆", 14f, Cyan, TextAlignmentOptions.Center,
                    new Vector2(0f, 0.45f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);
                view.label = Txt(rt, "Name", names[i], 14f, Dim, TextAlignmentOptions.Center,
                    new Vector2(0f, 0f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

                view.button = MakeButton(view.bg, new Color(1.2f, 1.2f, 1.2f, 1f));
                view.bg.raycastTarget = true;
                list.Add(view);
            }

            BuildFlowLine(global.transform);
            return list.ToArray();
        }

        static void BuildFlowLine(Transform p)
        {
            // 工艺流虚线：上排横线 + 右侧竖向 + 下排横线
            var lineColor = new Color(0f, 0.898f, 1f, 0.35f);
            Img(p, "Flow1", lineColor, new Vector2(0.12f, 0.83f), new Vector2(0.88f, 0.83f), new Vector2(0f, -1f), new Vector2(0f, 1f));
            Img(p, "Flow2", lineColor, new Vector2(0.88f, 0.13f), new Vector2(0.88f, 0.83f), new Vector2(-1f, 0f), new Vector2(1f, 0f));
            Img(p, "Flow3", lineColor, new Vector2(0.12f, 0.13f), new Vector2(0.88f, 0.13f), new Vector2(0f, -1f), new Vector2(0f, 1f));
        }

        static void BuildStageExtras(Transform center, Main4Controller ctrl)
        {
            string[] views = { "正视", "侧视", "俯视", "等轴测" };
            ctrl.viewButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                var img = Img(center, "View" + i, i == 0 ? new Color(0f, 0.898f, 1f, 0.85f) : new Color(0.35f, 0.55f, 0.65f, 1f),
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f + i * 56f, 10f), new Vector2(60f + i * 56f, 36f));
                var t = Txt(img.transform, "T", views[i], 14f, Deep, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                t.raycastTarget = false;
                ctrl.viewButtons[i] = MakeButton(img, new Color(1.3f, 1.3f, 1.3f, 1f));
            }

            var zoomOut = Img(center, "ZoomOut", new Color(0f, 0.898f, 1f, 0.18f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-86f, 10f), new Vector2(-60f, 36f));
            Txt(zoomOut.transform, "T", "−", 18f, Cyan, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.zoomOut = MakeButton(zoomOut, new Color(1.3f, 1.3f, 1.3f, 1f));

            var zoomIn = Img(center, "ZoomIn", new Color(0f, 0.898f, 1f, 0.18f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-52f, 10f), new Vector2(-26f, 36f));
            Txt(zoomIn.transform, "T", "+", 18f, Cyan, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.zoomIn = MakeButton(zoomIn, new Color(1.3f, 1.3f, 1.3f, 1f));

            // 光环
            var haloRoot = NewUI("HaloRoot", center);
            Rect(haloRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ctrl.haloRoot = (RectTransform)haloRoot.transform;
            ctrl.halos = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var h = Img(haloRoot.transform, "Halo" + i, new Color(0f, 0.898f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                h.rectTransform.sizeDelta = new Vector2(220f, 220f);
                h.gameObject.SetActive(false);
                ctrl.halos[i] = h;
            }

            // 粒子
            ctrl.particles = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var p = Img(center, "P" + i, Cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                p.rectTransform.sizeDelta = new Vector2(8f, 8f);
                p.gameObject.SetActive(false);
                ctrl.particles[i] = p;
            }

            BuildDetailPanels(center.Find("ZoomRoot/StageDetail"), ctrl);
        }

        static void BuildDetailPanels(Transform parent, Main4Controller ctrl)
        {
            ctrl.detailPanels = new GameObject[3];
            ctrl.heatCells = new Image[24];

            // 0 发酵罐
            var p0 = NewUI("Detail_Tank", parent);
            Rect(p0, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.86f), Vector2.zero, Vector2.zero);
            Img(p0.transform, "Body", new Color(0.06f, 0.19f, 0.28f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(p0.transform, "Liquid", new Color(0f, 0.898f, 1f, 0.16f), new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(4f, 4f), new Vector2(-4f, 0f));
            Txt(p0.transform, "T", "液体发酵罐 · 剖视", 18f, Ink, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -34f), new Vector2(-14f, -8f));
            for (int i = 0; i < 6; i++)
            {
                float v = 0.25f + i * 0.12f;
                Img(p0.transform, "Temp" + i, Color.Lerp(Cyan, Orange, i / 5f), new Vector2(0.08f + i * 0.14f, 0.66f),
                    new Vector2(0.19f + i * 0.14f, 0.72f), Vector2.zero, Vector2.zero);
            }
            Img(p0.transform, "Level", new Color(1f, 1f, 1f, 0.65f), new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.55f),
                new Vector2(0f, -1f), new Vector2(0f, 1f));
            Img(p0.transform, "Probe", Orange, new Vector2(0.48f, 0.30f), new Vector2(0.52f, 0.34f), Vector2.zero, Vector2.zero);
            Txt(p0.transform, "ProbeT", "探头 38.5℃", 14f, Orange, TextAlignmentOptions.Left,
                new Vector2(0.54f, 0.28f), new Vector2(0.95f, 0.36f), Vector2.zero, Vector2.zero);
            ctrl.detailPanels[0] = p0;

            // 1 发酵床 6x4
            var p1 = NewUI("Detail_Bed", parent);
            Rect(p1, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.86f), Vector2.zero, Vector2.zero);
            Img(p1.transform, "Body", new Color(0.06f, 0.19f, 0.28f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Txt(p1.transform, "T", "发酵床 · 6×4 温湿度矩阵", 18f, Ink, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -34f), new Vector2(-14f, -8f));
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 6; c++)
                {
                    int idx = r * 6 + c;
                    var cell = Img(p1.transform, "Cell" + idx, new Color(0f, 0.898f, 1f, 0.4f),
                        new Vector2(0.06f + c * 0.155f, 0.14f + r * 0.18f),
                        new Vector2(0.06f + c * 0.155f + 0.14f, 0.14f + r * 0.18f + 0.15f), Vector2.zero, Vector2.zero);
                    ctrl.heatCells[idx] = cell;
                }
            }
            var flopper = Img(p1.transform, "Flopper", new Color(1f, 0.635f, 0.227f, 0.9f),
                new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.06f), new Vector2(-3f, 0f), new Vector2(3f, 96f));
            ctrl.flopper = flopper.rectTransform;
            ctrl.detailPanels[1] = p1;

            // 2 烘干机 + 包装线
            var p2 = NewUI("Detail_Dryer", parent);
            Rect(p2, new Vector2(0.16f, 0.2f), new Vector2(0.84f, 0.86f), Vector2.zero, Vector2.zero);
            Img(p2.transform, "Body", new Color(0.06f, 0.19f, 0.28f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Txt(p2.transform, "T", "烘干机 · 风道与水分测点", 18f, Ink, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -34f), new Vector2(-14f, -8f));
            string[] zones = { "进风区", "出风区", "排风区" };
            for (int i = 0; i < 3; i++)
            {
                Img(p2.transform, "Zone" + i, new Color(0f, 0.898f, 1f, 0.12f + i * 0.05f),
                    new Vector2(0.05f + i * 0.31f, 0.42f), new Vector2(0.05f + i * 0.31f + 0.29f, 0.72f), Vector2.zero, Vector2.zero);
                Txt(p2.transform, "ZT" + i, zones[i], 14f, Dim, TextAlignmentOptions.Center,
                    new Vector2(0.05f + i * 0.31f, 0.42f), new Vector2(0.05f + i * 0.31f + 0.29f, 0.72f), Vector2.zero, Vector2.zero);
            }
            Img(p2.transform, "Seal", new Color(1f, 0.635f, 0.227f, 0.85f), new Vector2(0.06f, 0.22f), new Vector2(0.55f, 0.25f), Vector2.zero, Vector2.zero);
            Txt(p2.transform, "SealT", "封口加热条 178℃", 14f, Orange, TextAlignmentOptions.Left,
                new Vector2(0.58f, 0.19f), new Vector2(0.96f, 0.28f), Vector2.zero, Vector2.zero);
            Txt(p2.transform, "Moist", "水分测点 11.6%   称重回重 25.0kg", 14f, Green, TextAlignmentOptions.Left,
                new Vector2(0.06f, 0.05f), new Vector2(0.96f, 0.16f), Vector2.zero, Vector2.zero);
            ctrl.detailPanels[2] = p2;
        }

        // ---------------- 右侧 ----------------
        static void BuildRight(Transform panel, Main4Controller ctrl)
        {
            ctrl.rings = new Image[3];
            ctrl.ringValues = new TextMeshProUGUI[3];
            string[] cap = { "综合合格率", "最大偏差", "单次耗时" };
            for (int i = 0; i < 3; i++)
            {
                float x = 0.02f + i * 0.33f;
                var ring = Img(panel, "Ring" + i, Green, new Vector2(x, 1f), new Vector2(x + 0.30f, 1f),
                    new Vector2(0f, -150f), new Vector2(0f, -10f));
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.fillAmount = 0.85f;
                ctrl.rings[i] = ring;

                ctrl.ringValues[i] = Txt(panel, "RV" + i, "--", 24f, Ink, TextAlignmentOptions.Center,
                    new Vector2(x, 1f), new Vector2(x + 0.30f, 1f), new Vector2(0f, -110f), new Vector2(0f, -50f));

                Txt(panel, "RC" + i, cap[i], 14f, Dim, TextAlignmentOptions.Center,
                    new Vector2(x, 1f), new Vector2(x + 0.30f, 1f), new Vector2(0f, -178f), new Vector2(0f, -152f));
            }

            // 主图
            var chartPanel = NewUI("ChartPanel", panel);
            Rect(chartPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -520f), new Vector2(-14f, -196f));
            Img(chartPanel.transform, "Bg", new Color(0.035f, 0.086f, 0.15f, 0.92f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            ctrl.chartTitle = Txt(chartPanel.transform, "Title", "图表", 16f, Ink, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -30f), new Vector2(-12f, -6f));
            ctrl.chartNote = Txt(chartPanel.transform, "Note", "", 14f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -52f), new Vector2(-12f, -30f));

            var chartGo = NewUI("Chart", chartPanel.transform);
            Rect(chartGo, Vector2.zero, Vector2.one, new Vector2(16f, 14f), new Vector2(-16f, -60f));
            ctrl.chart = chartGo.AddComponent<Main4ChartGraphic>();
            ctrl.chart.color = Color.white;
            ctrl.chart.raycastTarget = false;

            // 日志
            var logPanel = NewUI("LogPanel", panel);
            Rect(logPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -740f), new Vector2(-14f, -535f));
            Img(logPanel.transform, "Bg", new Color(0.035f, 0.086f, 0.15f, 0.92f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Txt(logPanel.transform, "Title", "系统日志", 16f, Ink, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -28f), new Vector2(-12f, -6f));

            ctrl.logLines = new TextMeshProUGUI[6];
            for (int i = 0; i < 6; i++)
            {
                ctrl.logLines[i] = Txt(logPanel.transform, "Log" + i, "", 14f, Dim, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -54f - i * 26f), new Vector2(-12f, -34f - i * 26f));
                ctrl.logLines[i].richText = true;
                ctrl.logLines[i].overflowMode = TextOverflowModes.Ellipsis;
                ctrl.logLines[i].enableWordWrapping = true;
            }

            // 面包屑
            ctrl.breadcrumb = Txt(panel, "Breadcrumb", "", 14f, Cyan, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -772f), new Vector2(-16f, -746f));

            // 操作按钮
            var b1 = Img(panel, "BtnExport", new Color(0f, 0.6f, 0.75f, 0.9f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -826f), new Vector2(120f, -788f));
            Txt(b1.transform, "T", "导出报告", 14f, Deep, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.btnExport = MakeButton(b1, new Color(1.15f, 1.15f, 1.15f, 1f));

            var b2 = Img(panel, "BtnRescan", new Color(0f, 0.898f, 1f, 0.22f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(128f, -826f), new Vector2(234f, -788f));
            Txt(b2.transform, "T", "重新扫描", 14f, Cyan, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.btnRescan = MakeButton(b2, new Color(1.3f, 1.3f, 1.3f, 1f));

            var b3 = Img(panel, "BtnStop", new Color(0.5f, 0.13f, 0.16f, 0.85f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(242f, -826f), new Vector2(348f, -788f));
            Txt(b3.transform, "T", "紧急停机", 14f, Red, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.btnStop = MakeButton(b3, new Color(1.3f, 1.3f, 1.3f, 1f));

            var b4 = Img(panel, "BtnFull", new Color(0f, 0.898f, 1f, 0.16f), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(356f, -826f), new Vector2(-14f, -788f));
            Txt(b4.transform, "T", "一键全检", 14f, Cyan, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.btnFullScan = MakeButton(b4, new Color(1.3f, 1.3f, 1.3f, 1f));
        }

        // ---------------- 底栏 ----------------
        static void BuildBottom(Transform p, Main4Controller ctrl)
        {
            Img(p, "Bg", new Color(0.031f, 0.073f, 0.133f, 0.94f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(p, "Top", new Color(0f, 0.898f, 1f, 0.28f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1f), Vector2.zero);

            Img(p, "Dot", Green, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -4f), new Vector2(32f, 4f));
            Txt(p, "Conn", "设备连接正常", 14f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(40f, 0f), new Vector2(190f, 0f));
            ctrl.latencyText = Txt(p, "Latency", "延迟 --ms", 14f, Cyan, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(196f, 0f), new Vector2(320f, 0f));

            Txt(p, "Batch", "批次 B2026-1115-04", 14f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(344f, 0f), new Vector2(540f, 0f));

            Img(p, "ProgTrack", new Color(1f, 1f, 1f, 0.10f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(556f, -4f), new Vector2(756f, 4f));
            ctrl.progressFill = Img(p, "ProgFill", Cyan, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(556f, -4f), new Vector2(756f, 4f));
            ctrl.progressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            ctrl.progressFill.rectTransform.anchoredPosition = new Vector2(556f, 0f);
            ctrl.progressText = Txt(p, "ProgTxt", "45 / 120", 14f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(766f, 0f), new Vector2(856f, 0f));

            ctrl.loadBars = new Image[3];
            ctrl.loadTexts = new TextMeshProUGUI[3];
            string[] labels = { "CPU", "GPU", "MEM" };
            for (int i = 0; i < 3; i++)
            {
                float x = 880f + i * 150f;
                Txt(p, "L" + i, labels[i], 14f, Dim, TextAlignmentOptions.Left,
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(x, 0f), new Vector2(x + 40f, 0f));
                Img(p, "LT" + i, new Color(1f, 1f, 1f, 0.10f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(x + 42f, -3f), new Vector2(x + 112f, 3f));
                var bar = Img(p, "LB" + i, Cyan, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(x + 42f, -3f), new Vector2(x + 112f, 3f));
                bar.rectTransform.pivot = new Vector2(0f, 0.5f);
                bar.rectTransform.anchoredPosition = new Vector2(x + 42f, 0f);
                ctrl.loadBars[i] = bar;
                ctrl.loadTexts[i] = Txt(p, "LV" + i, "--", 14f, Cyan, TextAlignmentOptions.Left,
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(x + 118f, 0f), new Vector2(x + 150f, 0f));
            }
        }

        static void BuildOverlay(Transform p, Main4Controller ctrl)
        {
            var trace = NewUI("TracePanel", p);
            Rect(trace, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Img(trace.transform, "Bg", new Color(0.02f, 0.06f, 0.11f, 0.96f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ((RectTransform)trace.transform).sizeDelta = new Vector2(360f, 210f);
            Txt(trace.transform, "Title", "批次追溯", 18f, Cyan, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(16f, 160f), new Vector2(-16f, -12f));
            ctrl.traceBody = Txt(trace.transform, "Body", "", 14f, Dim, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -46f));
            ctrl.traceBody.enableWordWrapping = true;
            trace.SetActive(false);
            ctrl.tracePanel = trace;
        }
    }
}
#endif
