using System.Collections;
using UnityEngine;

namespace Daima
{
    /// <summary>
    /// Canvas main5 右侧分段页切换：button1~button6 对应 Image1~Image6。
    /// 按钮和页面都在场景里，脚本只负责显隐与选中态。
    /// </summary>
    public class SectionPageSwitcher : MonoBehaviour
    {
        public GameObject[] pages = new GameObject[0];
        public SectionTabButton[] tabs = new SectionTabButton[0];
        public int initialIndex = 0;
        public float fadeDuration = 0.16f;

        CanvasGroup[] groups;
        int current = -1;

        void Awake()
        {
            int count = Mathf.Min(pages != null ? pages.Length : 0, tabs != null ? tabs.Length : 0);
            groups = new CanvasGroup[count];

            for (int i = 0; i < count; i++)
            {
                if (pages[i] == null) continue;

                CanvasGroup group = pages[i].GetComponent<CanvasGroup>();
                if (group == null) group = pages[i].AddComponent<CanvasGroup>();
                groups[i] = group;

                if (tabs[i] != null && tabs[i].button != null)
                {
                    int index = i;
                    tabs[i].button.onClick.AddListener(() => Show(index));
                }
            }

            Show(Mathf.Clamp(initialIndex, 0, Mathf.Max(0, count - 1)), true);
        }

        public void Show(int index)
        {
            Show(index, false);
        }

        public void Show(int index, bool instant)
        {
            if (pages == null || pages.Length == 0) return;
            if (index < 0 || index >= pages.Length) return;
            if (index == current && !instant) return;

            current = index;
            StopAllCoroutines();

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == null) continue;

                bool active = i == index;
                pages[i].SetActive(active);

                CanvasGroup group = i < groups.Length ? groups[i] : null;
                if (group == null) continue;

                group.alpha = active ? (instant ? 1f : 0f) : 0f;
                group.blocksRaycasts = active;
                group.interactable = active;
            }

            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null) tabs[i].SetSelected(i == index);
            }

            if (!instant && groups != null && index < groups.Length && groups[index] != null)
                StartCoroutine(FadeIn(groups[index]));
        }

        IEnumerator FadeIn(CanvasGroup group)
        {
            float duration = Mathf.Max(0.01f, fadeDuration);
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(time / duration);
                yield return null;
            }
            group.alpha = 1f;
        }
    }
}
