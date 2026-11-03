using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 右侧三个图表的悬停数据提示框。
    /// 不依赖场景里已有（已损坏）的 Tooltip 组件，运行时自己创建一个浮层。
    /// </summary>
    public class ChartHoverTooltipView : MonoBehaviour
    {
        RectTransform panel;
        Text label;
        Canvas canvas;
        RectTransform canvasRect;

        public void Build(Canvas targetCanvas)
        {
            canvas = targetCanvas;
            canvasRect = targetCanvas.transform as RectTransform;

            panel = (RectTransform)transform;
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 1f);
            panel.sizeDelta = new Vector2(250f, 116f);
            gameObject.layer = targetCanvas.gameObject.layer;

            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0.050f, 0.140f, 0.240f, 0.96f);
            background.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.layer = gameObject.layer;
            textGo.transform.SetParent(transform, false);

            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            label = textGo.AddComponent<Text>();
            label.font = LoadFont();
            label.fontSize = 12;
            label.color = new Color(0.905f, 0.950f, 0.980f);
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.lineSpacing = 1.1f;

            gameObject.SetActive(false);
        }

        public void Show(string content, Vector2 screenPosition)
        {
            if (panel == null || label == null) return;

            label.text = content;

            int lines = 1;
            for (int i = 0; i < content.Length; i++)
                if (content[i] == '\n') lines++;

            panel.sizeDelta = new Vector2(250f, 26f + lines * 18f);
            gameObject.SetActive(true);
            panel.SetAsLastSibling();

            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 local;
            if (canvasRect == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out local))
                return;

            Vector2 size = panel.sizeDelta;
            Rect area = canvasRect.rect;
            Vector2 position = local + new Vector2(16f, -16f);

            position.x = Mathf.Clamp(position.x, area.xMin + 4f, area.xMax - size.x - 4f);
            position.y = Mathf.Clamp(position.y, area.yMin + size.y + 4f, area.yMax - 4f);

            panel.localPosition = position;
        }

        public void Hide()
        {
            if (panel != null) gameObject.SetActive(false);
        }

        static Font LoadFont()
        {
            Font result = null;

            try { result = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { }
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

    /// <summary>
    /// 运行时给 Image3 / Image4 / Image5 的数据元素挂上悬停数据。
    /// </summary>
    public static class ChartHoverBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Build()
        {
            Transform loadChart = FindChart("订单负荷与产能匹配", "Image3");
            Transform routeChart = FindChart("AGV配送任务进度", "Image4");
            Transform stockChart = FindChart("物料库存趋势", "Image5");
            Transform agvChart = FindChart("AGV实时调度", "Image2");

            // Image2 在 Canvas left，Image3~5 在 Canvas right，各自用所属 Canvas 的提示框
            ChartHoverTooltipView leftView = CreateView(agvChart);

            Transform rightChart = loadChart != null ? loadChart :
                (routeChart != null ? routeChart : stockChart);
            ChartHoverTooltipView rightView = CreateView(rightChart);

            if (leftView != null && agvChart != null)
                BuildAgvChart(agvChart, leftView);

            if (rightView != null)
            {
                if (loadChart != null) BuildLoadChart(loadChart, rightView);
                if (routeChart != null) BuildRouteChart(routeChart, rightView);
                if (stockChart != null) BuildStockChart(stockChart, rightView);
            }
        }

        static ChartHoverTooltipView CreateView(Transform chart)
        {
            if (chart == null) return null;

            Canvas canvas = chart.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            var go = new GameObject("__ChartHoverTooltip", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            var view = go.AddComponent<ChartHoverTooltipView>();
            view.Build(canvas);
            return view;
        }

        // ------------------------------------------------------------------ Image2

        static void BuildAgvChart(Transform root, ChartHoverTooltipView view)
        {
            List<Image> dots = CollectImages(root, "AgvDot");
            dots.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            List<Text> labels = CollectTexts(root, "AgvLabel");
            labels.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            for (int i = 0; i < dots.Count; i++)
            {
                string name = i < labels.Count && labels[i] != null
                    ? labels[i].text.Trim()
                    : "AGV-" + (i + 1).ToString("00");

                string content = AgvContent(name);
                AddHover(dots[i], view, content);
                if (i < labels.Count) AddHover(labels[i], view, content);
            }

            List<Image> nodeFills = CollectImages(root, "NodeFill");
            nodeFills.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            List<Text> nodeTexts = CollectTexts(root, "NodeText");
            nodeTexts.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            for (int i = 0; i < nodeFills.Count; i++)
            {
                string name = i < nodeTexts.Count && nodeTexts[i] != null
                    ? nodeTexts[i].text.Trim()
                    : "节点 " + (i + 1);

                AddHover(nodeFills[i], view, NodeContent(name));
            }
        }

        static string AgvContent(string name)
        {
            switch (name)
            {
                case "AGV-05":
                    return "AGV-05\n状态 配送中\n任务 原料库 → 线边仓\n载重 42%\n电量 81%\n预计 2 分钟到达";
                case "AGV-02":
                    return "AGV-02\n状态 配送中\n任务 线边仓 → 装配线\n载重 68%\n电量 76%\n预计 3 分钟到达";
                case "AGV-07":
                    return "AGV-07\n状态 低电量返航\n任务 返回充电区\n载重 0%\n电量 12%\n预计 1 分钟到达";
                case "AGV-04":
                    return "AGV-04\n状态 充电中\n任务 充电区补能\n电量 34%\n充电功率 3.2 kW\n预计 25 分钟充满";
                default:
                    return name + "\n状态 待命\n载重 0%\n电量 100%";
            }
        }

        static string NodeContent(string name)
        {
            switch (name)
            {
                case "原料库":
                    return "原料库\n状态 正常\n库存 1,242\n今日入库 36 单\n在库 AGV 2 台";
                case "线边仓":
                    return "线边仓\n状态 繁忙\n库存 698\n今日配送 42 单\n在库 AGV 2 台";
                case "装配线":
                    return "装配线\n状态 运行中\n在线工单 24\n产能利用率 87%\n在岗 AGV 1 台";
                case "成品区":
                    return "成品区\n状态 正常\n库存 518\n今日出库 28 单\n在岗 AGV 1 台";
                case "充电区":
                    return "充电区\n状态 空闲\n充电中 1 台\n可用充电位 3\n平均充电 22 分钟";
                default:
                    return name + "\n状态 正常\n设备运行中";
            }
        }

        // ------------------------------------------------------------------ Image3

        static void BuildLoadChart(Transform root, ChartHoverTooltipView view)
        {
            List<Image> bars = CollectImages(root, "Bar");
            bars.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            List<Text> dates = CollectTexts(root, "XLabel");
            dates.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            Text peakText = FindText(root, "Peak");
            Text capacityText = FindText(root, "CapacityLabel");

            int capacity = capacityText != null ? ParseInt(capacityText.text) : 420;
            int peakHours = peakText != null ? ParseInt(peakText.text) : 478;

            float maxHeight = 1f;
            for (int i = 0; i < bars.Count; i++)
                maxHeight = Mathf.Max(maxHeight, bars[i].rectTransform.rect.height);

            float hoursPerPixel = peakHours / maxHeight;

            for (int i = 0; i < bars.Count; i++)
            {
                int plan = Mathf.RoundToInt(bars[i].rectTransform.rect.height * hoursPerPixel);
                int rate = capacity > 0 ? Mathf.RoundToInt(plan * 100f / capacity) : 0;
                string date = i < dates.Count && dates[i] != null ? dates[i].text : "第 " + (i + 1) + " 天";
                string status = rate >= 100 ? "超负荷" : rate >= 90 ? "接近饱和" : rate >= 70 ? "正常" : "产能充足";

                string content = date + "\n计划工时 " + plan + " h\n可用产能 " + capacity +
                                 " h\n负载率 " + rate + "%\n状态 " + status;
                AddHover(bars[i], view, content);
            }
        }

        // ------------------------------------------------------------------ Image4

        static void BuildRouteChart(Transform root, ChartHoverTooltipView view)
        {
            List<Image> tracks = CollectImages(root, "Track");
            tracks.Sort((a, b) => b.rectTransform.anchoredPosition.y.CompareTo(a.rectTransform.anchoredPosition.y));

            List<Text> routes = CollectTexts(root, "Route");
            routes.Sort((a, b) => b.rectTransform.anchoredPosition.y.CompareTo(a.rectTransform.anchoredPosition.y));

            List<Text> plans = CollectTexts(root, "Plan");
            plans.Sort((a, b) => b.rectTransform.anchoredPosition.y.CompareTo(a.rectTransform.anchoredPosition.y));

            List<Text> percents = CollectTexts(root, "Percent");
            percents.Sort((a, b) => b.rectTransform.anchoredPosition.y.CompareTo(a.rectTransform.anchoredPosition.y));

            for (int i = 0; i < tracks.Count; i++)
            {
                string route = i < routes.Count && routes[i] != null ? routes[i].text : "路线 " + (i + 1);
                int planned = i < plans.Count && plans[i] != null ? ParseInt(plans[i].text) : 0;
                float rate = i < percents.Count && percents[i] != null ? ParseFloat(percents[i].text) : 0f;
                int completed = Mathf.RoundToInt(planned * rate / 100f);
                string status = rate >= 99.5f ? "已完成" : rate >= 85f ? "正常推进" : "滞后";

                string content = route + "\n计划 " + planned + " 单\n已完成 " + completed +
                                 " 单\n完成率 " + rate.ToString("0.0", CultureInfo.InvariantCulture) +
                                 "%\n状态 " + status;
                AddHover(tracks[i], view, content);
            }
        }

        // ------------------------------------------------------------------ Image5

        static void BuildStockChart(Transform root, ChartHoverTooltipView view)
        {
            List<Image> tracks = CollectImages(root, "RingTrack");
            tracks.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            List<Text> values = CollectTexts(root, "RingValue");
            values.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            List<Text> labels = CollectTexts(root, "RingLabel");
            labels.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));

            int total = 0;
            for (int i = 0; i < values.Count; i++)
                if (values[i] != null) total += ParseInt(values[i].text);

            for (int i = 0; i < tracks.Count; i++)
            {
                string label = i < labels.Count && labels[i] != null ? labels[i].text : "库存";
                int value = i < values.Count && values[i] != null ? ParseInt(values[i].text) : 0;
                float share = total > 0 ? value * 100f / total : 0f;

                string content = label + "\n库存 " + value +
                                 "\n占总库存 " + share.ToString("0.0", CultureInfo.InvariantCulture) +
                                 "%\n当前总库存 " + total;
                AddHover(tracks[i], view, content);
            }
        }

        // ------------------------------------------------------------------ 工具方法

        static void AddHover(Graphic target, ChartHoverTooltipView view, string content)
        {
            if (target == null) return;

            target.raycastTarget = true;
            ChartHoverTooltipItem item = target.GetComponent<ChartHoverTooltipItem>();
            if (item == null) item = target.gameObject.AddComponent<ChartHoverTooltipItem>();

            item.view = view;
            item.content = content;
        }

        static List<Image> CollectImages(Transform root, string name)
        {
            var result = new List<Image>();
            Image[] images = root.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image != null && image.gameObject.name == name)
                    result.Add(image);
            }
            return result;
        }

        static List<Text> CollectTexts(Transform root, string name)
        {
            var result = new List<Text>();
            Text[] texts = root.GetComponentsInChildren<Text>(true);

            foreach (Text text in texts)
            {
                if (text != null && text.gameObject.name == name)
                    result.Add(text);
            }
            return result;
        }

        static Text FindText(Transform root, string name)
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text != null && text.gameObject.name == name) return text;
            }
            return null;
        }

        static int ParseInt(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;

            string digits = "";
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i])) digits += value[i];
            }

            int result;
            return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
                ? result : 0;
        }

        static float ParseFloat(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0f;

            string digits = "";
            bool dotFound = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsDigit(c)) digits += c;
                else if (c == '.' && !dotFound) { digits += '.'; dotFound = true; }
            }

            float result;
            return float.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                ? result : 0f;
        }

        static Transform FindChart(string title, string panelName)
        {
            Transform chart = FindDeep(title, null);
            if (chart != null) return chart;

            return FindDeep(panelName, "Canvas right");
        }

        static Transform FindDeep(string name, string ancestor)
        {
            Transform[] all = Object.FindObjectsOfType<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name != name) continue;
                if (ancestor == null) return t;

                Transform parent = t.parent;
                while (parent != null)
                {
                    if (parent.name == ancestor) return t;
                    parent = parent.parent;
                }
            }
            return null;
        }
    }
}
