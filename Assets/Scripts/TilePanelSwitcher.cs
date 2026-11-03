using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 大屏页签切换：点击 TileButtonN 显示 Canvas mainN，其它页面隐藏。
    /// 选中按钮整体变为 #00F8FF，并重播该页面的图表入场动画。
    /// </summary>
    public static class TilePanelSwitcher
    {
        sealed class ButtonEntry
        {
            public int index;
            public Button button;
            public Graphic[] graphics;
            public Color[] colors;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            // circle / annulus 是径向菜单的装饰背景，Raycast 会挡住中心的 Button robot
            DisableRaycastBlocker("circle");
            DisableRaycastBlocker("annulus");

            // 收集 Canvas mainN
            var pages = new Dictionary<int, GameObject>();
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null) continue;

                int index = ParsePageIndex(canvas.gameObject.name);
                if (index <= 0 || pages.ContainsKey(index)) continue;

                pages[index] = canvas.gameObject;
            }

            // 兜底：页面不一定是 Canvas 组件，也可能是同名的普通 GameObject
            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t == null) continue;

                int index = ParsePageIndex(t.name);
                if (index <= 0 || pages.ContainsKey(index)) continue;

                pages[index] = t.gameObject;
            }

            if (pages.Count == 0) return;

            // 收集 TileButtonN，并记录每个按钮所有 Graphic 的原始颜色
            var entries = new List<ButtonEntry>();
            Button[] buttons = Object.FindObjectsOfType<Button>(true);

            foreach (Button button in buttons)
            {
                if (button == null) continue;

                int index = ParseIndex(button.gameObject.name, "TileButton");
                if (index <= 0 &&
                    string.Equals(button.gameObject.name, "Button robot", System.StringComparison.OrdinalIgnoreCase))
                {
                    index = 6;   // Button robot 对应 Canvas main6

                    if (!pages.ContainsKey(6))
                        Debug.LogWarning("[TilePanelSwitcher] 找到 Button robot，但场景里没有 Canvas main6，点击不会切页。");
                }

                if (index <= 0 || !pages.ContainsKey(index)) continue;

                Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
                var colors = new Color[graphics.Length];

                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] != null) colors[i] = graphics[i].color;
                }

                entries.Add(new ButtonEntry
                {
                    index = index,
                    button = button,
                    graphics = graphics,
                    colors = colors
                });

                int target = index;
                button.onClick.AddListener(() =>
                {
                    ShowPage(pages, target);
                    SetSelected(entries, target);
                    ChartEntranceAnimator.PlayPage(pages[target]);
                    Debug.Log("[TilePanelSwitcher] 切换到页面 " + target);
                });
            }

            // 起始页面固定为 TileButton1 / Canvas main1
            int initial = pages.ContainsKey(1) ? 1 : MinKey(pages);
            ShowPage(pages, initial);
            SetSelected(entries, initial);
            ChartEntranceAnimator.PlayPage(pages[initial]);
        }

        static void ShowPage(Dictionary<int, GameObject> pages, int index)
        {
            foreach (KeyValuePair<int, GameObject> pair in pages)
            {
                if (pair.Value != null)
                    pair.Value.SetActive(pair.Key == index);
            }
        }

        static void SetSelected(List<ButtonEntry> entries, int index)
        {
            Color cyan = new Color(0f, 248f / 255f, 1f, 1f);

            foreach (ButtonEntry entry in entries)
            {
                for (int i = 0; i < entry.graphics.Length; i++)
                {
                    Graphic graphic = entry.graphics[i];
                    if (graphic == null) continue;

                    if (entry.index == index)
                    {
                        Color c = cyan;
                        c.a = entry.colors[i].a;
                        graphic.color = c;
                    }
                    else
                    {
                        // 未选中的按钮统一恢复为白色（只保留原来的 alpha）
                        Color white = Color.white;
                        white.a = entry.colors[i].a;
                        graphic.color = white;
                    }
                }
            }
        }

        static int MinKey(Dictionary<int, GameObject> pages)
        {
            int min = int.MaxValue;
            foreach (int key in pages.Keys)
                if (key < min) min = key;
            return min;
        }

        /// <summary>关掉装饰物自身的 Raycast，避免挡住它下面的按钮。</summary>
        static void DisableRaycastBlocker(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null) return;

            Image image = go.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;
        }

        /// <summary>从 "Canvas mainN" / "CanvasMainN" 里取出页面编号。</summary>
        static int ParsePageIndex(string name)
        {
            int index = ParseIndex(name, "Canvas main");
            if (index > 0) return index;

            return ParseIndex(name, "CanvasMain");
        }

        /// <summary>从 "TileButton3" / "Canvas main3" 里取出编号 3。</summary>
        static int ParseIndex(string name, string prefix)
        {
            if (string.IsNullOrEmpty(name) ||
                !name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return 0;

            string rest = name.Substring(prefix.Length).Trim();
            int value = 0;
            int digits = 0;

            for (int i = 0; i < rest.Length && char.IsDigit(rest[i]); i++)
            {
                value = value * 10 + (rest[i] - '0');
                digits++;
            }

            return digits > 0 ? value : 0;
        }
    }
}
