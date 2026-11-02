using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 大屏内容控制器：
    /// - Image1：订单明细（可点击编辑的表格，自动滚动）
    /// - Image2：AGV 实时调度示意图
    /// - Image3：订单负荷与产能匹配柱状图
    /// - Image4：AGV 配送任务进度条
    /// - Image5：物料库存趋势折线图
    ///
    /// 所有图表都是 uGUI 控件（纯色块 + 文字 + 自绘折线），不是图片；
    /// 图表上的数据点带悬停提示。
    /// </summary>
    [DisallowMultipleComponent]
    public class DashboardTablesController : MonoBehaviour
    {
        public const string TablePrefix = "__Table_";
        public const int CurrentContentVersion = 11;

        [Header("内容挂载点（Canvas left / Image1、Image2；Canvas right / Image3~Image5）")]
        public RectTransform image1;
        public RectTransform image2;
        public RectTransform image3;
        public RectTransform image4;
        public RectTransform image5;

        [Header("字体（留空自动使用 Unity 内置字体）")]
        public Font font;

        [Header("订单明细自动滚动")]
        public bool autoScrollOrderTable = true;

        [HideInInspector] public int contentVersion;

        static readonly ColumnDef[] OrderColumns =
        {
            new ColumnDef("订单号", 1.45f, CellRole.Text),
            new ColumnDef("产品型号", 1.90f, CellRole.Text),
            new ColumnDef("数量", 0.62f, CellRole.Number),
            new ColumnDef("交货时间", 1.20f, CellRole.Date),
            new ColumnDef("库存", 0.60f, CellRole.Number),
            new ColumnDef("状态", 0.95f, CellRole.Status),
            new ColumnDef("预警", 1.05f, CellRole.Warning)
        };

        readonly List<RectTransform> generatedRoots = new List<RectTransform>();

        List<OrderRowData> orders;
        TableView orderView;

        /// <summary>当前生成的根节点（编辑器做 Undo 时使用）。</summary>
        public IList<RectTransform> GeneratedRoots { get { return generatedRoots; } }

        void Start()
        {
            if (Application.isPlaying) BuildAll();
        }

        /// <summary>清空并重新生成 Image1~Image5 的全部内容。</summary>
        [ContextMenu("重新生成表格与图表")]
        public void BuildAll()
        {
            if (font == null) font = LoadBuiltinFont();

            if (image1 == null || image2 == null || image3 == null || image4 == null || image5 == null)
            {
                Debug.LogError("[DashboardTables] 挂载点未配置完整：请指定 Canvas left/Image1、Image2 和 Canvas right/Image3~Image5。", this);
                return;
            }

            ClearAll();
            ApplyPanelStyle(image1);
            ApplyPanelStyle(image2);
            ApplyPanelStyle(image3);
            ApplyPanelStyle(image4);
            ApplyPanelStyle(image5);
            BuildScreenFrame();
            BuildOrderTable();

            List<AgvRowData> agvs = DashboardMockData.CreateAgvStatus();
            generatedRoots.Add(DashboardChartFactory.BuildAgvDispatch(image2, agvs, font));

            List<LoadRowData> loads = DashboardMockData.CreateLoad();
            generatedRoots.Add(DashboardChartFactory.BuildLoadChart(image3, loads, font));

            List<RouteRowData> routes = DashboardMockData.CreateRoutes();
            generatedRoots.Add(DashboardChartFactory.BuildRouteChart(image4, routes, font));

            List<InventoryRowData> inventories = DashboardMockData.CreateInventory();
            generatedRoots.Add(DashboardChartFactory.BuildInventoryChart(image5, inventories, font));

            contentVersion = CurrentContentVersion;
        }

        /// <summary>删除已经生成的内容（不会动 Image1~Image5 本身）。</summary>
        public void ClearAll()
        {
            generatedRoots.Clear();
            orderView = null;

            RectTransform[] targets = { image1, image2, image3, image4, image5 };
            foreach (RectTransform target in targets)
            {
                if (target == null) continue;

                for (int i = target.childCount - 1; i >= 0; i--)
                {
                    Transform child = target.GetChild(i);
                    if (!child.name.StartsWith(TablePrefix, StringComparison.Ordinal)) continue;

                    child.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        // ---------------------------------------------------------------- 订单明细（表格）

        void BuildOrderTable()
        {
            orders = DashboardMockData.CreateOrders();
            TableStyle style = MakeStyle(17, 38f, 34f, 14f);

            orderView = DashboardTableFactory.Create(image1, TablePrefix + "订单明细",
                                                     OrderColumns, orders.Count, style);
            generatedRoots.Add(orderView.root);

            for (int r = 0; r < orders.Count; r++)
            {
                int row = r;
                OrderRowData data = orders[r];

                for (int c = 0; c < OrderColumns.Length; c++)
                {
                    EditableCell cell = orderView.Cell(r, c);
                    BindOrderCell(cell, row, c, style);
                    cell.SetValue(OrderText(data, c));
                }
            }

            if (autoScrollOrderTable && orderView.scroll != null)
            {
                var auto = orderView.scroll.gameObject.AddComponent<TableAutoScroll>();
                auto.scrollRect = orderView.scroll;
            }
        }

        void BindOrderCell(EditableCell cell, int row, int column, TableStyle style)
        {
            switch (column)
            {
                case 2:
                case 4:
                    cell.validator = NonNegativeInt;
                    cell.styler = PlainStyler(style);
                    break;
                case 3:
                    cell.validator = DateText;
                    cell.styler = PlainStyler(style);
                    break;
                case 5:
                    cell.readOnly = true;
                    cell.styler = StatusStyler();
                    break;
                case 6:
                    cell.readOnly = true;
                    cell.styler = WarningStyler(style);
                    break;
                default:
                    cell.styler = PlainStyler(style);
                    break;
            }

            cell.Changed += value => OnOrderEdited(row, column, value);
        }

        void OnOrderEdited(int row, int column, string value)
        {
            if (row < 0 || row >= orders.Count) return;

            OrderRowData data = orders[row];
            int number;

            switch (column)
            {
                case 2:
                    if (int.TryParse(value, out number)) data.quantity = number;
                    break;
                case 3:
                    data.delivery = value;
                    break;
                case 4:
                    if (int.TryParse(value, out number)) data.stock = number;
                    break;
                default:
                    return;
            }

            data.Refresh();
            orderView.Cell(row, 5).SetValue(data.status);
            orderView.Cell(row, 6).SetValue(data.warning);
        }

        static string OrderText(OrderRowData data, int column)
        {
            switch (column)
            {
                case 0: return data.orderId;
                case 1: return data.model;
                case 2: return data.quantity.ToString();
                case 3: return data.delivery;
                case 4: return data.stock.ToString();
                case 5: return data.status;
                case 6: return data.warning;
                default: return string.Empty;
            }
        }

        // ---------------------------------------------------------------- 配色 / 校验

        static void SetTextColors(EditableCell cell, Color color)
        {
            if (cell.labelText != null) cell.labelText.color = color;
            if (cell.inputText != null) cell.inputText.color = color;
        }

        static Action<EditableCell, string> PlainStyler(TableStyle style)
        {
            return (cell, value) =>
            {
                if (cell.background != null) cell.background.color = Color.clear;
                SetTextColors(cell, style.text);
            };
        }

        static Action<EditableCell, string> StatusStyler()
        {
            return (cell, value) =>
            {
                Color color = TableStyle.StatusColor(value);
                if (cell.background != null)
                    cell.background.color = new Color(color.r, color.g, color.b, 0.22f);
                SetTextColors(cell, Color.Lerp(color, Color.white, 0.35f));
            };
        }

        static Action<EditableCell, string> WarningStyler(TableStyle style)
        {
            return (cell, value) =>
            {
                if (cell.background != null) cell.background.color = Color.clear;

                if (string.IsNullOrEmpty(value))
                    SetTextColors(cell, new Color(style.text.r, style.text.g, style.text.b, 0.45f));
                else
                    SetTextColors(cell, TableStyle.WarningColor(value));
            };
        }

        static string NonNegativeInt(string raw)
        {
            int number;
            if (!int.TryParse(raw.Trim(), out number) || number < 0) return null;
            return number.ToString(CultureInfo.InvariantCulture);
        }

        static string DateText(string raw)
        {
            DateTime date;
            if (!DateTime.TryParse(raw.Trim(), out date)) return null;
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 面板样式：半透明白蓝玻璃底 + 青色细边框 + 四角短角标 + 顶部 1px 高光线。
        /// 全部用纯色 Image 绘制，不使用任何贴图。
        /// </summary>
        static void ApplyPanelStyle(RectTransform panel)
        {
            if (panel == null) return;

            Image bg = panel.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = null;
                bg.type = Image.Type.Simple;
                bg.color = TableStyle.PanelFill;
            }

            var deco = new GameObject(TablePrefix + "PanelDeco", typeof(RectTransform));
            deco.layer = panel.gameObject.layer;
            var rt = (RectTransform)deco.transform;
            rt.SetParent(panel, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();

            Color edge = TableStyle.PanelEdge;
            Color corner = new Color(0.30f, 0.85f, 0.96f, 0.85f);

            // 四条 2px 细边框
            Anchored(rt, "EdgeTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 2), edge);
            Anchored(rt, "EdgeBottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 2), edge);
            Anchored(rt, "EdgeLeft", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(2, 0), edge);
            Anchored(rt, "EdgeRight", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(2, 0), edge);

            // 顶部 1px 白色高光线
            Anchored(rt, "TopHighlight", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                     new Vector2(0, -3), new Vector2(0, 1), TableStyle.PanelHighlight);

            // 四角短角标（8 条）
            const float len = 16f, thick = 3f, inset = 2f;
            Anchored(rt, "C_TL_H", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(inset, -inset), new Vector2(len, thick), corner);
            Anchored(rt, "C_TL_V", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(inset, -inset), new Vector2(thick, len), corner);
            Anchored(rt, "C_TR_H", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-inset, -inset), new Vector2(len, thick), corner);
            Anchored(rt, "C_TR_V", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-inset, -inset), new Vector2(thick, len), corner);
            Anchored(rt, "C_BL_H", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(inset, inset), new Vector2(len, thick), corner);
            Anchored(rt, "C_BL_V", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(inset, inset), new Vector2(thick, len), corner);
            Anchored(rt, "C_BR_H", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-inset, inset), new Vector2(len, thick), corner);
            Anchored(rt, "C_BR_V", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-inset, inset), new Vector2(thick, len), corner);
        }

        static void Anchored(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                             Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        /// <summary>
        /// 屏幕外框装饰线：在 Canvas_mask 的全屏画布上画双层边框 + 四角角标（纯色块，无贴图）。
        /// </summary>
        void BuildScreenFrame()
        {
            GameObject maskRoot = GameObject.Find("Canvas_mask");
            if (maskRoot == null) return;

            var canvasRT = maskRoot.transform as RectTransform;
            if (canvasRT == null) return;

            string frameName = TablePrefix + "ScreenFrame";
            Transform old = canvasRT.Find(frameName);
            if (old != null)
            {
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }

            var go = new GameObject(frameName, typeof(RectTransform));
            go.layer = canvasRT.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvasRT, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Color thin = new Color(0.30f, 0.85f, 0.96f, 0.22f);
            Color bright = new Color(0.28f, 0.88f, 1f, 0.75f);

            // 外层 2px 边框（距边缘 8px）
            Anchored(rt, "OuterTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(0, 2), thin);
            Anchored(rt, "OuterBottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(0, 2), thin);
            Anchored(rt, "OuterLeft", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(2, 0), thin);
            Anchored(rt, "OuterRight", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(2, 0), thin);

            // 内层 1px 边框（距边缘 16px）
            Anchored(rt, "InnerTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 1), thin);
            Anchored(rt, "InnerBottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(0, 1), thin);
            Anchored(rt, "InnerLeft", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(1, 0), thin);
            Anchored(rt, "InnerRight", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(1, 0), thin);

            // 四角亮青角标（长 64、粗 3）
            const float len = 64f, thick = 3f, inset = 8f;
            Anchored(rt, "F_TL_H", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(inset, -inset), new Vector2(len, thick), bright);
            Anchored(rt, "F_TL_V", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(inset, -inset), new Vector2(thick, len), bright);
            Anchored(rt, "F_TR_H", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-inset, -inset), new Vector2(len, thick), bright);
            Anchored(rt, "F_TR_V", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-inset, -inset), new Vector2(thick, len), bright);
            Anchored(rt, "F_BL_H", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(inset, inset), new Vector2(len, thick), bright);
            Anchored(rt, "F_BL_V", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(inset, inset), new Vector2(thick, len), bright);
            Anchored(rt, "F_BR_H", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-inset, inset), new Vector2(len, thick), bright);
            Anchored(rt, "F_BR_V", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-inset, inset), new Vector2(thick, len), bright);
        }

        TableStyle MakeStyle(int fontSize, float rowHeight, float headerHeight, float padding)
        {
            return new TableStyle
            {
                font = font,
                fontSize = fontSize,
                rowHeight = rowHeight,
                headerHeight = headerHeight,
                padding = padding
            };
        }

        public static Font LoadBuiltinFont()
        {
            Font result = null;

            try { result = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { /* 老版本 Unity 用 Arial.ttf */ }

            if (result == null)
            {
                try { result = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { }
            }

            if (result == null)
                result = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 16);

            return result;
        }
    }
}
