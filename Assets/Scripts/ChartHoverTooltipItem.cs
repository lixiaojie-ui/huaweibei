using UnityEngine;
using UnityEngine.EventSystems;

namespace Daima
{
    /// <summary>
    /// 挂在图表数据元素上，鼠标进入/移动时显示提示框。
    /// </summary>
    public class ChartHoverTooltipItem : MonoBehaviour,
        IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public ChartHoverTooltipView view;
        public string content;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (view != null) view.Show(content, eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (view != null) view.Show(content, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (view != null) view.Hide();
        }

        void OnDisable()
        {
            if (view != null) view.Hide();
        }
    }
}
