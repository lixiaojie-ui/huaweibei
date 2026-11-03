using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 表格自动滚动（用于订单明细）。
    /// 匀速向下滚动，到底停留片刻后回到顶部继续；用户拖动/滚轮时暂停。
    /// </summary>
    public class TableAutoScroll : MonoBehaviour
    {
        [Header("不填则自动取自身 ScrollRect")]
        public ScrollRect scrollRect;

        [Header("滚动速度（像素/秒，基于 1080 参考分辨率）")]
        public float pixelsPerSecond = 18f;

        [Header("到底后停留秒数")]
        public float bottomPause = 2f;

        [Header("回到顶部后停留秒数")]
        public float topPause = 0.6f;

        Canvas canvas;
        float wait;
        bool atBottom;

        void Awake()
        {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            canvas = GetComponentInParent<Canvas>();
        }

        void Update()
        {
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
                return;

            float range = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
            if (range <= 1f) return;   // 内容没超出视口，不滚动

            if (UserBusy())
            {
                if (scrollRect.verticalNormalizedPosition > 0.01f) atBottom = false;
                return;
            }

            if (atBottom && scrollRect.verticalNormalizedPosition > 0.01f)
                atBottom = false;

            if (wait > 0f)
            {
                wait -= Time.unscaledDeltaTime;
                if (wait <= 0f && atBottom)
                {
                    atBottom = false;
                    scrollRect.verticalNormalizedPosition = 1f;   // 1 = 顶部
                    wait = topPause;
                }
                return;
            }

            float position = scrollRect.verticalNormalizedPosition -
                             pixelsPerSecond * Time.unscaledDeltaTime / range;

            if (position <= 0f)
            {
                position = 0f;
                atBottom = true;
                wait = bottomPause;
            }

            scrollRect.verticalNormalizedPosition = position;
        }

        bool UserBusy()
        {
            // 鼠标在表格上按下拖动或滚轮时让位给用户操作
            if (!PointerOverTable()) return false;
            return Input.GetMouseButton(0) || Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f;
        }

        bool PointerOverTable()
        {
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(
                scrollRect.viewport, Input.mousePosition, camera);
        }
    }

    /// <summary>
    /// 兜底：场景里的订单表格如果因为历史原因没挂上滚动组件，运行时自动补一个，保证表格始终自动滚动。
    /// </summary>
    public static class TableAutoScrollBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Ensure()
        {
            ScrollRect scroll = FindTableScroll();
            if (scroll == null) return;

            TableAutoScroll existing = scroll.GetComponent<TableAutoScroll>();
            if (existing != null)
            {
                existing.enabled = true;
                return;
            }

            scroll.gameObject.AddComponent<TableAutoScroll>();
        }

        static ScrollRect FindTableScroll()
        {
            Transform image1 = FindDeep("Image1", "Canvas left");
            if (image1 != null)
            {
                ScrollRect scroll = image1.GetComponentInChildren<ScrollRect>(true);
                if (scroll != null) return scroll;
            }

            Transform order = FindDeep("订单明细", null);
            if (order != null) return order.GetComponentInChildren<ScrollRect>(true);

            return null;
        }

        static Transform FindDeep(string name, string ancestor)
        {
            Transform[] all = Object.FindObjectsOfType<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name != name) continue;
                if (ancestor == null) return t;

                Transform parent = t.parent;
                while (parent != null)
                {
                    if (parent.name == ancestor) return t;
                    parent = parent.parent;
                }
            }
            return null;
        }
    }
}
