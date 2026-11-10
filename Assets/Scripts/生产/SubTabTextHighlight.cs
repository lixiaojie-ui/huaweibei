using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// Canvas main3 里的三个子页签（产线总览 / 生产动画 / 设备信息）互斥高亮：
    /// 点中的按钮文字变成主题蓝，另外两个恢复各自原本的文字颜色。
    /// </summary>
    public static class SubTabTextHighlight
    {
        static readonly string[] TabNames = { "chanxianzonglan", "shenchandonghua", "shebeixinxi" };
        static readonly string[] TabTexts = { "产线总览", "生产动画", "设备信息" };

        // 选中高亮色 #5BB5F1
        static readonly Color HighlightColor = new Color(0x5B / 255f, 0xB5 / 255f, 0xF1 / 255f, 1f);

        sealed class Entry
        {
            public Button button;
            public TMP_Text tmp;
            public Text legacy;
            public Color original;
            public Color highlight;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            List<Entry> entries = new List<Entry>();

            foreach (Button button in Object.FindObjectsOfType<Button>(true))
            {
                if (button == null) continue;
                if (!IsTargetTab(button)) continue;

                TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
                Text legacy = tmp == null ? button.GetComponentInChildren<Text>(true) : null;

                Color original = tmp != null ? tmp.color
                    : legacy != null ? legacy.color
                    : Color.white;

                Entry entry = new Entry
                {
                    button = button,
                    tmp = tmp,
                    legacy = legacy,
                    original = original,
                    highlight = HighlightColor
                };

                Debug.Log("[SubTabTextHighlight] '" + button.gameObject.name +
                    "' 原本色=" + original +
                    " 高亮色=" + entry.highlight);

                Entry captured = entry;
                button.onClick.AddListener(() => Select(entries, captured));

                entries.Add(entry);
            }

            Debug.Log("[SubTabTextHighlight] 已接管 " + entries.Count + " 个子页签按钮的文字高亮。");
        }

        static bool IsTargetTab(Button button)
        {
            string name = button.gameObject.name.ToLowerInvariant();
            foreach (string tab in TabNames)
                if (name == tab || name.Contains(tab))
                    return true;

            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                foreach (string text in TabTexts)
                    if (tmp.text.Contains(text))
                        return true;

            return false;
        }

        static void Select(List<Entry> entries, Entry selected)
        {
            foreach (Entry entry in entries)
                SetColor(entry, entry == selected ? selected.highlight : entry.original);

            Debug.Log("[SubTabTextHighlight] 选中：" + selected.button.gameObject.name);
        }

        static void SetColor(Entry entry, Color color)
        {
            if (entry.tmp != null)
                entry.tmp.color = color;
            else if (entry.legacy != null)
                entry.legacy.color = color;
        }
    }
}
