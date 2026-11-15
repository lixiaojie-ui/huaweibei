using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Main4QC
{
    /// <summary>
    /// main4 专用运行时精灵：程序化生成圆环 / 光点，避免引入外部资源。
    /// </summary>
    public static class Main4Sprites
    {
        static Sprite _ring;
        static Sprite _glow;

        public static Sprite Ring
        {
            get
            {
                if (_ring == null) _ring = Make(256, (x, y) =>
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    float a = Mathf.Clamp01((0.95f - d) * 34f) * Mathf.Clamp01((d - 0.70f) * 34f);
                    return a;
                });
                return _ring;
            }
        }

        public static Sprite Glow
        {
            get
            {
                if (_glow == null) _glow = Make(128, (x, y) =>
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    return Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d);
                });
                return _glow;
            }
        }

        static Sprite Make(int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float ny = (y + 0.5f - half) / half;
                    float a = Mathf.Clamp01(alpha(nx, ny));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    [System.Serializable]
    public class Main4CardView
    {
        public RectTransform root;
        public Image bg;
        public Image accent;
        public TextMeshProUGUI name;
        public TextMeshProUGUI value;
        public TextMeshProUGUI unit;
        public TextMeshProUGUI badge;
        public Image dot;
        public Image band;
        public Image marker;
        public Image connector;
        public Button button;
    }

    [System.Serializable]
    public class Main4NodeView
    {
        public RectTransform root;
        public Image bg;
        public Image glow;
        public Image marker;
        public TextMeshProUGUI label;
        public Button button;
    }

    public class Main4Controller : MonoBehaviour
    {
        // ---------- 主题 ----------
        public static readonly Color Cyan = new Color(0f, 0.898f, 1f);
        public static readonly Color Green = new Color(0.169f, 0.878f, 0.478f);
        public static readonly Color Orange = new Color(1f, 0.635f, 0.227f);
        public static readonly Color Red = new Color(1f, 0.302f, 0.369f);
        public static readonly Color Ink = new Color(0.925f, 0.973f, 1f);
        public static readonly Color Dim = new Color(0.56f, 0.71f, 0.80f);
        public static readonly Color Panel = new Color(0.039f, 0.086f, 0.157f, 0.86f);

        [System.Serializable]
        public class CardData
        {
            public string name;
            public string value;
            public string unit;
            public string status;   // ok / warn / bad
            public int node;        // 关联工位
            public int detail;      // -1 表示无剖视面板
            public string chartTitle;
            public string chartNote;
        }

        [Header("舞台")]
        public RectTransform stage;
        public RectTransform zoomRoot;
        public GameObject stageGlobal;
        public GameObject stageDetail;
        public GameObject[] detailPanels;

        [Header("卡片 / 工位")]
        public Main4CardView[] cards;
        public Main4NodeView[] nodes;

        [Header("图表")]
        public Main4ChartGraphic chart;
        public TextMeshProUGUI chartTitle;
        public TextMeshProUGUI chartNote;
        public Image[] rings;
        public TextMeshProUGUI[] ringValues;
        public Image[] ringTracks;

        [Header("特写")]
        public RectTransform haloRoot;
        public Image[] halos;
        public Image[] particles;
        public Image[] heatCells;
        public RectTransform flopper;

        [Header("信息")]
        public TextMeshProUGUI breadcrumb;
        public TextMeshProUGUI[] logLines;
        public TextMeshProUGUI latencyText;
        public TextMeshProUGUI progressText;
        public Image progressFill;
        public Image[] loadBars;
        public TextMeshProUGUI[] loadTexts;
        public TextMeshProUGUI traceBody;
        public GameObject tracePanel;

        [Header("按钮")]
        public Button[] viewButtons;
        public Button zoomIn;
        public Button zoomOut;
        public Button btnExport;
        public Button btnRescan;
        public Button btnStop;
        public Button btnFullScan;

        // ---------- 数据 ----------
        CardData[] _data;
        int _active = -1;
        bool _busy;
        bool _stopped;
        float _zoom = 1f;
        Vector2 _basePos;
        Vector3 _baseScale = Vector3.one;
        float _chartProgress = 1f;
        int _chartCard = 2;
        float _time;
        float _latencyTimer;
        float _logTick;
        int _logHead;
        readonly List<string> _logBuffer = new List<string>();

        static readonly string[] NodeNames =
        {
            "原料库", "清理筛", "粉碎机", "配料秤", "发酵床",
            "发酵罐", "混合机", "烘干机", "包装线", "码垛"
        };

        void Awake()
        {
            _data = new[]
            {
                new CardData { name = "配料精度", value = "±0.15", unit = "%", status = "warn", node = 3, detail = -1,
                    chartTitle = "配料精度 · 公差带散点", chartNote = "允差 ±0.20%   超差点 2/24" },
                new CardData { name = "混合均匀度", value = "4.2", unit = "%CV", status = "ok", node = 6, detail = -1,
                    chartTitle = "混合均匀度 CV · 7 批", chartNote = "上限 5.0%   均值 4.36%" },
                new CardData { name = "发酵活性", value = "96.8", unit = "%", status = "ok", node = 4, detail = 1,
                    chartTitle = "发酵活性 · 温度 / pH / 溶氧 / CO₂", chartNote = "工艺窗口内 96.8%   峰值 38.5℃" },
                new CardData { name = "成品水分", value = "11.6", unit = "%", status = "warn", node = 7, detail = 2,
                    chartTitle = "成品水分 · 直方图与正态包络", chartNote = "上限 12.5%   下限 10.0%   Cpk 1.18" },
                new CardData { name = "金属/铁杂", value = "0", unit = "件", status = "ok", node = 1, detail = -1,
                    chartTitle = "金属检出 · 工位状态矩阵", chartNote = "8 工位 × 4 时段   红格 1 处" },
                new CardData { name = "包装合规", value = "99.2", unit = "%", status = "ok", node = 8, detail = -1,
                    chartTitle = "包装合规 · 袋重与速度", chartNote = "UCL 25.4kg   LCL 24.6kg   打码正常" },
                new CardData { name = "能耗与工况", value = "86.4", unit = "%OEE", status = "ok", node = 5, detail = 0,
                    chartTitle = "能耗与工况 · OEE 三率", chartNote = "单位电耗 0.42kWh/kg   气耗 0.11m³/kg" }
            };
        }

        void Start()
        {
            _basePos = zoomRoot.anchoredPosition;
            _baseScale = zoomRoot.localScale;
            InitSprites();

            for (int i = 0; i < cards.Length; i++)
            {
                int idx = i;
                if (cards[i].button != null) cards[i].button.onClick.AddListener(() => OnCard(idx));
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                int idx = i;
                if (nodes[i].button != null) nodes[i].button.onClick.AddListener(() => ShowTrace(idx));
            }
            if (zoomIn != null) zoomIn.onClick.AddListener(() => ChangeZoom(0.1f));
            if (zoomOut != null) zoomOut.onClick.AddListener(() => ChangeZoom(-0.1f));
            if (btnFullScan != null) btnFullScan.onClick.AddListener(() => StartCoroutine(FullScan()));
            if (btnRescan != null) btnRescan.onClick.AddListener(() => StartCoroutine(FullScan()));
            if (btnExport != null) btnExport.onClick.AddListener(Export);
            if (btnStop != null) btnStop.onClick.AddListener(ToggleStop);
            for (int i = 0; i < viewButtons.Length; i++)
            {
                int idx = i;
                if (viewButtons[i] != null) viewButtons[i].onClick.AddListener(() => SetView(idx));
            }

            if (tracePanel != null) tracePanel.SetActive(false);

            RefreshCards();
            RefreshRings();
            SetChart(2, 1f);
            PushLog("检测任务已加载 · 批次 B2026-1115-04", 0);
            PushLog("在线质量检测已启动，7 项指标订阅中", 0);
            SetBreadcrumb(-1);
        }

        void InitSprites()
        {
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                rings[i].sprite = Main4Sprites.Ring;
                rings[i].type = Image.Type.Filled;
                rings[i].fillMethod = Image.FillMethod.Radial360;
                rings[i].fillOrigin = (int)Image.Origin360.Top;
                rings[i].preserveAspect = true;
            }
            for (int i = 0; i < halos.Length; i++)
            {
                if (halos[i] == null) continue;
                halos[i].sprite = Main4Sprites.Ring;
                halos[i].preserveAspect = true;
            }
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null) continue;
                particles[i].sprite = Main4Sprites.Glow;
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && nodes[i].glow != null) nodes[i].glow.sprite = Main4Sprites.Glow;
            }
        }

        // ---------------- 交互 ----------------
        void OnCard(int i)
        {
            if (_busy || _stopped) return;
            StartCoroutine(ScanRoutine(i));
        }

        IEnumerator ScanRoutine(int i)
        {
            _busy = true;
            _active = i;
            CardData d = _data[i];

            for (int k = 0; k < cards.Length; k++)
            {
                cards[k].bg.color = k == i ? new Color(0.09f, 0.28f, 0.38f, 0.95f) : new Color(0.04f, 0.11f, 0.18f, 0.78f);
                cards[k].accent.color = k == i ? Cyan : new Color(0f, 0.898f, 1f, 0.22f);
            }

            for (int k = 0; k < nodes.Length; k++)
            {
                float a = k == d.node ? 1f : 0.2f;
                nodes[k].bg.color = new Color(nodes[k].bg.color.r, nodes[k].bg.color.g, nodes[k].bg.color.b, a);
                nodes[k].label.color = new Color(Dim.r, Dim.g, Dim.b, a);
            }

            SetBreadcrumb(i);
            SetDetail(d.detail);

            // 推近
            Vector2 target = Vector2.zero;
            float zoom = d.detail >= 0 ? 1.05f : 1.9f;
            if (d.detail < 0 && d.node >= 0 && d.node < nodes.Length)
            {
                Vector3 local = zoomRoot.InverseTransformPoint(nodes[d.node].root.position);
                target = new Vector2(-local.x * zoom, -local.y * zoom);
            }
            yield return EaseZoom(target, zoom, 0.6f);

            // 雷达光环
            StartCoroutine(HaloRoutine());
            yield return new WaitForSeconds(0.65f);

            // 粒子飞入右侧
            StartCoroutine(ParticleRoutine(5));
            yield return new WaitForSeconds(0.5f);

            SetChart(i, 0f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.7f;
                _chartProgress = Mathf.Clamp01(t);
                chart.Progress = _chartProgress;
                DrawChart();
                yield return null;
            }
            chart.Progress = 1f;

            // 拉回
            yield return new WaitForSeconds(0.35f);
            yield return EaseZoom(_basePos, _zoom, 0.55f);

            for (int k = 0; k < nodes.Length; k++)
            {
                nodes[k].bg.color = new Color(nodes[k].bg.color.r, nodes[k].bg.color.g, nodes[k].bg.color.b, 1f);
                nodes[k].label.color = new Color(Dim.r, Dim.g, Dim.b, 1f);
            }

            Image marker = nodes[d.node].marker;
            if (marker != null)
            {
                marker.gameObject.SetActive(true);
                marker.color = StatusColor(d.status);
            }

            PushLog(d.name + " 扫描完成 · " + d.value + d.unit + " · " + StatusText(d.status), d.status == "bad" ? 2 : (d.status == "warn" ? 1 : 0));
            SetDetail(-1);
            _busy = false;
        }

        IEnumerator EaseZoom(Vector2 pos, float scale, float dur)
        {
            Vector2 p0 = zoomRoot.anchoredPosition;
            float s0 = zoomRoot.localScale.x;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                zoomRoot.anchoredPosition = Vector2.Lerp(p0, pos, e);
                float s = Mathf.Lerp(s0, scale, e);
                zoomRoot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        IEnumerator HaloRoutine()
        {
            if (haloRoot != null) haloRoot.anchoredPosition = Vector2.zero;
            for (int loop = 0; loop < 2; loop++)
            {
                for (int i = 0; i < halos.Length; i++)
                {
                    StartCoroutine(OneHalo(halos[i], i * 0.12f));
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator OneHalo(Image img, float delay)
        {
            if (img == null) yield break;
            yield return new WaitForSeconds(delay);
            img.gameObject.SetActive(true);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.75f;
                float e = Mathf.Clamp01(t);
                img.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.1f, e);
                img.color = new Color(Cyan.r, Cyan.g, Cyan.b, Mathf.Lerp(0.9f, 0f, e));
                yield return null;
            }
            img.gameObject.SetActive(false);
        }

        IEnumerator ParticleRoutine(int count)
        {
            Vector2 start = Vector2.zero;
            RectTransform parent = particles.Length > 0 ? particles[0].rectTransform.parent as RectTransform : null;
            Vector2 end = new Vector2(280f, 40f);
            for (int i = 0; i < count; i++)
            {
                if (i >= particles.Length) break;
                Image p = particles[i];
                p.gameObject.SetActive(true);
                StartCoroutine(FlyOne(p, start, end, i * 0.06f));
            }
            yield return new WaitForSeconds(0.55f);
            for (int i = 0; i < particles.Length; i++) particles[i].gameObject.SetActive(false);
        }

        IEnumerator FlyOne(Image p, Vector2 a, Vector2 b, float delay)
        {
            if (p == null) yield break;
            yield return new WaitForSeconds(delay);
            Vector2 ctrl = new Vector2((a.x + b.x) * 0.5f, Mathf.Max(a.y, b.y) + 120f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.55f;
                float e = Mathf.Clamp01(t);
                Vector2 q1 = Vector2.Lerp(a, ctrl, e);
                Vector2 q2 = Vector2.Lerp(ctrl, b, e);
                p.rectTransform.anchoredPosition = Vector2.Lerp(q1, q2, e);
                p.color = new Color(Cyan.r, Cyan.g, Cyan.b, 1f - e * 0.6f);
                p.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, e);
                yield return null;
            }
        }

        void ChangeZoom(float delta)
        {
            _zoom = Mathf.Clamp(_zoom + delta, 0.6f, 2f);
            zoomRoot.localScale = new Vector3(_zoom, _zoom, 1f);
            PushLog("视图缩放 " + Mathf.RoundToInt(_zoom * 100) + "%", 0);
        }

        void SetView(int idx)
        {
            if (_busy) return;
            Vector2[] offsets = { Vector2.zero, new Vector2(-40f, 0f), new Vector2(0f, 32f), new Vector2(26f, -18f) };
            zoomRoot.anchoredPosition = _basePos + offsets[Mathf.Clamp(idx, 0, 3)];
            for (int i = 0; i < viewButtons.Length; i++)
            {
                Image img = viewButtons[i].targetGraphic as Image;
                if (img != null) img.color = i == idx ? new Color(0f, 0.898f, 1f, 0.85f) : new Color(0.35f, 0.55f, 0.65f, 1f);
            }
            PushLog("视角切换：" + (idx == 0 ? "正视" : idx == 1 ? "侧视" : idx == 2 ? "俯视" : "等轴测"), 0);
        }

        void ToggleStop()
        {
            _stopped = !_stopped;
            PushLog(_stopped ? "紧急停机已触发 · 全部检测暂停" : "紧急停机已解除 · 检测恢复", _stopped ? 2 : 0);
            Image img = btnStop != null ? btnStop.targetGraphic as Image : null;
            if (img != null) img.color = _stopped ? new Color(0.5f, 0.08f, 0.12f, 0.95f) : new Color(0.5f, 0.13f, 0.16f, 0.85f);
        }

        void Export()
        {
            PushLog("检测报告已导出 · B2026-1115-04.pdf", 0);
        }

        IEnumerator FullScan()
        {
            if (_busy || _stopped) yield break;
            PushLog("一键全检开始 · 共 7 项", 0);
            for (int i = 0; i < _data.Length; i++)
            {
                if (_stopped) break;
                yield return ScanRoutine(i);
                yield return new WaitForSeconds(0.25f);
            }
            PushLog("整批结论：合格 5 项 / 预警 2 项 / 缺陷 0 项", 1);
            SetBreadcrumb(-1);
        }

        // ---------------- 图表 ----------------
        void SetChart(int cardIndex, float progress)
        {
            _chartCard = cardIndex;
            CardData d = _data[cardIndex];
            if (chartTitle != null) chartTitle.text = d.chartTitle;
            if (chartNote != null) chartNote.text = d.chartNote;
            _chartProgress = progress;
            if (chart != null) chart.Progress = progress;
            DrawChart();
        }

        void DrawChart()
        {
            if (chart == null) return;
            chart.Clear();
            chart.GridLines(6, 4, new Color(0f, 0.898f, 1f, 0.07f), 1f);
            chart.FrameBox(new Color(0f, 0.898f, 1f, 0.16f), 1f);

            float pr = Mathf.Clamp01(_chartProgress);
            int count = Mathf.Max(1, Mathf.RoundToInt(pr * 8f));

            switch (_chartCard)
            {
                case 0: ChartTolerance(count, pr); break;
                case 1: ChartBarsMean(count, pr); break;
                case 2: ChartMultiAxis(pr); break;
                case 3: ChartHistogram(count, pr); break;
                case 4: ChartMatrix(pr); break;
                case 5: ChartDualAxis(pr); break;
                default: ChartEnergy(pr); break;
            }
        }

        void ChartTolerance(int count, float pr)
        {
            chart.Box(0f, 0.30f, 1f, 0.70f, new Color(Green.r, Green.g, Green.b, 0.10f));
            chart.HDash(0.70f, new Color(Green.r, Green.g, Green.b, 0.75f), 1.2f, 6f, 4f);
            chart.HDash(0.30f, new Color(Green.r, Green.g, Green.b, 0.75f), 1.2f, 6f, 4f);
            chart.HDash(0.5f, new Color(1f, 1f, 1f, 0.18f), 1f, 3f, 5f);

            float[] vals = { 0.52f, 0.62f, 0.44f, 0.68f, 0.36f, 0.58f, 0.47f, 0.72f, 0.33f, 0.55f, 0.61f, 0.42f };
            for (int i = 0; i < vals.Length && i < count * 2; i++)
            {
                float x = 0.06f + i * (0.88f / (vals.Length - 1));
                bool bad = Mathf.Abs(vals[i] - 0.5f) > 0.2f;
                chart.NDot(x, vals[i], 4f, bad ? Red : Cyan, 12);
            }
        }

        void ChartBarsMean(int count, float pr)
        {
            float[] vals = { 4.8f, 4.5f, 4.1f, 4.6f, 4.0f, 4.3f, 3.9f };
            float max = 6f;
            chart.HDash(5f / max, new Color(Red.r, Red.g, Red.b, 0.8f), 1.2f, 6f, 4f);

            float sum = 0f;
            List<Vector2> meanPts = new List<Vector2>();
            for (int i = 0; i < vals.Length && i < count; i++)
            {
                float x0 = 0.07f + i * 0.13f;
                float h = vals[i] / max;
                chart.Box(x0, 0f, x0 + 0.075f, h, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f));
                sum += vals[i];
                meanPts.Add(chart.P(x0 + 0.0375f, (sum / (i + 1)) / max));
            }
            if (meanPts.Count > 1) chart.Polyline(meanPts, 1.6f, Orange);
        }

        void ChartMultiAxis(float pr)
        {
            // 四条归一化曲线 + 工艺窗口色带
            chart.Box(0f, 0.62f, 1f, 0.88f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.07f));
            chart.Box(0f, 0.34f, 1f, 0.52f, new Color(Green.r, Green.g, Green.b, 0.07f));

            Color[] cols = { Cyan, Green, Orange, new Color(0.72f, 0.55f, 1f) };
            float[][] series =
            {
                new[] { 0.42f, 0.48f, 0.55f, 0.62f, 0.70f, 0.74f, 0.76f, 0.74f, 0.71f, 0.68f },
                new[] { 0.66f, 0.64f, 0.62f, 0.60f, 0.58f, 0.57f, 0.56f, 0.56f, 0.55f, 0.55f },
                new[] { 0.52f, 0.56f, 0.60f, 0.58f, 0.55f, 0.52f, 0.50f, 0.49f, 0.50f, 0.52f },
                new[] { 0.30f, 0.36f, 0.44f, 0.52f, 0.60f, 0.66f, 0.70f, 0.72f, 0.70f, 0.66f }
            };

            for (int s = 0; s < series.Length; s++)
            {
                List<Vector2> pts = new List<Vector2>();
                int limit = Mathf.Max(2, Mathf.RoundToInt(pr * series[s].Length));
                for (int i = 0; i < limit; i++)
                {
                    pts.Add(chart.P(0.04f + i * (0.92f / (series[s].Length - 1)), series[s][i]));
                }
                chart.Polyline(pts, 1.8f, cols[s]);
                if (pts.Count > 0) chart.Dot(pts[pts.Count - 1], 3.5f, cols[s], 10);
            }
        }

        void ChartHistogram(int count, float pr)
        {
            float[] bins = { 2f, 5f, 11f, 18f, 24f, 19f, 12f, 6f, 3f };
            float max = 26f;

            chart.NLine(0.55f, 0f, 0.55f, 1f, 1.2f, new Color(Red.r, Red.g, Red.b, 0.7f));
            chart.NLine(0.08f, 0f, 0.08f, 1f, 1.2f, new Color(Red.r, Red.g, Red.b, 0.5f));

            List<Vector2> env = new List<Vector2>();
            for (int i = 0; i < bins.Length; i++)
            {
                float x0 = 0.06f + i * 0.10f;
                float h = bins[i] / max;
                if (i < count + 1) chart.Box(x0, 0f, x0 + 0.08f, h, new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f));
                env.Add(chart.P(x0 + 0.04f, h));
            }
            if (env.Count > 1) chart.Polyline(env, 1.6f, Orange);
        }

        void ChartMatrix(float pr)
        {
            int cols = 8, rows = 4;
            int limit = Mathf.Max(1, Mathf.RoundToInt(pr * cols * rows));
            int k = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    k++;
                    if (k > limit) continue;
                    bool bad = (r == 2 && c == 5);
                    float x0 = 0.04f + c * 0.115f;
                    float y0 = 0.08f + r * 0.22f;
                    chart.Box(x0, y0, x0 + 0.085f, y0 + 0.16f,
                        bad ? new Color(Red.r, Red.g, Red.b, 0.85f) : new Color(Green.r, Green.g, Green.b, 0.35f));
                }
            }
        }

        void ChartDualAxis(float pr)
        {
            List<Vector2> weight = new List<Vector2>();
            List<Vector2> speed = new List<Vector2>();
            float[] w = { 0.52f, 0.56f, 0.58f, 0.54f, 0.60f, 0.57f, 0.62f, 0.59f };
            float[] v = { 0.66f, 0.60f, 0.64f, 0.70f, 0.63f, 0.68f, 0.66f, 0.71f };
            int limit = Mathf.Max(2, Mathf.RoundToInt(pr * w.Length));
            for (int i = 0; i < limit; i++)
            {
                weight.Add(chart.P(0.06f + i * 0.12f, w[i]));
                speed.Add(chart.P(0.06f + i * 0.12f, v[i]));
            }
            chart.Polyline(weight, 1.8f, Cyan);
            chart.Polyline(speed, 1.6f, Orange);
            chart.HDash(0.82f, new Color(Red.r, Red.g, Red.b, 0.65f), 1f, 6f, 4f);
            chart.HDash(0.18f, new Color(Red.r, Red.g, Red.b, 0.65f), 1f, 6f, 4f);
        }

        void ChartEnergy(float pr)
        {
            Color[] cols = { Cyan, Green, Orange };
            float[] vals = { 0.912f, 0.956f, 0.997f };
            for (int i = 0; i < 3; i++)
            {
                float radius = 40f + i * 0f;
                Vector2 c = chart.P(0.17f + i * 0.22f, 0.62f);
                float r = Mathf.Min(chart.rectTransform.rect.width, chart.rectTransform.rect.height) * 0.11f;
                if (r < 20f) r = 20f;
                chart.Arc(c, r, 7f, 0f, 360f, new Color(1f, 1f, 1f, 0.10f), 48);
                chart.Arc(c, r, 7f, 0f, 360f * vals[i] * pr, cols[i], 48);
            }
            chart.Box(0.06f, 0.16f, 0.46f, 0.22f, new Color(1f, 1f, 1f, 0.10f));
            chart.Box(0.06f, 0.16f, 0.06f + 0.40f * 0.72f * pr, 0.22f, Cyan);
            chart.Box(0.54f, 0.16f, 0.94f, 0.22f, new Color(1f, 1f, 1f, 0.10f));
            chart.Box(0.54f, 0.16f, 0.54f + 0.40f * 0.51f * pr, 0.22f, Orange);
        }

        // ---------------- 其它 UI ----------------
        void RefreshCards()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                CardData d = _data[i];
                if (cards[i].name != null) cards[i].name.text = d.name;
                if (cards[i].value != null) cards[i].value.text = d.value;
                if (cards[i].unit != null) cards[i].unit.text = d.unit;
                if (cards[i].dot != null) cards[i].dot.color = StatusColor(d.status);
                if (cards[i].badge != null)
                {
                    cards[i].badge.text = StatusText(d.status);
                    cards[i].badge.color = StatusColor(d.status);
                }
                if (cards[i].marker != null) cards[i].marker.color = StatusColor(d.status);
                if (cards[i].band != null)
                {
                    float w = Mathf.Clamp01(0.25f + (i % 3) * 0.18f);
                    cards[i].band.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                    cards[i].band.rectTransform.anchorMax = new Vector2(0.3f + w, 1f);
                    cards[i].band.color = new Color(StatusColor(d.status).r, StatusColor(d.status).g, StatusColor(d.status).b, 0.25f);
                }
            }
        }

        void RefreshRings()
        {
            float[] vals = { 0.985f, 0.72f, 0.83f };
            for (int i = 0; i < rings.Length && i < vals.Length; i++)
            {
                if (rings[i] != null)
                {
                    rings[i].fillAmount = vals[i];
                    rings[i].color = i == 0 ? Green : (i == 1 ? Orange : Cyan);
                }
                if (ringValues.Length > i && ringValues[i] != null)
                {
                    ringValues[i].text = i == 0 ? "98.5%" : (i == 1 ? "0.02mm" : "1.2s");
                }
            }
        }

        void SetDetail(int idx)
        {
            if (stageGlobal != null) stageGlobal.SetActive(idx < 0);
            if (stageDetail != null) stageDetail.SetActive(idx >= 0);
            for (int i = 0; i < detailPanels.Length; i++)
            {
                if (detailPanels[i] != null) detailPanels[i].SetActive(i == idx);
            }
        }

        void SetBreadcrumb(int cardIdx)
        {
            if (breadcrumb == null) return;
            if (cardIdx < 0)
            {
                breadcrumb.text = "全域视图  ›  10 工位  ›  批次 B2026-1115-04";
                return;
            }
            int n = _data[cardIdx].node;
            string area = n >= 7 ? "烘干区" : (n >= 4 ? "发酵区" : (n >= 2 ? "预处理区" : "原料区"));
            breadcrumb.text = "全域视图  ›  " + area + "  ›  " + NodeNames[Mathf.Clamp(n, 0, NodeNames.Length - 1)];
        }

        void ShowTrace(int idx)
        {
            if (tracePanel == null) return;
            bool open = !tracePanel.activeSelf;
            tracePanel.SetActive(open);
            if (traceBody != null && open)
            {
                traceBody.text =
                    "批次  B2026-1115-04\n" +
                    "配方号  F-0917-A\n" +
                    "原料库位  A-12-03\n" +
                    "设备  " + NodeNames[Mathf.Clamp(idx, 0, NodeNames.Length - 1)] + " · 已运行 6.2h\n" +
                    "操作人  张工 / OP-1027";
            }
        }

        void PushLog(string msg, int level)
        {
            string time = System.DateTime.Now.ToString("HH:mm:ss");
            string color = level == 2 ? "#FF4D5E" : (level == 1 ? "#FFA23A" : "#00E5FF");
            _logBuffer.Insert(0, "<color=" + color + ">[" + time + "]</color> " + msg);
            if (_logBuffer.Count > logLines.Length) _logBuffer.RemoveAt(_logBuffer.Count - 1);
            for (int i = 0; i < logLines.Length; i++)
            {
                if (logLines[i] == null) continue;
                logLines[i].text = i < _logBuffer.Count ? _logBuffer[i] : "";
            }
        }

        Color StatusColor(string s)
        {
            if (s == "bad") return Red;
            if (s == "warn") return Orange;
            return Green;
        }

        string StatusText(string s)
        {
            if (s == "bad") return "缺陷";
            if (s == "warn") return "预警";
            return "合格";
        }

        // ---------------- 帧更新 ----------------
        void Update()
        {
            _time += Time.deltaTime;

            // 呼吸光晕
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].glow == null) continue;
                float phase = _time * 1.6f + i * 0.55f;
                float a = 0.18f + Mathf.Abs(Mathf.Sin(phase)) * 0.30f;
                if (_stopped) a = 0.30f;
                Color c = _stopped ? Red : Cyan;
                nodes[i].glow.color = new Color(c.r, c.g, c.b, a);
            }

            // 特写：翻抛刀与温湿度格、搅拌桨
            if (flopper != null && flopper.gameObject.activeInHierarchy)
            {
                float x = Mathf.Sin(_time * 0.9f) * 0.32f;
                flopper.anchoredPosition = new Vector2(x * flopper.rect.width * 0.5f, flopper.anchoredPosition.y);
            }
            if (heatCells != null)
            {
                for (int i = 0; i < heatCells.Length; i++)
                {
                    if (heatCells[i] == null) continue;
                    float v = Mathf.PerlinNoise(i * 0.37f, _time * 0.15f);
                    Color c = Color.Lerp(Cyan, Orange, v);
                    heatCells[i].color = new Color(c.r, c.g, c.b, 0.35f + v * 0.35f);
                }
            }

            // 连线
            UpdateConnectors();

            // 底部指标
            _latencyTimer -= Time.deltaTime;
            if (_latencyTimer <= 0f)
            {
                _latencyTimer = 1.2f;
                if (latencyText != null) latencyText.text = "延迟 " + Random.Range(12, 28) + "ms";
                for (int i = 0; i < loadBars.Length; i++)
                {
                    if (loadBars[i] == null) continue;
                    float v = Mathf.Clamp01(0.35f + Mathf.PerlinNoise(i * 3.1f, _time * 0.4f) * 0.55f);
                    var lrt = loadBars[i].rectTransform;
                    lrt.sizeDelta = new Vector2(70f * v, lrt.sizeDelta.y);
                    if (loadTexts.Length > i && loadTexts[i] != null) loadTexts[i].text = Mathf.RoundToInt(v * 100) + "%";
                }
            }

            if (progressFill != null)
            {
                var prt = progressFill.rectTransform;
                float target = 200f * (45f / 120f);
                prt.sizeDelta = new Vector2(Mathf.Lerp(prt.sizeDelta.x, target, Time.deltaTime * 2f), prt.sizeDelta.y);
            }
            if (progressText != null) progressText.text = "45 / 120";
        }

        void UpdateConnectors()
        {
            for (int i = 0; i < cards.Length && i < nodes.Length; i++)
            {
                Image line = cards[i].connector;
                if (line == null) continue;
                int n = Mathf.Clamp(_data[i].node, 0, nodes.Length - 1);
                RectTransform from = cards[i].root;
                RectTransform to = nodes[n].root;

                Vector3 a = from.TransformPoint(new Vector3(from.rect.xMax, 0f, 0f));
                Vector3 b = to.position;

                RectTransform parent = line.rectTransform.parent as RectTransform;
                Vector2 la = parent.InverseTransformPoint(a);
                Vector2 lb = parent.InverseTransformPoint(b);

                Vector2 d = lb - la;
                line.rectTransform.anchoredPosition = la + d * 0.5f;
                line.rectTransform.sizeDelta = new Vector2(d.magnitude, 1f);
                line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                bool dim = _active >= 0 && _active != i;
                line.color = new Color(0f, 0.898f, 1f, dim ? 0.12f : 0.42f);
            }
        }
    }
}
