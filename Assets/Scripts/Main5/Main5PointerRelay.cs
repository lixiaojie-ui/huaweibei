using UnityEngine;
using UnityEngine.EventSystems;

namespace Daima
{
    /// <summary>把鼠标事件转交给 Main5Dashboard（用 zone 编号区分交互目标）。</summary>
    [AddComponentMenu("Main5/Pointer Relay")]
    public class Main5PointerRelay : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
    {
        public Main5Dashboard dashboard;
        public int zone;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (dashboard != null) dashboard.ZoneEnter(zone);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (dashboard != null) dashboard.ZoneExit(zone);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (dashboard != null) dashboard.ZoneMove(zone, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (zone >= 100 && zone <= 105 && ShowSectionPage())
            {
                if (dashboard != null) dashboard.ZoneClick(zone);
                return;
            }
            if (dashboard != null) dashboard.ZoneClick(zone);
        }

        bool ShowSectionPage()
        {
            Transform right5 = transform;
            while (right5 != null && right5.name != "Canvas right5") right5 = right5.parent;
            if (right5 == null) return false;

            int selected = zone - 99;
            for (int i = 1; i <= 6; i++)
            {
                Transform page = right5.Find("Image" + i);
                if (page != null) page.gameObject.SetActive(i == selected);
            }
            return true;
        }
    }
}
