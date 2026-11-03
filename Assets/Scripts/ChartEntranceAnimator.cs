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

        // 第一次播放时记录原始尺寸/位置/透明度，之后切页回来仍然使用同一套原始值
        readonly Dictionary<RectTransform, Vector2> barSize = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, Vector2> barPosition = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, float> barAlpha = new Dictionary<RectTransform, float>();
        readonly Dictionary<RectTransform, Vector2> fillSize = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, float> fillAlpha = new Dictionary<RectTransform, float>();
        readonly Dictionary<Image, float> ringFill = new Dictionary<Image, float>();
        readonly Dictionary<Image, float> ringAlpha = new Dictionary<Image, float>();
        readonly Dictionary<Image, float> trackAlpha = new Dictionary<Image, float>();

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
            StartCoroutine(GrowBars(page));
            StartCoroutine(SlideFills(page));
            StartCoroutine(GrowRings(page));
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

        static float EaseOutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
    }
}
