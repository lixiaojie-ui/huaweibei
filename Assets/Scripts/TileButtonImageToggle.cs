using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 页签联动：点击 TileButton3 时隐藏背景图 Image 123 / Image 123 (1)，
    /// 点击其它页签（TileButton1/2/4/5、Button robot）时恢复显示。
    /// 这两张图挂在根 Canvas 下，不随 Canvas mainN 的显隐自动切换，所以单独处理。
    /// </summary>
    public static class TileButtonImageToggle
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var targets = new List<GameObject>();
            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);

            foreach (Transform t in transforms)
            {
                if (t != null && IsTargetName(t.name)) targets.Add(t.gameObject);
            }

            if (targets.Count == 0)
            {
                Debug.LogWarning("[TileButtonImageToggle] 没有找到 Image 123 / Image 123 (1)。");
                return;
            }

            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null) continue;

                string buttonName = button.gameObject.name;
                if (buttonName == "TileButton3")
                    button.onClick.AddListener(() => SetVisible(targets, false));
                else if (buttonName.StartsWith("TileButton") || buttonName == "Button robot")
                    button.onClick.AddListener(() => SetVisible(targets, true));
            }
        }

        static bool IsTargetName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;

            string normalized = objectName.Replace(" ", "").Replace("（", "(").Replace("）", ")");
            return normalized == "Image123" || normalized == "Image123(1)";
        }

        static void SetVisible(List<GameObject> targets, bool visible)
        {
            foreach (GameObject target in targets)
            {
                if (target != null) target.SetActive(visible);
            }
            Debug.Log("[TileButtonImageToggle] Image 123 显示：" + visible);
        }
    }
}
