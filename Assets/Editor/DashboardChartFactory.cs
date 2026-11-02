using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 用纯 uGUI 元素搭出 4 个图表（柱状/进度条/折线/调度示意图）。
    /// 不加载任何图片资源，只用 Image 纯色块 + Text + 自绘折线；
    /// 每个数据元都带悬停提示。
    /// </summary>
    public static class DashboardChartFactory
    {
        // ---- 亮青蓝主题：图表用青色系，配深色文字 ----
        public static readonly Color Blue = new Color(0.20f, 0.76f, 0.92f);        // #33C2EB 亮青
        public static readonly Color BlueLight = new Color(0.48f, 0.90f, 0.99f);    // #7AE6FC
        public static readonly Color Violet = new Color(0.16f, 0.80f, 0.70f);       // #29CCB3 青绿
        public static readonly Color VioletLight = new Color(0.42f, 0.92f, 0.84f);  // #6BEBD6
        static Sprite _roundedBar;
        // 圆角柱体精灵（Assets/Resources/bar_round.png，运行时加载）
        public static Sprite RoundedBar { get { if (_roundedBar == null) _roundedBar = Resources.Load<Sprite>("bar_round"); return _roundedBar; } }
        static Sprite _ringSprite;
        // 圆环精灵（Assets/Resources/ring.png），用于圆环 KPI
        public static Sprite RingSprite { get { if (_ringSprite == null) _ringSprite = Resources.Load<Sprite>("ring"); return _ringSprite; } }
        static Sprite _roundedBarGrad;
        // 竖向渐变圆头柱（白底 alpha 上实下虚，颜色由 Image.color 着色）
        public static Sprite RoundedBarGrad { get { if (_roundedBarGrad == null) _roundedBarGrad = Resources.Load<Sprite>("bar_round_grad"); return _roundedBarGrad; } }
        static Sprite _fillGrad;
        // 横向渐变进度条（左深青→右亮青，本身带色，Image.color 需为白色）
        public static Sprite FillGrad { get { if (_fillGrad == null) _fillGrad = Resources.Load<Sprite>("bar_fill_grad"); return _fillGrad; } }
        public static readonly Color Orange = new Color(0.98f, 0.70f, 0.24f);      // #FAB33D
        public static readonly Color Green = new Color(0.26f, 0.87f, 0.62f);       // #42DE9E
        public static readonly Color TextColor = new Color(0.905f, 0.950f, 0.980f);
        public static readonly Color Muted = new Color(0.620f, 0.740f, 0.850f);
        public static readonly Color GridColor = new Color(0.70f, 0.88f, 1f, 0.10f);
        public static readonly Color Track = new Color(1f, 1f, 1f, 0.10f);
        public static readonly Color EdgeColor = new Color(0.30f, 0.70f, 0.95f, 0.35f);

        // ------------------------------------------------------------------ Image2：AGV 实时调度示意

        class Node
        {
            public string name;
            public float x;
            public float y;
            public string tip;
        }

        public static RectTransform BuildAgvDispatch(RectTransform parent, List<AgvRowData> agvs, Font font)
        {
            RectTransform root = CreateRoot(parent, "__Table_AGV实时调度", 12f);
            float width = root.rect.width;
            float height = root.rect.height;
            ChartTooltip tooltip = CreateTooltip(root, font);

            float legendX = 0f;
            legendX = Legend(root, font, legendX, 4f, Blue, "配送中");
            legendX = Legend(root, font, legendX, 4f, Orange, "充电中");
            Legend(root, font, legendX, 4f, Green, "待命");

            HeaderKpi(root, font, "12 项", "执行中任务", 4f, 20);

            float diagramTop = 32f;
            float diagramHeight = height - diagramTop - 64f;

            Node[] nodes =
            {
                new Node { name = "原料库", x = 0.08f, y = 0.22f, tip = "原料库\n待命 AGV 2 台" },
                new Node { name = "线边仓", x = 0.38f, y = 0.12f, tip = "线边仓\n配送中 AGV 2 台" },
                new Node { name = "装配线", x = 0.35f, y = 0.62f, tip = "装配线\n配送中 AGV 1 台" },
                new Node { name = "成品区", x = 0.68f, y = 0.54f, tip = "成品区\n避让中 AGV 1 台" },
                new Node { name = "充电区", x = 0.82f, y = 0.82f, tip = "充电区\n充电中 AGV 1 台" }
            };

            Vector2[] center = new Vector2[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
                center[i] = new Vector2(nodes[i].x * width, diagramTop + nodes[i].y * diagramHeight);

            // 连线先画，节点压在上面
            Edge(root, center[0], center[1]);
            Edge(root, center[1], center[2]);
            Edge(root, center[0], center[2]);
            Edge(root, center[2], center[3]);
            Edge(root, center[3], center[1]);

            // AGV 位置点
            DrawAgvDot(root, font, Vector2.Lerp(center[0], center[1], 0.45f), Blue, "AGV-05",
                       tooltip, "AGV-05\n配送料箱\n原料库 → 线边仓\n电量 81%");
            DrawAgvDot(root, font, Vector2.Lerp(center[1], center[2], 0.55f), Blue, "AGV-02",
                       tooltip, "AGV-02\n配送料箱\n线边仓 → 装配线\n电量 76%");
            DrawAgvDot(root, font, Vector2.Lerp(center[2], center[3], 0.50f), Amber(), "AGV-07",
                       tooltip, "AGV-07\n低电量返航\n电量 12%");
            DrawAgvDot(root, font, center[4] + new Vector2(34f, 0f), Orange, "AGV-04",
                       tooltip, "AGV-04\n充电中\n电量 34%");

            for (int i = 0; i < nodes.Length; i++)
                DrawNode(root, font, center[i], nodes[i].name, tooltip, nodes[i].tip);

            // 底部指标
            string[] values = { "8/10", "76%", "98.6%", "78%" };
            string[] labels = { "在线车辆", "平均电量", "配送准确率", "车辆利用率" };

            float kpiY = height - 56f;
            float column = width / values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                Txt(root, "KpiValue", values[i], 14, TextColor, TextAnchor.MiddleCenter,
                    i * column, kpiY, column, 20f, font, FontStyle.Bold);
                Txt(root, "KpiLabel", labels[i], 9, Muted, TextAnchor.MiddleCenter,
                    i * column, kpiY + 19f, column, 12f, font);
            }

            Txt(root, "Warning", "● AGV-07 低电量返航，预计 6 分钟恢复", 9, Orange, TextAnchor.MiddleLeft,
                0f, height - 18f, width, 12f, font);

            return root;
        }

        static void DrawNode(RectTransform root, Font font, Vector2 center, string text,
                             ChartTooltip tooltip, string tip)
        {
            const float w = 86f, h = 24f;
            Image border = Img(root, "NodeBorder", new Color(0.30f, 0.70f, 0.95f, 0.55f),
                               center.x - w * 0.5f, center.y - h * 0.5f, w, h, true);
            AttachTooltip(border, tooltip, tip);

            Img(root, "NodeFill", new Color(0.055f, 0.180f, 0.300f, 0.92f),
                center.x - w * 0.5f + 1f, center.y - h * 0.5f + 1f, w - 2f, h - 2f);

            Txt(root, "NodeText", text, 11, TextColor, TextAnchor.MiddleCenter,
                center.x - w * 0.5f, center.y - h * 0.5f, w, h, font);
        }

        static void DrawAgvDot(RectTransform root, Font font, Vector2 position, Color color,
                               string label, ChartTooltip tooltip, string tip)
        {
            Image dot = Img(root, "AgvDot", color, position.x - 3.5f, position.y - 3.5f, 7f, 7f, true);
            AttachTooltip(dot, tooltip, tip);

            Txt(root, "AgvLabel", label, 8, color, TextAnchor.MiddleLeft,
                position.x + 6f, position.y - 7f, 60f, 14f, font);
        }

        static Color Amber()
        {
            return new Color(0.96f, 0.69f, 0.20f, 1f);
        }

        static void Edge(RectTransform root, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.01f) return;

            float angle = Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg;

            RectTransform rect = Panel(root, "Edge", a.x, a.y, length, 1.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(a.x, -a.y);
            rect.localEulerAngles = new Vector3(0f, 0f, angle);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = EdgeColor;
            image.raycastTarget = false;
        }

        // ------------------------------------------------------------------ Image3：订单负荷与产能匹配

        public static RectTransform BuildLoadChart(RectTransform parent, List<LoadRowData> rows, Font font)
        {
            RectTransform root = CreateRoot(parent, "__Table_订单负荷与产能匹配", 10f);
            float width = root.rect.width;
            float height = root.rect.height;
            ChartTooltip tooltip = CreateTooltip(root, font);

            float legendX = 0f;
            legendX = Legend(root, font, legendX, 6f, Blue, "计划工时");
            Legend(root, font, legendX, 6f, Orange, "可用产能");

            float maxValue = 600f;

            float totalPlan = 0f, totalCapacity = 0f;
            foreach (LoadRowData row in rows)
            {
                totalPlan += row.planHours;
                totalCapacity += row.capacity;
            }
            float overall = totalCapacity > 0f ? totalPlan * 100f / totalCapacity : 0f;
            HeaderKpi(root, font, overall.ToString("0.0") + "%", "综合负荷率", 4f, 24);

            float top = 46f, bottom = 24f, left = 30f, right = 38f;
            float plotWidth = width - left - right;
            float plotHeight = height - top - bottom;

            for (int i = 0; i <= 3; i++)
            {
                float value = i * 200f;
                float y = top + plotHeight - value / maxValue * plotHeight;
                Img(root, "Grid", GridColor, left, y, plotWidth, 1f);
                Txt(root, "YLabel", ((int)value).ToString(), 10, Muted, TextAnchor.MiddleRight,
                    0f, y - 8f, left - 6f, 16f, font);
            }

            int count = rows.Count;
            float slot = plotWidth / count;
            float barWidth = Mathf.Min(24f, slot * 0.44f);

            int peakIndex = 0;
            for (int i = 1; i < count; i++)
                if (rows[i].planHours > rows[peakIndex].planHours) peakIndex = i;

            for (int i = 0; i < count; i++)
            {
                LoadRowData row = rows[i];
                float barHeight = Mathf.Max(1f, row.planHours / maxValue * plotHeight);
                float x = left + slot * i + (slot - barWidth) * 0.5f;
                float y = top + plotHeight - barHeight;

                Image bar = Img(root, "Bar", i == peakIndex ? BlueLight : Blue,
                                x, y, barWidth, barHeight, true);
                // 竖向渐变圆头柱：顶部实色、底部渐隐（Simple 让渐变铺满整根柱）
                if (RoundedBarGrad != null) { bar.sprite = RoundedBarGrad; bar.type = Image.Type.Simple; }
                AttachTooltip(bar, tooltip,
                    row.date + "\n计划工时 " + row.planHours + " h\n可用产能 " + row.capacity +
                    " h\n负载率 " + row.RateText);

                Txt(root, "XLabel", row.date, 10, Muted, TextAnchor.MiddleCenter,
                    left + slot * i, top + plotHeight + 6f, slot, 14f, font);
            }

            // 峰值标注
            LoadRowData peak = rows[peakIndex];
            float peakBarHeight = peak.planHours / maxValue * plotHeight;
            float peakCenterX = left + slot * peakIndex + slot * 0.5f;
            Txt(root, "Peak", "峰值 " + peak.planHours + "h", 10, TextColor, TextAnchor.MiddleCenter,
                peakCenterX - 50f, top + plotHeight - peakBarHeight - 18f, 100f, 14f, font);

            // 可用产能线
            float capacityValue = rows[0].capacity;
            float capacityY = top + plotHeight - capacityValue / maxValue * plotHeight;
            Img(root, "CapacityLine", Orange, left, capacityY, plotWidth, 1.5f);
            Txt(root, "CapacityLabel", capacityValue + " h", 10, Orange, TextAnchor.MiddleLeft,
                width - right + 4f, capacityY - 8f, right - 4f, 16f, font);

            return root;
        }

        // ------------------------------------------------------------------ Image4：AGV 配送任务进度

        public static RectTransform BuildRouteChart(RectTransform parent, List<RouteRowData> rows, Font font)
        {
            RectTransform root = CreateRoot(parent, "__Table_AGV配送任务进度", 10f);
            float width = root.rect.width;
            float height = root.rect.height;
            ChartTooltip tooltip = CreateTooltip(root, font);

            float legendX = 0f;
            legendX = Legend(root, font, legendX, 6f, new Color(0.55f, 0.65f, 0.78f, 0.55f), "计划任务");
            Legend(root, font, legendX, 6f, Blue, "已完成任务");

            int totalPlanned = 0, totalCompleted = 0;
            foreach (RouteRowData row in rows)
            {
                totalPlanned += row.planned;
                totalCompleted += row.completed;
            }
            float overall = totalPlanned > 0 ? totalCompleted * 100f / totalPlanned : 0f;
            HeaderKpi(root, font, overall.ToString("0.0") + "%", "今日完成率", 4f, 24);

            float top = 42f;
            float rowHeight = (height - top - 6f) / Mathf.Max(1, rows.Count);
            const float labelWidth = 128f;
            float trackX = labelWidth;
            float trackWidth = width - labelWidth - 54f;
            const float trackHeight = 12f;

            for (int i = 0; i < rows.Count; i++)
            {
                RouteRowData row = rows[i];
                float y = top + i * rowHeight;

                Txt(root, "Route", row.route, 13, TextColor, TextAnchor.MiddleLeft,
                    0f, y + 2f, labelWidth - 8f, 18f, font);
                Txt(root, "Plan", "计划 " + row.planned + " 单", 9, Muted, TextAnchor.MiddleLeft,
                    0f, y + 20f, labelWidth - 8f, 13f, font);

                float barY = y + 12f;
                Image track = Img(root, "Track", Track, trackX, barY, trackWidth, trackHeight, true);
                float fillWidth = Mathf.Max(2f, trackWidth * row.rate / 100f);
                Image fill = Img(root, "Fill", Color.white, trackX, barY, fillWidth, trackHeight);
                if (FillGrad != null) fill.sprite = FillGrad;   // 横向渐变：左深青→右亮青

                float percentX = trackX + fillWidth + 7f;
                if (percentX + 46f > width) percentX = width - 46f;
                Txt(root, "Percent", row.RateText, 11, BlueLight, TextAnchor.MiddleLeft,
                    percentX, barY - 3f, 44f, 16f, font);

                AttachTooltip(track, tooltip,
                    row.route + "\n计划任务 " + row.planned + " 单\n已完成 " + row.completed +
                    " 单\n完成率 " + row.RateText + "\n状态 " + row.status);
            }

            return root;
        }

        // ------------------------------------------------------------------ Image5：物料库存趋势

        public static RectTransform BuildInventoryChart(RectTransform parent, List<InventoryRowData> rows, Font font)
        {
            RectTransform root = CreateRoot(parent, "__Table_物料库存趋势", 8f);
            float width = root.rect.width;
            float height = root.rect.height;

            float legendX = 0f;
            legendX = Legend(root, font, legendX, 2f, Blue, "原材料");
            legendX = Legend(root, font, legendX, 2f, Orange, "线边库存");
            Legend(root, font, legendX, 2f, Green, "成品");

            int total = rows.Count > 0 ? rows[rows.Count - 1].Total : 0;
            HeaderKpi(root, font, total.ToString("N0"), "当前总库存", 4f, 22);

            // 只保留三个圆环 KPI，不画折线 / 网格 / 坐标轴
            if (rows.Count > 0)
            {
                InventoryRowData d = rows[rows.Count - 1];

                float ringSize = Mathf.Min(124f, height - 56f);
                if (ringSize < 60f) ringSize = 60f;

                float ringGap = 26f;
                float allWidth = ringSize * 3f + ringGap * 2f;
                float originX = (width - allWidth) * 0.5f;
                float originY = Mathf.Max(50f, (height - ringSize) * 0.5f + 14f);

                const float norm = 900f;
                DrawRing(root, font, originX, originY, ringSize, d.raw / norm, Blue, d.raw.ToString("N0"), "原材料");
                DrawRing(root, font, originX + ringSize + ringGap, originY, ringSize, d.lineSide / norm, Orange, d.lineSide.ToString("N0"), "线边库存");
                DrawRing(root, font, originX + (ringSize + ringGap) * 2f, originY, ringSize, d.finished / norm, Green, d.finished.ToString("N0"), "成品");
            }

            return root;
        }

        /// <summary>
        /// 圆环 KPI：ring.png 用 Filled/Radial360 填充进度，中间放数值 + 标签。
        /// </summary>
        static void DrawRing(RectTransform root, Font font, float x, float y, float size,
                             float percent, Color color, string value, string label)
        {
            Sprite ring = RingSprite;

            Image track = Img(root, "RingTrack", new Color(0.30f, 0.55f, 0.75f, 0.22f), x, y, size, size);
            Image arc = Img(root, "RingArc", color, x, y, size, size);

            if (ring != null)
            {
                track.sprite = ring;
                track.type = Image.Type.Filled;
                track.fillMethod = Image.FillMethod.Radial360;
                track.fillOrigin = 2;
                track.fillAmount = 1f;

                arc.sprite = ring;
                arc.type = Image.Type.Filled;
                arc.fillMethod = Image.FillMethod.Radial360;
                arc.fillOrigin = 2;
                arc.fillAmount = Mathf.Clamp01(percent);
            }

            int valueFont = Mathf.Max(12, Mathf.RoundToInt(size * 0.19f));
            int labelFont = Mathf.Max(9, Mathf.RoundToInt(size * 0.10f));

            Txt(root, "RingValue", value, valueFont, TextColor, TextAnchor.MiddleCenter,
                x, y + size * 0.33f, size, size * 0.26f, font, FontStyle.Bold);
            Txt(root, "RingLabel", label, labelFont, Muted, TextAnchor.MiddleCenter,
                x, y + size * 0.58f, size, size * 0.20f, font);
        }

        static void DrawSeries(RectTransform root, List<InventoryRowData> rows,
                               System.Func<InventoryRowData, int> selector, Color color,
                               float left, float top, float plotWidth, float plotHeight,
                               float maxValue)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < rows.Count; i++)
            {
                float x = rows.Count > 1 ? (float)i / (rows.Count - 1) : 0f;
                float y = Mathf.Clamp01(selector(rows[i]) / maxValue);
                points.Add(new Vector2(x, y));
            }

            RectTransform rect = Panel(root, "Line", left, top, plotWidth, plotHeight);
            var graphic = rect.gameObject.AddComponent<UILineGraphic>();
            graphic.color = color;
            graphic.raycastTarget = false;
            graphic.SetData(points, 2f, 4f, color);
        }

        static void DrawEndLabel(RectTransform root, Font font, int value, Color color,
                                 float width, float left, float top, float plotHeight,
                                 float maxValue, float right)
        {
            float y = top + plotHeight - Mathf.Clamp01(value / maxValue) * plotHeight;
            Txt(root, "EndLabel", value.ToString(), 9, color, TextAnchor.MiddleLeft,
                left + (width - left - right) + 4f, y - 7f, right - 4f, 14f, font);
        }

        // ------------------------------------------------------------------ 公共构建工具

        static RectTransform CreateRoot(RectTransform parent, string name, float padding)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        static Image Img(Transform parent, string name, Color color,
                         float x, float y, float width, float height, bool raycast = false)
        {
            RectTransform rect = Panel(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        static Text Txt(Transform parent, string name, string content, int size, Color color,
                        TextAnchor align, float x, float y, float width, float height,
                        Font font, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = Panel(parent, name, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = style;
            text.text = content;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            return text;
        }

        static ChartTooltip CreateTooltip(RectTransform root, Font font)
        {
            RectTransform panel = Panel(root, "Tooltip", 0f, 0f, 210f, 74f);

            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.050f, 0.140f, 0.240f, 0.96f);
            background.raycastTarget = false;

            Text label = Txt(panel, "Text", "", 10, TextColor, TextAnchor.UpperLeft,
                             8f, 6f, 194f, 62f, font);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            var tooltip = panel.gameObject.AddComponent<ChartTooltip>();
            tooltip.panel = panel;
            tooltip.label = label;
            tooltip.Init(root, root.GetComponentInParent<Canvas>());
            return tooltip;
        }

        static void AttachTooltip(Image target, ChartTooltip tooltip, string content)
        {
            if (target == null || tooltip == null) return;

            var trigger = target.gameObject.AddComponent<ChartTooltipTrigger>();
            trigger.tooltip = tooltip;
            trigger.content = content;
        }

        static float Legend(RectTransform root, Font font, float x, float y, Color color, string text)
        {
            Img(root, "LegendDot", color, x, y + 4f, 8f, 8f);
            Txt(root, "LegendText", text, 11, Muted, TextAnchor.MiddleLeft,
                x + 13f, y, 92f, 16f, font);

            return x + 13f + text.Length * 11f + 18f;
        }

        static void HeaderKpi(RectTransform root, Font font, string value, string label,
                              float rightPadding, int valueSize)
        {
            float width = 120f;
            float x = root.rect.width - rightPadding - width;

            Txt(root, "KpiValue", value, valueSize, TextColor, TextAnchor.MiddleRight,
                x, 0f, width, 30f, font, FontStyle.Bold);
            Txt(root, "KpiLabel", label, 10, Muted, TextAnchor.MiddleRight,
                x, 30f, width, 14f, font);
        }
    }
}
