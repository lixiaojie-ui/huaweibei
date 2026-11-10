using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// HUD 装饰层（BOTTOM/TOP/background 等全屏 Image）默认开启 Raycast，
    /// 会把 3D 世界空间悬浮按钮的点击全部拦住。
    /// 这里在启动时关掉“非交互”图形的 Raycast，按钮类（Selectable/ScrollRect/EventTrigger）保持不动。
    /// </summary>
    public static class OverlayRaycastCleanup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            int disabled = 0;

            foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                if (canvas.gameObject.name != "Canvas") continue;

                foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic == null || !graphic.raycastTarget) continue;

                    // 有交互职能的保留：按钮/滑块等在 Selectable 上，滚动区域在 ScrollRect 上
                    if (graphic.GetComponentInParent<Selectable>() != null) continue;
                    if (graphic.GetComponentInParent<ScrollRect>() != null) continue;
                    if (graphic.GetComponentInParent<EventTrigger>() != null) continue;

                    graphic.raycastTarget = false;
                    disabled++;
                }
            }

            Debug.Log("[OverlayRaycastCleanup] 关闭了 " + disabled + " 个装饰 UI 的 Raycast。");
        }
    }
}
