using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在按钮上：点一下面板从右侧滑入，再点一下滑出屏幕；
/// 打开另一个按钮的面板时，当前面板自动滑出。
/// 面板留空时，该按钮只作为“关闭全部”按钮使用（调用 CloseAll）。
/// </summary>
public class PanelSwitcher : MonoBehaviour
{
    [Tooltip("这个按钮要控制的面板；留空表示不打开任何面板")]
    [SerializeField] private GameObject panel;

    [Tooltip("滑动时长（秒）")]
    [SerializeField] private float duration = 0.25f;

    private static readonly List<PanelSwitcher> all = new List<PanelSwitcher>();
    private static PanelSwitcher opened;

    private Vector2 restPos;
    private bool restCached;

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Toggle()
    {
        if (panel == null) return;

        if (panel.activeSelf) Close();
        else
        {
            if (opened != null && opened != this) opened.Close();
            Open();
        }
    }

    /// <summary>所有面板一起右滑出屏幕。</summary>
    public static void CloseAllPanels()
    {
        for (int i = all.Count - 1; i >= 0; i--)
            all[i].Close();
    }

    private void Open()
    {
        var rt = panel.GetComponent<RectTransform>();
        panel.SetActive(true);
        opened = this;

        if (rt == null) return;

        if (!restCached)
        {
            restPos = rt.anchoredPosition;
            restCached = true;
        }

        var start = restPos + new Vector2(rt.rect.width, 0f);
        rt.anchoredPosition = start;

        StopAllCoroutines();
        StartCoroutine(Slide(rt, start, restPos, false));
    }

    private void Close()
    {
        if (panel == null) return;
        if (opened == this) opened = null;

        var rt = panel.GetComponent<RectTransform>();
        if (rt == null || !restCached)
        {
            panel.SetActive(false);
            return;
        }

        var end = restPos + new Vector2(rt.rect.width, 0f);

        StopAllCoroutines();
        StartCoroutine(Slide(rt, rt.anchoredPosition, end, true));
    }

    private IEnumerator Slide(RectTransform rt, Vector2 from, Vector2 to, bool hideAtEnd)
    {
        float t = 0f;
        float d = Mathf.Max(duration, 0.01f);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / d;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }

        rt.anchoredPosition = to;
        if (hideAtEnd) panel.SetActive(false);
    }
}
