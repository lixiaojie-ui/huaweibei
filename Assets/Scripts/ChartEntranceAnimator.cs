using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 图表出场动画：
    /// - 柱状图柱子从底部生长
    /// - 进度条从左往右展开
    /// - 圆环从 0 充能
    /// 由 TilePanelSwitcher 在初始页面和切页时调用，只对传入页面下的元素播放。
    /// </summary>
    public class ChartEntranceAnimator : MonoBehaviour
    {
        [Header("单根柱子/进度条的动画时长")]
        public float growDuration = 1.0f;

        [Header("每个元素之间的间隔")]
        public float stagger = 0.09f;

        [Header("开始前的延迟")]
        public float startDelay = 0.2f;

        [Header("圆环动画时长")]
        public float ringDuration = 1.3f;

        [Header("圆环之间的间隔")]
        public float ringStagger = 0.16f;

        [Header("横向条的生长时长/间隔（安环能耗页）")]
        public float flatBarDuration = 0.65f;
        public float flatBarStagger = 0.05f;

        [Header("热力格弹出时长/间隔（安环能耗页）")]
        public float cellDuration = 0.35f;
        public float cellStagger = 0.012f;

        [Header("折线逐段绘制间隔（安环能耗页）")]
        public float segmentStagger = 0.015f;

        // 第一次播放时记录原始尺寸/位置/透明度，之后切页回来仍然使用同一套原始值
        readonly Dictionary<RectTransform, Vector2> barSize = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, Vector2> barPosition = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, float> barAlpha = new Dictionary<RectTransform, float>();
        readonly Dictionary<RectTransform, Vector2> fillSize = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, float> fillAlpha = new Dictionary<RectTransform, float>();
        readonly Dictionary<Image, float> ringFill = new Dictionary<Image, float>();
        readonly Dictionary<Image, float> ringAlpha = new Dictionary<Image, float>();
        readonly Dictionary<Image, float> trackAlpha = new Dictionary<Image, float>();

        // 安环能耗页（Canvas main5）专用元素
        readonly Dictionary<RectTransform, Vector2> flatSize = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, Vector2> flatPos = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, float> flatAlpha = new Dictionary<RectTransform, float>();
        readonly Dictionary<RectTransform, Vector3> cellScale = new Dictionary<RectTransform, Vector3>();
        readonly Dictionary<RectTransform, float> cellAlpha = new Dictionary<RectTransform, float>();
        readonly Dictionary<RectTransform, float> segAlpha = new Dictionary<RectTransform, float>();

        /// <summary>播放指定页面（Canvas mainN）的入场动画。</summary>
        public static void PlayPage(GameObject page)
        {
            if (page == null) return;

            ChartEntranceAnimator host = Object.FindObjectOfType<ChartEntranceAnimator>();
            if (host == null)
            {
                var go = new GameObject("__ChartEntranceAnimator");
                host = go.AddComponent<ChartEntranceAnimator>();
            }

            host.Play(page.transform);
        }

        void Play(Transform page)
        {
            StopAllCoroutines();
            StartCoroutine(PlayRoutine(page));
        }

        /// <summary>延后一帧再开始：main1 右侧图表是运行时生成的，等它们构建完成后再收集元素。</summary>
        IEnumerator PlayRoutine(Transform page)
        {
            yield return null;

            StartCoroutine(GrowBars(page));
            StartCoroutine(SlideFills(page));
            StartCoroutine(GrowRings(page));

            // 数据大屏（main2 / main4 / main5）：环形仪表充能、条形生长、热力格/散点弹出、折线逐段绘制
            int pageIndex = PageIndexOf(page != null ? page.name : null);
            if (pageIndex == 2 || pageIndex == 4 || pageIndex == 5)
            {
                StartCoroutine(ChargeEnergyRings(page));
                StartCoroutine(GrowFlatBars(page));
                StartCoroutine(PopElements(page, "cell", true, 0f));
                StartCoroutine(DrawSegments(page));
            }

            // 主界面（main1）：右侧「产能缺口」堆叠柱逐节生长、产能基准线绘制、数值标签淡入
            if (pageIndex == 1)
            {
                StartCoroutine(GrowFlatBars(page, new[] { "BarSeg" }));
                StartCoroutine(DrawSegments(page, "CapacityLine"));
                StartCoroutine(FadePrefixes(page, new[] { "BarValue", "GapValue", "GapTotal", "GapCap", "CapacityLabel" }, 0.35f));
            }
        }

        /// <summary>从 "Canvas mainN" 取出页序号。</summary>
        static int PageIndexOf(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return -1;

            string s = objectName.Replace(" ", string.Empty);
            if (!s.StartsWith("Canvasmain")) return -1;

            int index = -1;
            return int.TryParse(s.Substring("Canvasmain".Length), out index) ? index : -1;
        }

        /// <summary>柱子从底部向上生长（轴心在左上角，所以同步调整 Y 位置保持底边不动）。</summary>
        IEnumerator GrowBars(Transform root)
        {
            List<RectTransform> bars = Collect(root, "Bar");
            bars.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
            if (bars.Count == 0) yield break;

            int n = bars.Count;
            Vector2[] size = new Vector2[n];
            Vector2[] origin = new Vector2[n];
            float[] alpha = new float[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = bars[i];
                if (rt == null) continue;

                Image image = rt.GetComponent<Image>();

                if (!barSize.ContainsKey(rt))
                {
                    barSize[rt] = rt.sizeDelta;
                    barPosition[rt] = rt.anchoredPosition;
                    barAlpha[rt] = image != null ? image.color.a : 1f;
                }

                size[i] = barSize[rt];
                origin[i] = barPosition[rt];
                alpha[i] = barAlpha[rt];

                rt.sizeDelta = new Vector2(size[i].x, 0f);
                rt.anchoredPosition = new Vector2(origin[i].x, origin[i].y - size[i].y);
                SetAlpha(image, 0f);
            }

            yield return new WaitForSeconds(startDelay);

            float total = growDuration + stagger * (n - 1);
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < n; i++)
                {
                    RectTransform rt = bars[i];
                    if (rt == null) continue;

                    float t = Mathf.Clamp01((elapsed - stagger * i) / growDuration);
                    float e = EaseOutCubic(t);

                    rt.sizeDelta = new Vector2(size[i].x, size[i].y * e);
                    rt.anchoredPosition = new Vector2(origin[i].x, origin[i].y - size[i].y * (1f - e));
                    SetAlpha(rt.GetComponent<Image>(), alpha[i] * e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = bars[i];
                if (rt == null) continue;

                rt.sizeDelta = size[i];
                rt.anchoredPosition = origin[i];
                SetAlpha(rt.GetComponent<Image>(), alpha[i]);
            }
        }

        /// <summary>进度条从左往右展开（左端固定，宽度从 0 长到原宽度）。</summary>
        IEnumerator SlideFills(Transform root)
        {
            List<RectTransform> fills = Collect(root, "Fill");
            fills.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
            if (fills.Count == 0) yield break;

            int n = fills.Count;
            Vector2[] size = new Vector2[n];
            float[] alpha = new float[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = fills[i];
                if (rt == null) continue;

                Image image = rt.GetComponent<Image>();

                if (!fillSize.ContainsKey(rt))
                {
                    fillSize[rt] = rt.sizeDelta;
                    fillAlpha[rt] = image != null ? image.color.a : 1f;
                }

                size[i] = fillSize[rt];
                alpha[i] = fillAlpha[rt];

                rt.sizeDelta = new Vector2(0f, size[i].y);
                SetAlpha(image, 0f);
            }

            yield return new WaitForSeconds(startDelay);

            float total = growDuration + stagger * (n - 1);
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < n; i++)
                {
                    RectTransform rt = fills[i];
                    if (rt == null) continue;

                    float t = Mathf.Clamp01((elapsed - stagger * i) / growDuration);
                    float e = EaseOutCubic(t);

                    rt.sizeDelta = new Vector2(size[i].x * e, size[i].y);
                    SetAlpha(rt.GetComponent<Image>(), alpha[i] * e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = fills[i];
                if (rt == null) continue;

                rt.sizeDelta = size[i];
                SetAlpha(rt.GetComponent<Image>(), alpha[i]);
            }
        }

        /// <summary>
        /// 圆环依次从 0 充能到目标百分比，同时淡入。
        /// 按名字前缀匹配，RingArc1(1) 这类复制件会和原环一起动；按中心位置分组，同一圆环的复制件同步。
        /// </summary>
        IEnumerator GrowRings(Transform root)
        {
            List<RectTransform> arcs = CollectPrefix(root, "RingArc");
            List<RectTransform> tracks = CollectPrefix(root, "RingTrack");
            if (arcs.Count == 0) yield break;

            arcs.Sort((a, b) => CenterX(a).CompareTo(CenterX(b)));

            int n = arcs.Count;
            float[] target = new float[n];
            float[] alpha = new float[n];
            int[] group = new int[n];
            int groupCount = -1;
            float lastCenter = float.MinValue;

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = arcs[i];
                if (rt == null) continue;

                Image arc = rt.GetComponent<Image>();
                if (arc == null) continue;

                float center = CenterX(rt);
                if (i == 0 || Mathf.Abs(center - lastCenter) > 0.5f)
                {
                    groupCount++;
                    lastCenter = center;
                }
                group[i] = groupCount;

                if (!ringFill.ContainsKey(arc))
                {
                    ringFill[arc] = arc.fillAmount;
                    ringAlpha[arc] = arc.color.a;
                }

                target[i] = ringFill[arc];
                alpha[i] = ringAlpha[arc];
                arc.fillAmount = 0f;
                SetAlpha(arc, 0f);
            }

            float[] tracksAlpha = new float[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
            {
                RectTransform rt = tracks[i];
                if (rt == null) continue;

                Image track = rt.GetComponent<Image>();
                if (track == null) continue;

                if (!trackAlpha.ContainsKey(track))
                    trackAlpha[track] = track.color.a;

                tracksAlpha[i] = trackAlpha[track];
                SetAlpha(track, 0f);
            }

            yield return new WaitForSeconds(startDelay);

            int groups = groupCount + 1;
            float total = ringDuration + ringStagger * Mathf.Max(0, groups - 1);
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                float globalE = EaseOutCubic(Mathf.Clamp01(elapsed / ringDuration));

                for (int i = 0; i < n; i++)
                {
                    RectTransform rt = arcs[i];
                    if (rt == null) continue;

                    Image arc = rt.GetComponent<Image>();
                    if (arc == null) continue;

                    float t = Mathf.Clamp01((elapsed - ringStagger * group[i]) / ringDuration);
                    float e = EaseOutCubic(t);

                    arc.fillAmount = target[i] * e;
                    SetAlpha(arc, alpha[i] * e);
                }

                for (int i = 0; i < tracks.Count; i++)
                {
                    RectTransform rt = tracks[i];
                    if (rt == null) continue;

                    Image track = rt.GetComponent<Image>();
                    if (track != null) SetAlpha(track, tracksAlpha[i] * globalE);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = arcs[i];
                if (rt == null) continue;

                Image arc = rt.GetComponent<Image>();
                if (arc != null)
                {
                    arc.fillAmount = target[i];
                    SetAlpha(arc, alpha[i]);
                }
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                RectTransform rt = tracks[i];
                if (rt == null) continue;

                Image track = rt.GetComponent<Image>();
                if (track != null) SetAlpha(track, tracksAlpha[i]);
            }
        }

        /// <summary>环形仪表从 0 充能（gFg 为 Filled/Radial360），底环 gBg 同步淡入。</summary>
        IEnumerator ChargeEnergyRings(Transform root)
        {
            List<RectTransform> arcs = CollectPrefix(root, "ring");
            arcs.AddRange(Collect(root, "gFg"));
            List<RectTransform> tracks = Collect(root, "gBg");

            arcs.RemoveAll(rt => !IsFilledArc(rt != null ? rt.GetComponent<Image>() : null));
            if (arcs.Count == 0 && tracks.Count == 0) yield break;

            arcs.Sort((a, b) => CenterX(a).CompareTo(CenterX(b)));

            float[] target = new float[arcs.Count];
            float[] alpha = new float[arcs.Count];

            for (int i = 0; i < arcs.Count; i++)
            {
                RectTransform rt = arcs[i];
                if (rt == null) continue;

                Image arc = rt.GetComponent<Image>();
                if (arc == null) continue;

                if (!ringFill.ContainsKey(arc))
                {
                    ringFill[arc] = arc.fillAmount;
                    ringAlpha[arc] = arc.color.a;
                }

                target[i] = ringFill[arc];
                alpha[i] = ringAlpha[arc];
                arc.fillAmount = 0f;
                SetAlpha(arc, 0f);
            }

            float[] tracksAlpha = new float[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
            {
                RectTransform rt = tracks[i];
                if (rt == null) continue;

                Image track = rt.GetComponent<Image>();
                if (track == null) continue;

                if (!trackAlpha.ContainsKey(track))
                    trackAlpha[track] = track.color.a;

                tracksAlpha[i] = trackAlpha[track];
                SetAlpha(track, 0f);
            }

            yield return new WaitForSeconds(startDelay);

            float total = ringDuration + ringStagger * Mathf.Max(0, arcs.Count - 1);
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                float globalE = EaseOutCubic(Mathf.Clamp01(elapsed / ringDuration));

                for (int i = 0; i < arcs.Count; i++)
                {
                    RectTransform rt = arcs[i];
                    if (rt == null) continue;

                    Image arc = rt.GetComponent<Image>();
                    if (arc == null) continue;

                    float t = Mathf.Clamp01((elapsed - ringStagger * i) / ringDuration);
                    float e = EaseOutCubic(t);

                    arc.fillAmount = target[i] * e;
                    SetAlpha(arc, alpha[i] * e);
                }

                for (int i = 0; i < tracks.Count; i++)
                {
                    RectTransform rt = tracks[i];
                    if (rt == null) continue;

                    Image track = rt.GetComponent<Image>();
                    if (track != null) SetAlpha(track, tracksAlpha[i] * globalE);
                }

                yield return null;
            }

            for (int i = 0; i < arcs.Count; i++)
            {
                RectTransform rt = arcs[i];
                if (rt == null) continue;

                Image arc = rt.GetComponent<Image>();
                if (arc == null) continue;

                arc.fillAmount = target[i];
                SetAlpha(arc, alpha[i]);
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                RectTransform rt = tracks[i];
                if (rt == null) continue;

                Image track = rt.GetComponent<Image>();
                if (track != null) SetAlpha(track, tracksAlpha[i]);
            }
        }

        /// <summary>条形（bar / fn / rw / sc / kpiBar）沿主轴生长，按从上到下、从左到右依次展开。</summary>
        IEnumerator GrowFlatBars(Transform root)
        {
            return GrowFlatBars(root, FlatBarPrefixes);
        }

        /// <summary>指定前缀的条形沿主轴生长（主界面堆叠柱用 "BarSeg"）。</summary>
        IEnumerator GrowFlatBars(Transform root, string[] prefixes)
        {
            var all = new List<RectTransform>();
            foreach (string prefix in prefixes)
                all.AddRange(CollectPrefix(root, prefix));

            if (all.Count == 0) yield break;

            SortByPosition(all);

            int n = all.Count;
            Vector2[] size = new Vector2[n];
            Vector2[] origin = new Vector2[n];
            float[] alpha = new float[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = all[i];
                if (rt == null) continue;

                Image image = rt.GetComponent<Image>();
                if (image == null) continue;

                if (!flatSize.ContainsKey(rt))
                {
                    flatSize[rt] = rt.sizeDelta;
                    flatPos[rt] = rt.anchoredPosition;
                    flatAlpha[rt] = image.color.a;
                }

                size[i] = flatSize[rt];
                origin[i] = flatPos[rt];
                alpha[i] = flatAlpha[rt];

                rt.sizeDelta = IsVertical(size[i]) ? new Vector2(size[i].x, 0f) : new Vector2(0f, size[i].y);
                SetAlpha(image, 0f);
            }

            yield return new WaitForSeconds(startDelay);

            float total = flatBarDuration + flatBarStagger * Mathf.Max(0, n - 1);
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < n; i++)
                {
                    RectTransform rt = all[i];
                    if (rt == null) continue;

                    float t = Mathf.Clamp01((elapsed - flatBarStagger * i) / flatBarDuration);
                    float e = EaseOutCubic(t);

                    if (IsVertical(size[i]))
                    {
                        rt.sizeDelta = new Vector2(size[i].x, size[i].y * e);
                        rt.anchoredPosition = new Vector2(origin[i].x, origin[i].y - rt.pivot.y * size[i].y * (1f - e));
                    }
                    else
                    {
                        rt.sizeDelta = new Vector2(size[i].x * e, size[i].y);
                        rt.anchoredPosition = new Vector2(origin[i].x - rt.pivot.x * size[i].x * (1f - e), origin[i].y);
                    }

                    SetAlpha(rt.GetComponent<Image>(), alpha[i] * e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = all[i];
                if (rt == null) continue;

                rt.sizeDelta = size[i];
                rt.anchoredPosition = origin[i];
                SetAlpha(rt.GetComponent<Image>(), alpha[i]);
            }
        }

        /// <summary>格子/散点逐个弹出（缩放 + 淡入）。rowMajor=true 按行优先，否则按 X 从左到右。</summary>
        IEnumerator PopElements(Transform root, string prefix, bool rowMajor, float extraDelay)
        {
            List<RectTransform> cells = CollectPrefix(root, prefix);
            if (cells.Count == 0) yield break;

            if (rowMajor) SortByPosition(cells);
            else cells.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));

            int n = cells.Count;
            Vector3[] scale = new Vector3[n];
            float[] alpha = new float[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = cells[i];
                if (rt == null) continue;

                Image image = rt.GetComponent<Image>();

                if (!cellScale.ContainsKey(rt))
                {
                    cellScale[rt] = rt.localScale;
                    cellAlpha[rt] = image != null ? image.color.a : 1f;
                }

                scale[i] = cellScale[rt];
                alpha[i] = cellAlpha[rt];

                rt.localScale = Vector3.zero;
                SetAlpha(image, 0f);
            }

            yield return new WaitForSeconds(startDelay + extraDelay);

            float total = cellDuration + cellStagger * Mathf.Max(0, n - 1);
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < n; i++)
                {
                    RectTransform rt = cells[i];
                    if (rt == null) continue;

                    float t = Mathf.Clamp01((elapsed - cellStagger * i) / cellDuration);
                    float e = EaseOutCubic(t);

                    rt.localScale = new Vector3(scale[i].x * e, scale[i].y * e, scale[i].z);
                    SetAlpha(rt.GetComponent<Image>(), alpha[i] * e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = cells[i];
                if (rt == null) continue;

                rt.localScale = scale[i];
                SetAlpha(rt.GetComponent<Image>(), alpha[i]);
            }
        }

        /// <summary>折线的分段从左往右依次淡入，形成绘制效果。</summary>
        IEnumerator DrawSegments(Transform root, string prefix = "seg")
        {
            List<RectTransform> segs = CollectPrefix(root, prefix);
            if (segs.Count == 0) yield break;

            segs.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));

            int n = segs.Count;
            float[] alpha = new float[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = segs[i];
                if (rt == null) continue;

                Image image = rt.GetComponent<Image>();

                if (!segAlpha.ContainsKey(rt))
                    segAlpha[rt] = image != null ? image.color.a : 1f;

                alpha[i] = segAlpha[rt];
                SetAlpha(image, 0f);
            }

            yield return new WaitForSeconds(startDelay);

            float total = 0.25f + segmentStagger * Mathf.Max(0, n - 1);
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < n; i++)
                {
                    RectTransform rt = segs[i];
                    if (rt == null) continue;

                    float e = EaseOutCubic(Mathf.Clamp01((elapsed - segmentStagger * i) / 0.25f));
                    SetAlpha(rt.GetComponent<Image>(), alpha[i] * e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = segs[i];
                if (rt == null) continue;

                SetAlpha(rt.GetComponent<Image>(), alpha[i]);
            }
        }

        /// <summary>指定前缀的图片/文字依次淡入（只改透明度，不改缩放）。</summary>
        IEnumerator FadePrefixes(Transform root, string[] prefixes, float extraDelay)
        {
            List<RectTransform> list = CollectPrefixGraphics(root, prefixes);
            if (list.Count == 0) yield break;

            SortByPosition(list);

            int n = list.Count;
            float[] alpha = new float[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = list[i];
                if (rt == null) continue;

                Graphic graphic = rt.GetComponent<Graphic>();
                if (graphic == null) continue;

                if (!segAlpha.ContainsKey(rt))
                    segAlpha[rt] = graphic.color.a;

                alpha[i] = segAlpha[rt];
                SetGraphicAlpha(graphic, 0f);
            }

            yield return new WaitForSeconds(startDelay + extraDelay);

            float total = 0.35f + segmentStagger * Mathf.Max(0, n - 1);
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < n; i++)
                {
                    RectTransform rt = list[i];
                    if (rt == null) continue;

                    float e = EaseOutCubic(Mathf.Clamp01((elapsed - segmentStagger * i) / 0.35f));
                    SetGraphicAlpha(rt.GetComponent<Graphic>(), alpha[i] * e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = list[i];
                if (rt == null) continue;

                SetGraphicAlpha(rt.GetComponent<Graphic>(), alpha[i]);
            }
        }

        /// <summary>需要做生长动画的条形元素名前缀。</summary>
        static readonly string[] FlatBarPrefixes = { "bar", "fn", "rw", "sc", "kpiBar", "span" };

        static bool IsVertical(Vector2 size)
        {
            return size.y > size.x;
        }

        static bool IsFilledArc(Image image)
        {
            return image != null && image.type == Image.Type.Filled;
        }

        /// <summary>按显示顺序排序：先上后下，同一行先左后右。</summary>
        static void SortByPosition(List<RectTransform> list)
        {
            list.Sort((a, b) =>
            {
                if (a == null || b == null) return 0;

                float dy = a.anchoredPosition.y - b.anchoredPosition.y;
                if (Mathf.Abs(dy) > 0.5f) return dy > 0f ? -1 : 1;

                return a.anchoredPosition.x.CompareTo(b.anchoredPosition.x);
            });
        }

        static List<RectTransform> CollectPrefix(Transform root, string prefix)
        {
            var result = new List<RectTransform>();
            Image[] images = root.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image != null && image.gameObject.name.StartsWith(prefix))
                    result.Add(image.rectTransform);
            }
            return result;
        }

        static List<RectTransform> CollectPrefixGraphics(Transform root, string[] prefixes)
        {
            var result = new List<RectTransform>();
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);

            foreach (Graphic graphic in graphics)
            {
                if (graphic == null) continue;

                string name = graphic.gameObject.name;
                foreach (string prefix in prefixes)
                {
                    if (name.StartsWith(prefix))
                    {
                        result.Add(graphic.rectTransform);
                        break;
                    }
                }
            }
            return result;
        }

        static float CenterX(RectTransform rect)
        {
            return rect.anchoredPosition.x + rect.rect.center.x;
        }

        static List<RectTransform> Collect(Transform root, string objectName)
        {
            var result = new List<RectTransform>();
            Image[] images = root.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image != null && image.gameObject.name == objectName)
                    result.Add(image.rectTransform);
            }

            return result;
        }

        static void SetAlpha(Image image, float alpha)
        {
            if (image == null) return;

            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;

            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        static float EaseOutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
    }
}
