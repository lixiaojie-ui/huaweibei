using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// Play 时给两个渐变图表做入场动画：
    /// - Image3 订单负荷与产能匹配：柱子从底部依次长出
    /// - Image4 AGV 配送任务进度：进度条从左往右依次展开
    /// 不修改场景，运行时自动查找场景里已经烘焙好的图表对象。
    /// </summary>
    public static class ChartEntranceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("__ChartEntranceAnimator");
            go.AddComponent<ChartEntranceAnimator>();
        }
    }

    public class ChartEntranceAnimator : MonoBehaviour
    {
        [Header("单根柱子/进度条的动画时长")]
        public float growDuration = 0.55f;

        [Header("每个元素之间的间隔")]
        public float stagger = 0.055f;

        [Header("开始前的延迟")]
        public float startDelay = 0.15f;

        [Header("圆环动画时长")]
        public float ringDuration = 0.8f;

        [Header("圆环之间的间隔")]
        public float ringStagger = 0.1f;

        IEnumerator Start()
        {
            Transform loadChart = FindChart("订单负荷与产能匹配", "Image3");
            Transform routeChart = FindChart("AGV配送任务进度", "Image4");
            Transform stockChart = FindChart("物料库存趋势", "Image5");

            if (loadChart != null) StartCoroutine(GrowBars(loadChart));
            if (routeChart != null) StartCoroutine(SlideFills(routeChart));
            if (stockChart != null) StartCoroutine(GrowRings(stockChart));

            yield return null;
        }

        /// <summary>柱子从底部向上生长（轴心在左上角，所以同步调整 Y 位置保持底边不动）。</summary>
        IEnumerator GrowBars(Transform root)
        {
            List<RectTransform> bars = Collect(root, "Bar");
            bars.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
            if (bars.Count == 0) yield break;

            int n = bars.Count;
            Vector2[] origin = new Vector2[n];
            Vector2[] size = new Vector2[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = bars[i];
                origin[i] = rt.anchoredPosition;
                size[i] = rt.sizeDelta;

                rt.sizeDelta = new Vector2(size[i].x, 0f);
                rt.anchoredPosition = new Vector2(origin[i].x, origin[i].y - size[i].y);
                SetAlpha(rt.GetComponent<Image>(), 0f);
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
                    SetAlpha(rt.GetComponent<Image>(), e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = bars[i];
                if (rt == null) continue;

                rt.sizeDelta = size[i];
                rt.anchoredPosition = origin[i];
                SetAlpha(rt.GetComponent<Image>(), 1f);
            }
        }

        /// <summary>进度条从左往右展开（左端固定，宽度从 0 长到原宽度）。</summary>
        IEnumerator SlideFills(Transform root)
        {
            List<RectTransform> fills = Collect(root, "Fill");
            fills.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
            if (fills.Count == 0) yield break;

            int n = fills.Count;
            Vector2[] origin = new Vector2[n];
            Vector2[] size = new Vector2[n];

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = fills[i];
                origin[i] = rt.anchoredPosition;
                size[i] = rt.sizeDelta;

                rt.sizeDelta = new Vector2(0f, size[i].y);
                SetAlpha(rt.GetComponent<Image>(), 0f);
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
                    SetAlpha(rt.GetComponent<Image>(), e);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                RectTransform rt = fills[i];
                if (rt == null) continue;

                rt.sizeDelta = size[i];
                SetAlpha(rt.GetComponent<Image>(), 1f);
            }
        }

        /// <summary>三个圆环依次从 0 充能到目标百分比，同时淡入。</summary>
        IEnumerator GrowRings(Transform root)
        {
            List<RectTransform> arcs = Collect(root, "RingArc");
            List<RectTransform> tracks = Collect(root, "RingTrack");
            arcs.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
            tracks.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
            if (arcs.Count == 0) yield break;

            int n = arcs.Count;
            float[] target = new float[n];
            float[] arcAlpha = new float[n];
            float[] trackAlpha = new float[tracks.Count];

            for (int i = 0; i < n; i++)
            {
                Image arc = arcs[i].GetComponent<Image>();
                if (arc == null) continue;

                target[i] = arc.fillAmount;
                arcAlpha[i] = arc.color.a;
                arc.fillAmount = 0f;
                SetAlpha(arc, 0f);
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                Image track = tracks[i].GetComponent<Image>();
                if (track == null) continue;

                trackAlpha[i] = track.color.a;
                SetAlpha(track, 0f);
            }

            yield return new WaitForSeconds(startDelay);

            float total = ringDuration + ringStagger * (n - 1);
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < n; i++)
                {
                    float t = Mathf.Clamp01((elapsed - ringStagger * i) / ringDuration);
                    float e = EaseOutCubic(t);

                    Image arc = arcs[i].GetComponent<Image>();
                    if (arc != null)
                    {
                        arc.fillAmount = target[i] * e;
                        SetAlpha(arc, arcAlpha[i] * e);
                    }

                    if (i < tracks.Count)
                    {
                        Image track = tracks[i].GetComponent<Image>();
                        if (track != null) SetAlpha(track, trackAlpha[i] * e);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                Image arc = arcs[i].GetComponent<Image>();
                if (arc != null)
                {
                    arc.fillAmount = target[i];
                    SetAlpha(arc, arcAlpha[i]);
                }

                if (i < tracks.Count)
                {
                    Image track = tracks[i].GetComponent<Image>();
                    if (track != null) SetAlpha(track, trackAlpha[i]);
                }
            }
        }

        static List<RectTransform> Collect(Transform root, string objectName)
        {
            var result = new List<RectTransform>();
            Image[] images = root.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image.gameObject.name == objectName)
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

        static Transform FindChart(string title, string panelName)
        {
            Transform chart = FindDeep(title);
            if (chart != null) return chart;

            // 找不到标题节点时退回到 Image3 / Image4 面板，仍然能按子物体名字找到 Bar / Fill
            return FindDeep(panelName);
        }

        static Transform FindDeep(string name)
        {
            Transform[] all = Object.FindObjectsOfType<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == name) return t;
            }
            return null;
        }
    }
}
