using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 表格自动滚动（用于订单明细）。
    /// 匀速向下滚动，到底停留片刻后回到顶部继续；用户拖动/滚轮/正在编辑单元格时自动暂停，
    /// 停止操作 2.5 秒后恢复。
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

        [Header("用户停止操作后恢复滚动的秒数")]
        public float resumeDelay = 2.5f;

        Canvas canvas;
        float wait;
        float idle;
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
                idle = resumeDelay;
                if (scrollRect.verticalNormalizedPosition > 0.01f) atBottom = false;
                return;
            }

            if (idle > 0f)
            {
                idle -= Time.unscaledDeltaTime;
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
            // 正在编辑某个单元格
            var es = EventSystem.current;
            if (es != null)
            {
                var selected = es.currentSelectedGameObject;
                if (selected != null && selected.activeInHierarchy &&
                    selected.transform.IsChildOf(scrollRect.content))
                    return true;
            }

            // 鼠标在表格上按下拖动或滚轮
            if (!PointerOverTable()) return false;
            if (Input.GetMouseButton(0) || Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
                return true;

            return false;
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
}
