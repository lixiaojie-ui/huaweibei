using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    [Serializable]
    public class Main5DeviceInfo
    {
        public string name;
        public float now;
        public float prev;
    }

    [Serializable]
    public class Main5PageData
    {
        public Main5DeviceInfo[] devices = new Main5DeviceInfo[0];
    }

    [Serializable]
    public class Main5EnergyData
    {
        public string[] days = new string[0];
        public float[] values = new float[0];
    }

    /// <summary>
    /// Canvas main5 安环能耗的运行时控制器：
    /// 入场动画、导航悬停/选中、数据表切换、图表 tooltip 与故障图标呼吸。
    /// 编辑态不修改任何内容，构建器写出的就是页面最终静态样子。
    /// </summary>
    [AddComponentMenu("Main5/Dashboard")]
    public class Main5Dashboard : MonoBehaviour
    {
        [Header("页面")]
        public GameObject[] pageRoots = new GameObject[0];
        public CanvasGroup[] pageContents = new CanvasGroup[0];
        public CanvasGroup[] statsGroups = new CanvasGroup[0];

        [Header("右侧导航")]
        public UIRoundRect[] navBg = new UIRoundRect[0];
        public Image[] navAccent = new Image[0];
        public Text[] navIdx = new Text[0];
        public Text[] navLabels = new Text[0];
        public RectTransform[] navRects = new RectTransform[0];
        public CanvasGroup[] navGroups = new CanvasGroup[0];

        [Header("数据表按钮")]
        public UIRoundRect[] tableBtns = new UIRoundRect[0];
        public Text[] tableBtnLabels = new Text[0];
        public GameObject[] tableViews = new GameObject[0];

        [Header("提示框")]
        public Main5Tooltip tooltipLeft;
        public Main5Tooltip tooltipRight;
        public RectTransform leftCanvasRect;
        public RectTransform rightCanvasRect;

        [Header("左侧 / 顶栏 / 页脚")]
        public CanvasGroup[] leftContents = new CanvasGroup[0];
        public RectTransform[] leftContentRects = new RectTransform[0];
        public RectTransform[] bulletFills = new RectTransform[0];
        public RectTransform energyMask;
        public RectTransform energyPointer;
        public CanvasGroup headerGroup;
        public CanvasGroup footerGroup;
        public RectTransform headerRect;
        public RectTransform footerRect;

        [Header("七日用电折线定位（图表内坐标）")]
        public float energyPlotLeft = 42f;
        public float energyPlotTop = 26f;
        public float energyPlotWidth = 468f;
        public float energyPlotHeight = 126.5f;

        [Header("数据")]
        public Main5PageData[] pageData = new Main5PageData[0];
        public Main5EnergyData energy = new Main5EnergyData();

        public RectTransform energyZone;

        public int activeIndex = 0;

        sealed class PageAnim
        {
            public RectTransform statsRect;
            public List<RectTransform> bars = new List<RectTransform>();
            public List<Text> barLabels = new List<Text>();
            public List<RectTransform> stems = new List<RectTransform>();
            public List<RectTransform> dots = new List<RectTransform>();
            public List<Text> changeLabels = new List<Text>();
            public RectTransform mixMask;
            public RectMask2D mixMaskComp;
            public UIRoundRect barHighlight;
            public RectTransform barZone;
            public RectTransform mixZone;
            public RectTransform changeZone;
        }

        PageAnim[] anims;
        RectTransform[] statsBase;
        Vector2[] navBase;
        Vector2[] leftBase;
        Vector2 headerBase;
        Vector2 footerBase;

        readonly Dictionary<RectTransform, Vector2> finalSize = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, Vector3> finalScale = new Dictionary<RectTransform, Vector3>();
        readonly Dictionary<Graphic, float> finalAlpha = new Dictionary<Graphic, float>();

        Color[] navFill;
        Color[] navFill2;
        Color[] navBorder;
        Color[] navLabelColor;
        Color[] navIdxColor;
        Color[] navAccentColor;
        float[] navShift;

        Color[] btnFill;
        Color[] btnBorder;
        Color[] btnTextColor;

        readonly List<CanvasGroup> faultIcons = new List<CanvasGroup>();

        int hoverZone = -1;
        bool tableOpen;
        bool initialized;

        static readonly Color TRANSPARENT = new Color(0f, 0f, 0f, 0f);
        static readonly Color ACCENT = new Color(0.310f, 0.788f, 0.941f, 1f);      // #4fc9f0
        static readonly Color GREEN = new Color(0.435f, 0.890f, 0.608f, 1f);       // #6fe3ae
        static readonly Color TEXT_MAIN = new Color(0.918f, 0.965f, 1f, 1f);       // #eaf6ff
        static readonly Color TEXT_NAV = new Color(0.745f, 0.851f, 0.910f, 1f);    // #bed9e8
        static readonly Color TEXT_MUTED = new Color(0.663f, 0.749f, 0.816f, 1f);  // #a9c9db
        static readonly Color WARN_TEXT = new Color(1f, 0.812f, 0.525f, 1f);       // #ffcf86
        static readonly Color GOOD_TEXT = new Color(0.435f, 0.890f, 0.608f, 1f);   // #6fe3ae

        void Awake()
        {
            BuildRuntimeCache();

            // 预制体实例/场景里的交互区在运行时统一挂到本控制器上
            Main5PointerRelay[] relays = FindObjectsOfType<Main5PointerRelay>(true);
            for (int i = 0; i < relays.Length; i++)
                if (relays[i] != null) relays[i].dashboard = this;
        }

        void OnEnable()
        {
            if (!initialized) return;

            StopAllCoroutines();
            HideAllHover();

            if (pageRoots.Length > 0)
            {
                activeIndex = Mathf.Clamp(activeIndex, 0, pageRoots.Length - 1);
                for (int i = 0; i < pageRoots.Length; i++)
                    if (pageRoots[i] != null) pageRoots[i].SetActive(i == activeIndex);
            }

            SnapNav();
            ApplyTableState();
            StartCoroutine(IntroRoutine());
        }

        void BuildRuntimeCache()
        {
            int n = pageRoots.Length;
            anims = new PageAnim[n];
            statsBase = new RectTransform[n];
            navBase = new Vector2[n];
            navFill = new Color[n];
            navFill2 = new Color[n];
            navBorder = new Color[n];
            navLabelColor = new Color[n];
            navIdxColor = new Color[n];
            navAccentColor = new Color[n];
            navShift = new float[n];
            btnFill = new Color[n];
            btnBorder = new Color[n];
            btnTextColor = new Color[n];

            for (int i = 0; i < n; i++)
            {
                PageAnim a = new PageAnim();
                GameObject root = pageRoots[i];
                if (root != null)
                {
                    Transform[] all = root.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in all)
                    {
                        string nm = t.name;
                        if (nm.StartsWith("mBarNow") || nm.StartsWith("mBarPrev"))
                        {
                            RectTransform rt = t as RectTransform;
                            if (rt != null) a.bars.Add(rt);
                        }
                        else if (nm.StartsWith("mBarLabel"))
                        {
                            Text tx = t.GetComponent<Text>();
                            if (tx != null) a.barLabels.Add(tx);
                        }
                        else if (nm.StartsWith("mStem"))
                        {
                            RectTransform rt = t as RectTransform;
                            if (rt != null) a.stems.Add(rt);
                        }
                        else if (nm.StartsWith("mDot"))
                        {
                            RectTransform rt = t as RectTransform;
                            if (rt != null) a.dots.Add(rt);
                        }
                        else if (nm.StartsWith("mChangeLabel"))
                        {
                            Text tx = t.GetComponent<Text>();
                            if (tx != null) a.changeLabels.Add(tx);
                        }
                        else if (nm == "mMixMask")
                        {
                            a.mixMask = t as RectTransform;
                            a.mixMaskComp = t.GetComponent<RectMask2D>();
                        }
                        else if (nm == "mBarHighlight")
                        {
                            a.barHighlight = t.GetComponent<UIRoundRect>();
                        }
                        else if (nm == "mBarZone")
                        {
                            a.barZone = t as RectTransform;
                        }
                        else if (nm == "mMixZone")
                        {
                            a.mixZone = t as RectTransform;
                        }
                        else if (nm == "mChangeZone")
                        {
                            a.changeZone = t as RectTransform;
                        }
                        else if (nm == "mStats")
                        {
                            a.statsRect = t as RectTransform;
                        }
                        else if (nm == "mFaultIcon")
                        {
                            CanvasGroup cg = t.GetComponent<CanvasGroup>();
                            if (cg != null) faultIcons.Add(cg);
                        }
                    }
                }
                anims[i] = a;

                if (a.statsRect != null) statsBase[i] = a.statsRect;
                if (i < navRects.Length && navRects[i] != null) navBase[i] = navRects[i].anchoredPosition;

                CollectFinals(a);
            }

            // stats 的初始位置
            if (statsPos == null) statsPos = new Vector2[n];
            for (int i = 0; i < n; i++)
                if (anims[i].statsRect != null) statsPos[i] = anims[i].statsRect.anchoredPosition;

            if (leftContentRects != null)
            {
                leftBase = new Vector2[leftContentRects.Length];
                for (int i = 0; i < leftContentRects.Length; i++)
                    if (leftContentRects[i] != null) leftBase[i] = leftContentRects[i].anchoredPosition;
            }

            if (headerRect != null) headerBase = headerRect.anchoredPosition;
            if (footerRect != null) footerBase = footerRect.anchoredPosition;

            for (int i = 0; i < navFill.Length; i++)
            {
                navFill[i] = TRANSPARENT;
                navFill2[i] = TRANSPARENT;
                navBorder[i] = RGBA(126, 205, 245, 0.20f);
                navLabelColor[i] = TEXT_NAV;
                navIdxColor[i] = RGBA(79, 201, 240, 0.68f);
                navAccentColor[i] = RGBA(79, 201, 240, 0.32f);
            }

            for (int i = 0; i < n; i++)
            {
                btnFill[i] = TRANSPARENT;
                btnBorder[i] = RGBA(79, 201, 240, 0.28f);
                btnTextColor[i] = new Color(0.498f, 0.847f, 1f, 1f); // #7fd8ff
            }

            initialized = true;
        }

        Vector2[] statsPos;

        void CollectFinals(PageAnim a)
        {
            for (int i = 0; i < a.bars.Count; i++) RememberSize(a.bars[i]);
            for (int i = 0; i < a.stems.Count; i++) RememberSize(a.stems[i]);
            for (int i = 0; i < a.dots.Count; i++) RememberScale(a.dots[i]);
            for (int i = 0; i < a.barLabels.Count; i++) RememberAlpha(a.barLabels[i]);
            for (int i = 0; i < a.changeLabels.Count; i++) RememberAlpha(a.changeLabels[i]);
            if (a.mixMask != null) RememberSize(a.mixMask);
            if (energyMask != null) RememberSize(energyMask);
            if (bulletFills != null)
                for (int i = 0; i < bulletFills.Length; i++) RememberSize(bulletFills[i]);
        }

        void RememberSize(RectTransform rt)
        {
            if (rt != null && !finalSize.ContainsKey(rt)) finalSize[rt] = rt.sizeDelta;
        }

        void RememberScale(RectTransform rt)
        {
            if (rt != null && !finalScale.ContainsKey(rt)) finalScale[rt] = rt.localScale;
        }

        void RememberAlpha(Graphic g)
        {
            if (g != null && !finalAlpha.ContainsKey(g)) finalAlpha[g] = g.color.a;
        }

        // ============================================================ 入场

        IEnumerator IntroRoutine()
        {
            HideAllHover();

            if (headerGroup != null) headerGroup.alpha = 0f;
            if (footerGroup != null) footerGroup.alpha = 0f;
            if (headerRect != null) headerRect.anchoredPosition = headerBase + new Vector2(0f, -8f);
            if (footerRect != null) footerRect.anchoredPosition = footerBase + new Vector2(0f, 6f);

            for (int i = 0; i < leftContents.Length; i++)
            {
                if (leftContents[i] != null) leftContents[i].alpha = 0f;
                if (leftContentRects != null && i < leftContentRects.Length && leftContentRects[i] != null)
                    leftContentRects[i].anchoredPosition = leftBase[i] + new Vector2(-18f, 0f);
            }

            for (int i = 0; i < navGroups.Length; i++)
            {
                if (navGroups[i] != null) navGroups[i].alpha = 0f;
                if (navRects[i] != null) navRects[i].anchoredPosition = navBase[i] + new Vector2(14f, 0f);
            }

            if (activeIndex < pageContents.Length && pageContents[activeIndex] != null)
                pageContents[activeIndex].alpha = 0f;

            yield return null;

            float dur = 0.55f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);

                if (headerGroup != null) headerGroup.alpha = EaseOut(Mathf.Clamp01(t / 0.7f));
                if (footerGroup != null) footerGroup.alpha = EaseOut(Mathf.Clamp01((t - 0.15f) / 0.7f));
                if (headerRect != null) headerRect.anchoredPosition = headerBase + new Vector2(0f, -8f * (1f - EaseOut(Mathf.Clamp01(t / 0.7f))));
                if (footerRect != null) footerRect.anchoredPosition = footerBase + new Vector2(0f, 6f * (1f - EaseOut(Mathf.Clamp01((t - 0.15f) / 0.7f))));

                for (int i = 0; i < leftContents.Length; i++)
                {
                    float ti = Mathf.Clamp01((elapsed - 0.10f * i) / 0.5f);
                    float e = EaseOut(ti);
                    if (leftContents[i] != null) leftContents[i].alpha = e;
                    if (leftContentRects != null && i < leftContentRects.Length && leftContentRects[i] != null)
                        leftContentRects[i].anchoredPosition = leftBase[i] + new Vector2(-18f * (1f - e), 0f);
                }

                for (int i = 0; i < navGroups.Length; i++)
                {
                    float ti = Mathf.Clamp01((elapsed - 0.12f - 0.045f * i) / 0.36f);
                    float e = EaseOut(ti);
                    if (navGroups[i] != null) navGroups[i].alpha = e;
                    if (navRects[i] != null) navRects[i].anchoredPosition = navBase[i] + new Vector2(14f * (1f - e), 0f);
                }

                if (activeIndex < pageContents.Length && pageContents[activeIndex] != null)
                    pageContents[activeIndex].alpha = EaseOut(Mathf.Clamp01((elapsed - 0.06f) / 0.5f));

                yield return null;
            }

            if (headerGroup != null) headerGroup.alpha = 1f;
            if (footerGroup != null) footerGroup.alpha = 1f;
            if (headerRect != null) headerRect.anchoredPosition = headerBase;
            if (footerRect != null) footerRect.anchoredPosition = footerBase;

            for (int i = 0; i < leftContents.Length; i++)
            {
                if (leftContents[i] != null) leftContents[i].alpha = 1f;
                if (leftContentRects != null && i < leftContentRects.Length && leftContentRects[i] != null)
                    leftContentRects[i].anchoredPosition = leftBase[i];
            }
            for (int i = 0; i < navGroups.Length; i++)
            {
                if (navGroups[i] != null) navGroups[i].alpha = 1f;
                if (navRects[i] != null) navRects[i].anchoredPosition = navBase[i];
            }
            if (activeIndex < pageContents.Length && pageContents[activeIndex] != null)
                pageContents[activeIndex].alpha = 1f;

            StartCoroutine(AnimateBullets());
            StartCoroutine(AnimateEnergy());
            yield return StartCoroutine(PlayPageAnimations(activeIndex));
        }

        IEnumerator AnimateBullets()
        {
            if (bulletFills == null || bulletFills.Length == 0) yield break;

            float dur = 0.65f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                for (int i = 0; i < bulletFills.Length; i++)
                {
                    RectTransform rt = bulletFills[i];
                    if (rt == null || !finalSize.ContainsKey(rt)) continue;
                    float e = EaseOut(Mathf.Clamp01((t - i * 0.08f) / 0.7f));
                    Vector2 f = finalSize[rt];
                    rt.sizeDelta = new Vector2(f.x * e, f.y);
                }
                yield return null;
            }
            for (int i = 0; i < bulletFills.Length; i++)
            {
                RectTransform rt = bulletFills[i];
                if (rt != null && finalSize.ContainsKey(rt)) rt.sizeDelta = finalSize[rt];
            }
        }

        IEnumerator AnimateEnergy()
        {
            if (energyMask == null || !finalSize.ContainsKey(energyMask)) yield break;

            RectMask2D mask = energyMask.GetComponent<RectMask2D>();
            if (mask != null) mask.enabled = true;

            Vector2 full = finalSize[energyMask];
            float dur = 0.85f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float e = EaseOut(Mathf.Clamp01(elapsed / dur));
                energyMask.sizeDelta = new Vector2(full.x * e, full.y);
                yield return null;
            }
            energyMask.sizeDelta = full;
            if (mask != null) mask.enabled = false;
        }

        // ============================================================ 页面动画

        IEnumerator PlayPageAnimations(int index)
        {
            if (index < 0 || index >= anims.Length) yield break;
            PageAnim a = anims[index];
            if (a == null) yield break;

            StartCoroutine(AnimateStats(index));
            yield return null;

            float dur = 0.42f;
            float elapsed = 0f;

            if (a.mixMaskComp != null) a.mixMaskComp.enabled = true;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float e = EaseOut(Mathf.Clamp01(elapsed / dur));

                for (int i = 0; i < a.bars.Count; i++)
                {
                    RectTransform rt = a.bars[i];
                    if (rt == null || !finalSize.ContainsKey(rt)) continue;
                    Vector2 f = finalSize[rt];
                    rt.sizeDelta = new Vector2(f.x, f.y * e);
                }

                for (int i = 0; i < a.stems.Count; i++)
                {
                    RectTransform rt = a.stems[i];
                    if (rt == null || !finalSize.ContainsKey(rt)) continue;
                    Vector2 f = finalSize[rt];
                    rt.sizeDelta = new Vector2(f.x * e, f.y);
                }

                if (a.mixMask != null && finalSize.ContainsKey(a.mixMask))
                {
                    Vector2 f = finalSize[a.mixMask];
                    a.mixMask.sizeDelta = new Vector2(f.x * e, f.y);
                }

                for (int i = 0; i < a.dots.Count; i++)
                {
                    RectTransform rt = a.dots[i];
                    if (rt == null || !finalScale.ContainsKey(rt)) continue;
                    float ti = Mathf.Clamp01((elapsed - 0.03f * i) / 0.30f);
                    float ei = EaseOut(ti);
                    Vector3 s = finalScale[rt];
                    rt.localScale = new Vector3(s.x * ei, s.y * ei, s.z);
                }

                for (int i = 0; i < a.barLabels.Count; i++)
                {
                    Text tx = a.barLabels[i];
                    if (tx == null || !finalAlpha.ContainsKey(tx)) continue;
                    float ti = Mathf.Clamp01((elapsed - 0.18f) / 0.24f);
                    SetAlpha(tx, finalAlpha[tx] * ti);
                }

                for (int i = 0; i < a.changeLabels.Count; i++)
                {
                    Text tx = a.changeLabels[i];
                    if (tx == null || !finalAlpha.ContainsKey(tx)) continue;
                    float ti = Mathf.Clamp01((elapsed - 0.12f - 0.02f * i) / 0.26f);
                    SetAlpha(tx, finalAlpha[tx] * ti);
                }

                yield return null;
            }

            for (int i = 0; i < a.bars.Count; i++)
                if (a.bars[i] != null && finalSize.ContainsKey(a.bars[i])) a.bars[i].sizeDelta = finalSize[a.bars[i]];
            for (int i = 0; i < a.stems.Count; i++)
                if (a.stems[i] != null && finalSize.ContainsKey(a.stems[i])) a.stems[i].sizeDelta = finalSize[a.stems[i]];
            if (a.mixMask != null && finalSize.ContainsKey(a.mixMask)) a.mixMask.sizeDelta = finalSize[a.mixMask];
            if (a.mixMaskComp != null) a.mixMaskComp.enabled = false;
            for (int i = 0; i < a.dots.Count; i++)
                if (a.dots[i] != null && finalScale.ContainsKey(a.dots[i])) a.dots[i].localScale = finalScale[a.dots[i]];
            for (int i = 0; i < a.barLabels.Count; i++)
                if (a.barLabels[i] != null && finalAlpha.ContainsKey(a.barLabels[i])) SetAlpha(a.barLabels[i], finalAlpha[a.barLabels[i]]);
            for (int i = 0; i < a.changeLabels.Count; i++)
                if (a.changeLabels[i] != null && finalAlpha.ContainsKey(a.changeLabels[i])) SetAlpha(a.changeLabels[i], finalAlpha[a.changeLabels[i]]);
        }

        IEnumerator AnimateStats(int index)
        {
            if (index >= anims.Length) yield break;
            RectTransform rt = anims[index].statsRect;
            CanvasGroup cg = index < statsGroups.Length ? statsGroups[index] : null;
            if (rt == null && cg == null) yield break;

            Vector2 basePos = statsPos != null && index < statsPos.Length ? statsPos[index] : (rt != null ? rt.anchoredPosition : Vector2.zero);

            float dur = 0.26f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float e = EaseOut(Mathf.Clamp01(elapsed / dur));
                if (cg != null) cg.alpha = Mathf.Lerp(0.3f, 1f, e);
                if (rt != null) rt.anchoredPosition = basePos + new Vector2(0f, -5f * (1f - e));
                yield return null;
            }
            if (cg != null) cg.alpha = 1f;
            if (rt != null) rt.anchoredPosition = basePos;
        }

        // ============================================================ 每帧

        void Update()
        {
            if (!initialized) return;

            float dt = Time.unscaledDeltaTime;
            AnimateNav(dt);
            AnimateTableButtons(dt);

            float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.8f));
            for (int i = 0; i < faultIcons.Count; i++)
                if (faultIcons[i] != null) faultIcons[i].alpha = pulse;
        }

        void AnimateNav(float dt)
        {
            float k = 1f - Mathf.Exp(-dt / 0.10f);

            for (int i = 0; i < navBg.Length; i++)
            {
                bool active = i == activeIndex;
                bool hover = hoverZone == 100 + i;

                Color fill, fill2, border, label, idx, accent;
                float targetShift;

                if (active)
                {
                    fill = RGBA(40, 140, 200, 0.32f);
                    fill2 = RGBA(20, 60, 90, 0.10f);
                    border = RGBA(79, 201, 240, 0.60f);
                    label = Color.white;
                    idx = ACCENT;
                    accent = ACCENT;
                    targetShift = hover ? 2f : 0f;
                }
                else if (hover)
                {
                    fill = RGBA(79, 201, 240, 0.08f);
                    fill2 = fill;
                    border = RGBA(79, 201, 240, 0.52f);
                    label = Color.white;
                    idx = RGBA(79, 201, 240, 0.68f);
                    accent = RGBA(79, 201, 240, 0.32f);
                    targetShift = 2f;
                }
                else
                {
                    fill = TRANSPARENT;
                    fill2 = TRANSPARENT;
                    border = RGBA(126, 205, 245, 0.20f);
                    label = TEXT_NAV;
                    idx = RGBA(79, 201, 240, 0.68f);
                    accent = RGBA(79, 201, 240, 0.32f);
                    targetShift = 0f;
                }

                navFill[i] = Color.Lerp(navFill[i], fill, k);
                navFill2[i] = Color.Lerp(navFill2[i], fill2, k);
                navBorder[i] = Color.Lerp(navBorder[i], border, k);
                navLabelColor[i] = Color.Lerp(navLabelColor[i], label, k);
                navIdxColor[i] = Color.Lerp(navIdxColor[i], idx, k);
                navAccentColor[i] = Color.Lerp(navAccentColor[i], accent, k);
                navShift[i] = Mathf.Lerp(navShift[i], targetShift, k);

                if (navBg[i] != null)
                {
                    navBg[i].color = navFill[i];
                    navBg[i].color2 = navFill2[i];
                    navBg[i].gradient = true;
                    navBg[i].borderColor = navBorder[i];
                    navBg[i].Refresh();
                }
                if (navAccent != null && i < navAccent.Length && navAccent[i] != null)
                    navAccent[i].color = navAccentColor[i];
                if (navIdx != null && i < navIdx.Length && navIdx[i] != null)
                    navIdx[i].color = navIdxColor[i];
                if (navLabels != null && i < navLabels.Length && navLabels[i] != null)
                    navLabels[i].color = navLabelColor[i];
                if (navRects != null && i < navRects.Length && navRects[i] != null)
                    navRects[i].anchoredPosition = navBase[i] + new Vector2(navShift[i], 0f);
            }
        }

        void AnimateTableButtons(float dt)
        {
            float k = 1f - Mathf.Exp(-dt / 0.09f);

            for (int i = 0; i < tableBtns.Length; i++)
            {
                bool open = tableOpen && i == activeIndex;
                bool hover = hoverZone == 200 + i;

                Color fill = open ? RGBA(70, 214, 155, 0.08f)
                            : hover ? RGBA(79, 201, 240, 0.10f) : TRANSPARENT;
                Color border = open ? RGBA(70, 214, 155, 0.50f)
                             : hover ? RGBA(79, 201, 240, 0.55f) : RGBA(79, 201, 240, 0.28f);
                Color text = open ? GREEN : hover ? Color.white : new Color(0.498f, 0.847f, 1f, 1f);

                btnFill[i] = Color.Lerp(btnFill[i], fill, k);
                btnBorder[i] = Color.Lerp(btnBorder[i], border, k);
                btnTextColor[i] = Color.Lerp(btnTextColor[i], text, k);

                if (tableBtns[i] != null)
                {
                    tableBtns[i].color = btnFill[i];
                    tableBtns[i].borderColor = btnBorder[i];
                    tableBtns[i].Refresh();
                }
                if (tableBtnLabels != null && i < tableBtnLabels.Length && tableBtnLabels[i] != null)
                    tableBtnLabels[i].color = btnTextColor[i];
            }
        }

        void SnapNav()
        {
            for (int i = 0; i < navFill.Length; i++)
            {
                bool active = i == activeIndex;
                navFill[i] = active ? RGBA(40, 140, 200, 0.32f) : TRANSPARENT;
                navFill2[i] = active ? RGBA(20, 60, 90, 0.10f) : TRANSPARENT;
                navBorder[i] = active ? RGBA(79, 201, 240, 0.60f) : RGBA(126, 205, 245, 0.20f);
                navLabelColor[i] = active ? Color.white : TEXT_NAV;
                navIdxColor[i] = active ? ACCENT : RGBA(79, 201, 240, 0.68f);
                navAccentColor[i] = active ? ACCENT : RGBA(79, 201, 240, 0.32f);
                navShift[i] = 0f;

                if (navBg[i] != null)
                {
                    navBg[i].color = navFill[i];
                    navBg[i].color2 = navFill2[i];
                    navBg[i].gradient = true;
                    navBg[i].borderColor = navBorder[i];
                    navBg[i].Refresh();
                }
                if (navAccent != null && i < navAccent.Length && navAccent[i] != null) navAccent[i].color = navAccentColor[i];
                if (navIdx != null && i < navIdx.Length && navIdx[i] != null) navIdx[i].color = navIdxColor[i];
                if (navLabels != null && i < navLabels.Length && navLabels[i] != null) navLabels[i].color = navLabelColor[i];
                if (navRects != null && i < navRects.Length && navRects[i] != null) navRects[i].anchoredPosition = navBase[i];
            }
        }

        // ============================================================ 交互

        public void ZoneEnter(int zone)
        {
            hoverZone = zone;
        }

        public void ZoneExit(int zone)
        {
            if (hoverZone == zone) hoverZone = -1;
            if (zone == 300) HideEnergyHover();
            else if (zone >= 400 && zone < 500) HideBarHover(zone - 400);
            else if (zone >= 500 && zone < 600) HideTooltip(tooltipRight);
            else if (zone >= 600 && zone < 700) HideTooltip(tooltipRight);
        }

        public void ZoneMove(int zone, PointerEventData eventData)
        {
            if (zone == 300) HandleEnergyMove(eventData);
            else if (zone >= 400 && zone < 500) HandleBarMove(zone - 400, eventData);
            else if (zone >= 500 && zone < 600) HandleMixMove(zone - 500, eventData);
            else if (zone >= 600 && zone < 700) HandleChangeMove(zone - 600, eventData);
        }

        public void ZoneClick(int zone)
        {
            if (zone >= 100 && zone < 200) Select(zone - 100);
            else if (zone >= 200 && zone < 300) ToggleTable();
        }

        public void Select(int index)
        {
            if (index < 0 || index >= pageRoots.Length || index == activeIndex) return;

            if (pageRoots[activeIndex] != null) pageRoots[activeIndex].SetActive(false);
            activeIndex = index;
            if (pageRoots[index] != null) pageRoots[index].SetActive(true);
            if (index < pageContents.Length && pageContents[index] != null) pageContents[index].alpha = 1f;

            HideAllHover();
            ApplyTableState();
            StartCoroutine(PlayPageAnimations(index));
        }

        void ToggleTable()
        {
            tableOpen = !tableOpen;
            ApplyTableState();
        }

        void ApplyTableState()
        {
            for (int i = 0; i < tableViews.Length; i++)
                if (tableViews[i] != null) tableViews[i].SetActive(tableOpen && i == activeIndex);

            for (int i = 0; i < tableBtnLabels.Length; i++)
            {
                if (tableBtnLabels[i] == null) continue;
                bool showClose = tableOpen && i == activeIndex;
                tableBtnLabels[i].text = showClose ? "关闭表格" : "数据表";
                ResizeTableButton(i, showClose ? 94f : 72f);
            }
        }

        void ResizeTableButton(int i, float width)
        {
            if (tableBtns == null || i >= tableBtns.Length || tableBtns[i] == null) return;

            RectTransform rt = tableBtns[i].rectTransform;
            RectTransform parent = rt.parent as RectTransform;
            if (parent == null) return;

            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(parent.rect.width - 18f - width, rt.anchoredPosition.y);
            tableBtns[i].Refresh();

            if (tableBtnLabels != null && i < tableBtnLabels.Length && tableBtnLabels[i] != null)
            {
                RectTransform lt = tableBtnLabels[i].rectTransform;
                lt.sizeDelta = new Vector2(width, lt.sizeDelta.y);
            }
        }

        void HideAllHover()
        {
            HideEnergyHover();
            for (int i = 0; i < anims.Length; i++)
            {
                HideBarHover(i);
            }
            HideTooltip(tooltipLeft);
            HideTooltip(tooltipRight);
        }

        // ============================================================ 图表提示

        void HandleEnergyMove(PointerEventData e)
        {
            if (energy == null || energy.days == null || energy.days.Length == 0 || energyZone == null) return;

            int n = Mathf.Min(energy.days.Length, energy.values.Length);
            if (n <= 0) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(energyZone, e.position, null, out local)) return;

            Rect r = energyZone.rect;
            float t = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
            int index = Mathf.Clamp(Mathf.RoundToInt(t * (n - 1)), 0, n - 1);

            if (energyPointer != null)
            {
                energyPointer.gameObject.SetActive(true);
                RectTransform prt = energyPointer;
                float px = energyPlotLeft + (n > 1 ? index / (float)(n - 1) : 0f) * energyPlotWidth;
                prt.anchoredPosition = new Vector2(px, -energyPlotTop);
                prt.sizeDelta = new Vector2(1f, energyPlotHeight);
            }

            ShowTooltip(tooltipLeft, energy.days[index], new[] { "用电量" },
                new[] { energy.values[index].ToString("N0") + " kWh" },
                new[] { "#eaf6ff" }, e.position);
        }

        void HideEnergyHover()
        {
            if (energyPointer != null) energyPointer.gameObject.SetActive(false);
            HideTooltip(tooltipLeft);
        }

        void HandleBarMove(int page, PointerEventData e)
        {
            if (page < 0 || page >= anims.Length) return;
            PageAnim a = anims[page];
            if (a == null || a.barZone == null) return;

            Main5DeviceInfo[] devices = GetDevices(page);
            if (devices.Length == 0) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(a.barZone, e.position, null, out local)) return;

            Rect r = a.barZone.rect;
            float t = (local.x - r.xMin) / Mathf.Max(1f, r.width);
            int index = Mathf.Clamp(Mathf.FloorToInt(t * devices.Length), 0, devices.Length - 1);

            if (a.barHighlight != null)
            {
                a.barHighlight.gameObject.SetActive(true);
                float band = r.width / devices.Length;
                RectTransform hr = a.barHighlight.rectTransform;
                hr.anchoredPosition = a.barZone.anchoredPosition + new Vector2(index * band, 0f);
                hr.sizeDelta = new Vector2(band, r.height);
                a.barHighlight.Refresh();
            }

            Main5DeviceInfo d = devices[index];
            float pct = d.prev > 0f ? (d.now - d.prev) / d.prev * 100f : 0f;
            string color = pct >= 0f ? "#ffcf86" : "#6fe3ae";

            ShowTooltip(tooltipRight, d.name,
                new[] { "本月", "上月", "环比" },
                new[] { d.now.ToString("N0") + " kWh", d.prev.ToString("N0") + " kWh", FmtPct(pct) },
                new[] { "#eaf6ff", "#eaf6ff", color }, e.position);
        }

        void HideBarHover(int page)
        {
            if (page >= 0 && page < anims.Length && anims[page] != null && anims[page].barHighlight != null)
                anims[page].barHighlight.gameObject.SetActive(false);
            HideTooltip(tooltipRight);
        }

        void HandleMixMove(int page, PointerEventData e)
        {
            if (page < 0 || page >= anims.Length) return;
            PageAnim a = anims[page];
            if (a == null || a.mixZone == null) return;

            Main5DeviceInfo[] devices = GetDevices(page);
            if (devices.Length == 0) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(a.mixZone, e.position, null, out local)) return;

            Rect r = a.mixZone.rect;
            float t = Mathf.InverseLerp(r.xMin, r.xMax, local.x);

            float total = 0f;
            for (int i = 0; i < devices.Length; i++) total += devices[i].now;
            if (total <= 0f) return;

            float acc = 0f;
            int index = devices.Length - 1;
            for (int i = 0; i < devices.Length; i++)
            {
                acc += devices[i].now / total;
                if (t <= acc) { index = i; break; }
            }

            float pct = devices[index].now / total * 100f;
            ShowTooltip(tooltipRight, devices[index].name,
                new[] { "本月电耗", "占比" },
                new[] { devices[index].now.ToString("N0") + " kWh", pct.ToString("0.0") + "%" },
                new[] { "#eaf6ff", "#eaf6ff" }, e.position);
        }

        void HandleChangeMove(int page, PointerEventData e)
        {
            if (page < 0 || page >= anims.Length) return;
            PageAnim a = anims[page];
            if (a == null || a.changeZone == null) return;

            Main5DeviceInfo[] sorted = GetSortedDevices(page);
            if (sorted.Length == 0) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(a.changeZone, e.position, null, out local)) return;

            Rect r = a.changeZone.rect;
            float t = (r.yMax - local.y) / Mathf.Max(1f, r.height);
            int index = Mathf.Clamp(Mathf.FloorToInt(t * sorted.Length), 0, sorted.Length - 1);

            Main5DeviceInfo d = sorted[index];
            float diff = d.now - d.prev;
            float pct = d.prev > 0f ? diff / d.prev * 100f : 0f;
            string color = pct >= 0f ? "#ffcf86" : "#6fe3ae";

            ShowTooltip(tooltipRight, d.name,
                new[] { "本月", "上月", "增减", "增幅" },
                new[]
                {
                    d.now.ToString("N0") + " kWh",
                    d.prev.ToString("N0") + " kWh",
                    (diff >= 0f ? "+" : "") + diff.ToString("N0") + " kWh",
                    FmtPct(pct)
                },
                new[] { "#eaf6ff", "#eaf6ff", color, color }, e.position);
        }

        Main5DeviceInfo[] GetDevices(int page)
        {
            if (pageData == null || page < 0 || page >= pageData.Length || pageData[page] == null)
                return new Main5DeviceInfo[0];
            return pageData[page].devices ?? new Main5DeviceInfo[0];
        }

        Main5DeviceInfo[] GetSortedDevices(int page)
        {
            Main5DeviceInfo[] src = GetDevices(page);
            var list = new List<Main5DeviceInfo>(src);
            list.Sort((x, y) => PctValue(y).CompareTo(PctValue(x)));
            return list.ToArray();
        }

        static float PctValue(Main5DeviceInfo d)
        {
            return d.prev > 0f ? (d.now - d.prev) / d.prev * 100f : 0f;
        }

        void ShowTooltip(Main5Tooltip tip, string title, string[] labels, string[] values, string[] colors, Vector2 pos)
        {
            if (tip != null) tip.Show(title, labels, values, colors, pos);
        }

        static void HideTooltip(Main5Tooltip tip)
        {
            if (tip != null) tip.Hide();
        }

        // ============================================================ 工具

        static string FmtPct(float v)
        {
            return (v >= 0f ? "+" : "") + v.ToString("0.0") + "%";
        }

        static Color RGBA(int r, int g, int b, float a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        static void SetAlpha(Graphic g, float a)
        {
            if (g == null) return;
            Color c = g.color;
            c.a = a;
            g.color = c;
        }

        static float EaseOut(float t)
        {
            float inv = 1f - Mathf.Clamp01(t);
            return 1f - inv * inv * inv;
        }
    }
}
