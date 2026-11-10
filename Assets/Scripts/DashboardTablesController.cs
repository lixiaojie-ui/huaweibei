using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 大屏内容控制器（Canvas main1）：
    /// - Image1：订单明细（沿用原手工表格，运行时只更新文字与原有配色）
    /// - Image2：AGV 实时调度示意图（沿用原手工示意图，运行时只更新 KPI 与告警）
    /// - Image3：各产线日计划与产能缺口（代码生成）
    /// - Image4：各工序配方执行进度（代码生成）
    /// - Image5：物料库存趋势（沿用原手工三环图，运行时只更新数值与圆环比例）
    ///
    /// 数据库对接后不再重建 Image1/Image2/Image5 的外观，避免改变原有
    /// 框线、图片、字号和配色；只把 API 数据写进原有控件。
    /// </summary>
    [DisallowMultipleComponent]
    public class DashboardTablesController : MonoBehaviour
    {
        public const string TablePrefix = "__Table_";
        public const int CurrentContentVersion = 16;

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

        // 面板原始底色（与场景原有大屏玻璃底一致），用于恢复被上一版代码改深的颜色
        static readonly Color OriginalPanelFill = new Color(0.055f, 0.180f, 0.300f, 0.23921569f);

        static readonly Color PlainText = new Color(0.905f, 0.950f, 0.980f, 1f);
        static readonly Color EmptyWarning = new Color(0.905f, 0.950f, 0.980f, 0.45f);
        static readonly Color AgvAlertRed = new Color(1f, 0.42f, 0.42f, 1f);

        // 物料库存模块原有配色（恢复被上一版代码提亮/加粗的字体与图例）
        static readonly Color LegacyMutedText = new Color(0.620f, 0.740f, 0.850f, 1f);
        static readonly Color LegacyLegendText = new Color(0.84313726f, 0.8784314f, 0.9137255f, 1f);
        static readonly Color LegacyLegendRaw = new Color(0.200f, 0.760f, 0.920f, 1f);
        static readonly Color LegacyLegendLine = new Color(0.980f, 0.700f, 0.240f, 1f);
        static readonly Color LegacyLegendFinished = new Color(0.260f, 0.870f, 0.620f, 1f);

        static readonly ColumnDef[] OrderColumns =
        {
            new ColumnDef("订单号", 1.45f, CellRole.Text),
            new ColumnDef("饲料品种", 1.70f, CellRole.Text),
            new ColumnDef("数量", 0.55f, CellRole.Number),
            new ColumnDef("交货时间", 1.20f, CellRole.Date),
            new ColumnDef("成品库存", 1.25f, CellRole.Number),
            new ColumnDef("状态", 0.90f, CellRole.Status),
            new ColumnDef("预警", 1.10f, CellRole.Warning)
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

        /// <summary>刷新 Image1~Image5 的内容：原手工模块只更新数据，右侧两张图重建。</summary>
        [ContextMenu("重新生成表格与图表")]
        public void BuildAll()
        {
            if (font == null) font = LoadBuiltinFont();

            if (image1 == null || image2 == null || image3 == null || image4 == null || image5 == null)
            {
                Debug.LogError("[DashboardTables] 挂载点未配置完整：请指定 Canvas left/Image1、Image2 和 Canvas right/Image3~Image5。", this);
                return;
            }

            // 只清理上一版代码生成的 __Table_ 内容，保留原手工模块
            ClearAll();

            // 恢复五个模块原有底色与原来的边框对象（上一版曾改成深色半透明并额外画了青色框）
            RestorePanelFill(image1);
            RestorePanelFill(image2);
            RestorePanelFill(image3);
            RestorePanelFill(image4);
            RestorePanelFill(image5);
            EnsureActive(image1, "框1");
            EnsureActive(image2, "框2");
            EnsureActive(image3, "right-frame1");
            EnsureActive(image4, "right-frame2");
            EnsureActive(image5, "right-frame3");

            // 数据库数据：GET /api/dashboard?days=7；失败时自动回退本地缓存 / 演示数据
            DashboardSnapshot snapshot = PlanApiClient.LoadSnapshot();
            if (snapshot != null)
            {
                Debug.Log(string.Format(
                    "[DashboardTables] 已加载数据库数据：订单 {0} 条 · 产线 {1} 天 · 工序 {2} 条 · 库存 {3} 天 · 生成时间 {4}",
                    snapshot.orderCount, snapshot.loadDayCount, snapshot.recipeCount,
                    snapshot.inventoryDayCount, snapshot.generatedAt), this);
            }
            else
            {
                Debug.LogWarning("[DashboardTables] 数据库接口不可用（" + PlanApiClient.LastError +
                                 "），main1 已回退演示数据。", this);
            }

            // 订单明细：写入原手工表格；只有找不到原表格时才退回代码生成
            if (!BindLegacyOrderTable(snapshot != null ? snapshot.orders : null))
            {
                BuildOrderTable(snapshot != null ? snapshot.orders : null);
            }

            // AGV 实时调度：写入原手工示意图
            if (!BindLegacyAgv(snapshot != null ? snapshot.agv : null,
                               snapshot != null ? snapshot.agvAlert : null))
            {
                List<AgvRowData> agvs = DashboardMockData.CreateAgvStatus();
                generatedRoots.Add(DashboardChartFactory.BuildAgvDispatch(
                    image2, agvs, font,
                    snapshot != null ? snapshot.agv : null,
                    snapshot != null ? snapshot.agvAlert : null));
            }

            // 右侧两张图沿用原先的生成方式与样式
            List<LoadRowData> loads = snapshot != null && snapshot.loads.Count > 0
                ? snapshot.loads
                : DashboardMockData.CreateLoad();
            generatedRoots.Add(DashboardChartFactory.BuildLoadChart(
                image3, loads, font, snapshot != null ? snapshot.load : null));

            List<RecipeRowData> recipes = snapshot != null && snapshot.recipes.Count > 0
                ? snapshot.recipes
                : DashboardMockData.CreateRecipeProgress();
            generatedRoots.Add(DashboardChartFactory.BuildRecipeChart(image4, recipes, font));

            // 物料库存趋势：写入原手工三环图
            if (!BindLegacyInventory(snapshot != null ? snapshot.inventoryKpi : null))
            {
                List<InventoryRowData> inventories = snapshot != null && snapshot.inventories.Count > 0
                    ? snapshot.inventories
                    : DashboardMockData.CreateInventory();
                generatedRoots.Add(DashboardChartFactory.BuildInventoryChart(
                    image5, inventories, font, snapshot != null ? snapshot.inventoryKpi : null));
            }

            contentVersion = CurrentContentVersion;
        }

        /// <summary>删除本项目生成的 __Table_ 内容（不会动原有手工模块、边框和面板本身）。</summary>
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

            ClearScreenFrame();
        }

        /// <summary>删除上一版在 Canvas_mask 上额外加的全屏装饰框。</summary>
        static void ClearScreenFrame()
        {
            GameObject maskRoot = GameObject.Find("Canvas_mask");
            if (maskRoot == null) return;

            Transform old = maskRoot.transform.Find(TablePrefix + "ScreenFrame");
            if (old == null) return;

            old.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        static void RestorePanelFill(RectTransform panel)
        {
            if (panel == null) return;

            Image bg = panel.GetComponent<Image>();
            if (bg != null) bg.color = OriginalPanelFill;
        }

        static void EnsureActive(RectTransform panel, string childName)
        {
            Transform child = FindChildRecursive(panel, childName);
            if (child != null) child.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- 订单明细（原手工表格）

        bool BindLegacyOrderTable(List<OrderRowData> source)
        {
            Transform table = FindChildRecursive(image1, "表格内容1");
            if (table == null) return false;

            table.gameObject.SetActive(true);
            orders = source != null && source.Count > 0 ? source : DashboardMockData.CreateOrders();

            Transform content = table.Find("Scroll/Viewport/Content");
            if (content == null) content = FindChildRecursive(table, "Content");
            if (content == null) return false;

            const float rowHeight = 38f;
            const int maxRows = 20;

            for (int i = 0; i < maxRows; i++)
            {
                Transform row = content.Find("Row_" + (i + 1).ToString("00", CultureInfo.InvariantCulture));
                if (row == null) continue;

                bool used = i < orders.Count;
                row.gameObject.SetActive(used);
                if (!used) continue;

                OrderRowData data = orders[i];
                SetLegacyCell(row, "C_订单号", data.orderId);
                SetLegacyCell(row, "C_产品型号", data.model);
                SetLegacyCell(row, "C_数量", data.quantity.ToString(CultureInfo.InvariantCulture));
                SetLegacyCell(row, "C_交货时间", data.delivery);
                SetLegacyCell(row, "C_库存", data.stock.ToString(CultureInfo.InvariantCulture));
                SetLegacyStatusCell(row, "C_状态", data.status);
                SetLegacyWarningCell(row, "C_预警", data.warning);
            }

            ScrollRect scroll = table.GetComponentInChildren<ScrollRect>(true);

            RectTransform contentRect = content as RectTransform;
            if (contentRect != null)
                EnsureLoopScroll(contentRect, maxRows, rowHeight, scroll);

            if (scroll != null) scroll.verticalNormalizedPosition = 1f;

            return true;
        }

        /// <summary>
        /// 把有效行复制若干份拼在下方，让内容始终高于视口。
        /// 滚到底部时看到的副本与顶部原始行完全一致，回顶的瞬间视觉上没有跳变。
        /// </summary>
        static void EnsureLoopScroll(RectTransform content, int maxRows, float fallbackRowHeight, ScrollRect scroll)
        {
            if (content == null) return;

            // 清掉上一次生成的副本，避免重复叠加
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                if (child.name.EndsWith("_dup", System.StringComparison.Ordinal))
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(child.gameObject);
                    else UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            // 统计表格里真实存在、并且已启用的行
            var rows = new List<Transform>();
            for (int i = 1; i <= maxRows; i++)
            {
                Transform row = content.Find("Row_" + i.ToString("00", CultureInfo.InvariantCulture));
                if (row != null && row.gameObject.activeSelf) rows.Add(row);
            }
            if (rows.Count == 0) return;

            RectTransform firstRect = rows[0] as RectTransform;
            float rowHeight = firstRect != null ? firstRect.sizeDelta.y : fallbackRowHeight;

            float span = rowHeight;
            if (firstRect != null && rows.Count > 1)
                span = Mathf.Abs(firstRect.anchoredPosition.y - ((RectTransform)rows[rows.Count - 1]).anchoredPosition.y) + rowHeight;

            float viewportHeight = scroll != null && scroll.viewport != null ? scroll.viewport.rect.height : 0f;

            int copies = 1;
            while (copies < 4 && span * (copies + 1) < viewportHeight + rowHeight * 2f) copies++;

            for (int k = 1; k <= copies; k++)
            {
                foreach (Transform row in rows)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(row.gameObject, content, true);
                    clone.name = row.name + "_dup";

                    RectTransform rt = clone.GetComponent<RectTransform>();
                    RectTransform source = row as RectTransform;
                    rt.anchoredPosition = new Vector2(source.anchoredPosition.x, source.anchoredPosition.y - span * k);

                    // 副本只用于展示，关掉编辑交互
                    foreach (EditableCell cell in clone.GetComponentsInChildren<EditableCell>(true))
                        cell.enabled = false;
                }
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x, span * (copies + 1));
        }

        static void SetLegacyCell(Transform row, string cellName, string value)
        {
            Transform cell = row.Find(cellName);
            if (cell == null) return;

            Text label = FindCellLabel(cell);
            if (label != null)
            {
                label.text = value;
                label.color = PlainText;
            }

            Image bg = cell.GetComponent<Image>();
            if (bg != null) bg.color = Color.clear;

            SyncEditorText(cell, value);
        }

        static void SetLegacyStatusCell(Transform row, string cellName, string value)
        {
            Transform cell = row.Find(cellName);
            if (cell == null) return;

            Color color = TableStyle.StatusColor(value);

            Text label = FindCellLabel(cell);
            if (label != null)
            {
                label.text = value;
                label.color = Color.Lerp(color, Color.white, 0.35f);
            }

            Image bg = cell.GetComponent<Image>();
            if (bg != null) bg.color = new Color(color.r, color.g, color.b, 0.22f);

            SyncEditorText(cell, value);
        }

        static void SetLegacyWarningCell(Transform row, string cellName, string value)
        {
            Transform cell = row.Find(cellName);
            if (cell == null) return;

            Text label = FindCellLabel(cell);
            if (label != null)
            {
                label.text = value;
                label.color = string.IsNullOrEmpty(value)
                    ? EmptyWarning
                    : TableStyle.WarningColor(value);

                // 预警列保持常亮，不做闪烁
                WarningBlink blink = label.GetComponent<WarningBlink>();
                if (blink != null) blink.enabled = false;
            }

            SyncEditorText(cell, value);
        }

        static Text FindCellLabel(Transform cell)
        {
            Transform label = cell.Find("Label");
            if (label != null)
            {
                Text text = label.GetComponent<Text>();
                if (text != null) return text;
            }
            return cell.GetComponentInChildren<Text>(true);
        }

        static void SyncEditorText(Transform cell, string value)
        {
            Transform editor = cell.Find("Editor");
            if (editor == null) return;

            InputField field = editor.GetComponent<InputField>();
            if (field != null) field.SetTextWithoutNotify(value);

            Transform editText = editor.Find("Text");
            if (editText != null)
            {
                Text text = editText.GetComponent<Text>();
                if (text != null) text.text = value;
            }
        }

        // ---------------------------------------------------------------- AGV 实时调度（原手工示意图）

        bool BindLegacyAgv(AgvKpiData kpi, string alertText)
        {
            Transform root = FindChildRecursive(image2, "表格内容2");
            if (root == null) return false;

            root.gameObject.SetActive(true);

            Text[] texts = root.GetComponentsInChildren<Text>(true);

            if (kpi != null)
            {
                BindAgvKpi(texts, "在线车辆", "在线车辆",
                           kpi.running.ToString(CultureInfo.InvariantCulture) + "/" +
                           kpi.total.ToString(CultureInfo.InvariantCulture));
                BindAgvKpi(texts, "平均电量", "平均负载", FormatPercent(kpi.avgLoad));
                BindAgvKpi(texts, "平均负载", "平均负载", FormatPercent(kpi.avgLoad));
                BindAgvKpi(texts, "配送准确率", "配送达成率", FormatPercent(kpi.taskExec));
                BindAgvKpi(texts, "配送达成率", "配送达成率", FormatPercent(kpi.taskExec));
                BindAgvKpi(texts, "车辆利用率", "车辆完好率", FormatPercent(kpi.onlineRate));
                BindAgvKpi(texts, "车辆完好率", "车辆完好率", FormatPercent(kpi.onlineRate));

                Text topValue = FindTopText(texts, "KpiValue");
                if (topValue != null)
                    topValue.text = kpi.tasksExecuting.ToString(CultureInfo.InvariantCulture) + " 项";
            }

            Transform warning = FindChildRecursive(root, "Warning");
            if (warning != null)
            {
                Text warningText = warning.GetComponent<Text>();
                if (warningText != null)
                {
                    warningText.text = string.IsNullOrEmpty(alertText)
                        ? "● 暂无 AGV 告警"
                        : "● " + alertText;
                    warningText.color = AgvAlertRed;
                }
            }

            return true;
        }

        static void BindAgvKpi(Text[] texts, string matchLabel, string finalLabel, string value)
        {
            Text label = null;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name == "KpiLabel" && texts[i].text == matchLabel)
                {
                    label = texts[i];
                    break;
                }
            }
            if (label == null) return;

            label.text = finalLabel;

            float x = label.rectTransform.anchoredPosition.x;
            Text valueText = FindNearestText(texts, "KpiValue", x, -100f);
            if (valueText != null) valueText.text = value;
        }

        // ---------------------------------------------------------------- 物料库存趋势（原手工三环图）

        bool BindLegacyInventory(InventoryKpiData kpi)
        {
            Transform root = FindChildRecursive(image5, "物料库存趋势");
            if (root == null) return false;

            root.gameObject.SetActive(true);
            RestoreLegacyInventoryTypography(root);
            if (kpi == null) return true;

            int raw = kpi.raw;
            int lineSide = kpi.lineSide;
            int finished = kpi.finished;
            int total = kpi.total > 0 ? kpi.total : raw + lineSide + finished;

            Text[] texts = root.GetComponentsInChildren<Text>(true);

            Text totalLabel = FindTextByContent(texts, "KpiLabel", "当前总库存");
            if (totalLabel != null)
            {
                Text totalValue = FindNearestText(texts, "KpiValue",
                    totalLabel.rectTransform.anchoredPosition.x, float.NaN);
                if (totalValue != null) totalValue.text = total.ToString("N0", CultureInfo.InvariantCulture);
            }

            BindInventoryRing(root, texts, "原材料", raw, "RingArc1");
            BindInventoryRing(root, texts, "线边库存", lineSide, "RingArc2");
            BindInventoryRing(root, texts, "成品", finished, "RingArc3");

            return true;
        }

        /// <summary>
        /// 物料库存模块沿用原手工三环图：恢复原字号、原文字颜色、原图例圆点大小与颜色。
        /// 上一版代码把 KpiLabel/RingLabel/LegendText 提亮、放大，并放大了图例圆点。
        /// </summary>
        static void RestoreLegacyInventoryTypography(Transform root)
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                string name = text.gameObject.name;

                if (name == "KpiLabel" && text.text == "当前总库存")
                {
                    text.fontSize = 10;
                    text.color = LegacyMutedText;
                }
                else if (name == "RingLabel")
                {
                    text.color = LegacyMutedText;
                }
                else if (name == "LegendText")
                {
                    text.fontSize = 11;
                    text.color = LegacyLegendText;
                }
            }

            // 图例圆点按从左到右还原：原材料（蓝）/ 线边库存（橙）/ 成品（绿）
            List<Image> dots = new List<Image>();
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name == "LegendDot") dots.Add(images[i]);
            }
            dots.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            Color[] colors = { LegacyLegendRaw, LegacyLegendLine, LegacyLegendFinished };
            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].color = colors[Mathf.Min(i, colors.Length - 1)];
                dots[i].rectTransform.sizeDelta = new Vector2(8f, 8f);
            }
        }

        static void BindInventoryRing(Transform root, Text[] texts, string category, int value, string arcPrefix)
        {
            Text label = FindTextByContent(texts, "RingLabel", category);
            if (label == null) return;

            Text valueText = FindNearestText(texts, "RingValue",
                label.rectTransform.anchoredPosition.x, float.NaN);
            if (valueText != null) valueText.text = value.ToString("N0", CultureInfo.InvariantCulture);

            // 原图圆环比例尺为 900，超过 900 按满环显示；保持原图动画与观感，只更新比例
            float fill = Mathf.Clamp01(value / 900f);
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (!images[i].gameObject.name.StartsWith(arcPrefix, StringComparison.Ordinal)) continue;
                images[i].fillAmount = fill;
            }
        }

        // ---------------------------------------------------------------- 通用查找

        static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static Text FindTextByContent(Text[] texts, string objectName, string content)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name == objectName && texts[i].text == content)
                    return texts[i];
            }
            return null;
        }

        static Text FindNearestText(Text[] texts, string objectName, float x, float yBelow)
        {
            Text best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text.gameObject.name != objectName) continue;

                Vector2 position = text.rectTransform.anchoredPosition;
                if (!float.IsNaN(yBelow) && position.y > yBelow) continue;

                float distance = float.IsNaN(x)
                    ? Mathf.Abs(position.y - yBelow)
                    : Mathf.Abs(position.x - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = text;
                }
            }
            return best;
        }

        static Text FindTopText(Text[] texts, string objectName)
        {
            Text best = null;
            float bestY = float.MinValue;

            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text.gameObject.name != objectName) continue;

                float y = text.rectTransform.anchoredPosition.y;
                if (y > bestY)
                {
                    bestY = y;
                    best = text;
                }
            }
            return best;
        }

        static string FormatPercent(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        // ---------------------------------------------------------------- 回退：代码生成订单表

        void BuildOrderTable(List<OrderRowData> source)
        {
            orders = source != null && source.Count > 0 ? source : DashboardMockData.CreateOrders();
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
                    var blink = cell.gameObject.AddComponent<WarningBlink>();
                    blink.label = cell.labelText;
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

        // ---------------------------------------------------------------- 回退表配色 / 校验

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
