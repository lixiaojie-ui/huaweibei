using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 在 UI 里画折线（用归一化坐标：0~1 映射到自身 Rect）。
    /// 不依赖任何贴图/图片，只在网格里生成三角面。
    /// </summary>
    public class UILineGraphic : MaskableGraphic
    {
        [SerializeField] List<Vector2> points = new List<Vector2>();
        public float thickness = 2f;
        [SerializeField] float dotSize = 0f;
        [SerializeField] Color dotColor = Color.white;

        public void SetData(IList<Vector2> normalizedPoints, float lineThickness, float pointSize, Color pointsColor)
        {
            points = new List<Vector2>(normalizedPoints);
            thickness = lineThickness;
            dotSize = pointSize;
            dotColor = pointsColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Count < 2) return;

            Rect rect = rectTransform.rect;
            Vector3 previous = ToLocal(points[0], rect);

            if (dotSize > 0f) AddDot(vh, previous);
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 current = ToLocal(points[i], rect);
                AddSegment(vh, previous, current);
                if (dotSize > 0f) AddDot(vh, current);
                previous = current;
            }
        }

        static Vector3 ToLocal(Vector2 point, Rect rect)
        {
            return new Vector3(rect.xMin + point.x * rect.width,
                               rect.yMin + point.y * rect.height,
                               0f);
        }

        void AddSegment(VertexHelper vh, Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            Vector3 normal = new Vector3(-dir.y, dir.x, 0f) * (thickness * 0.5f);

            var v = new UIVertex[4];
            for (int i = 0; i < 4; i++) v[i].color = color;
            v[0].position = a - normal;
            v[1].position = a + normal;
            v[2].position = b + normal;
            v[3].position = b - normal;

            int index = vh.currentVertCount;
            for (int i = 0; i < 4; i++) vh.AddVert(v[i]);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }

        void AddDot(VertexHelper vh, Vector3 center)
        {
            float half = dotSize * 0.5f;

            var v = new UIVertex[4];
            for (int i = 0; i < 4; i++) v[i].color = dotColor;
            v[0].position = center + new Vector3(-half, -half, 0f);
            v[1].position = center + new Vector3(-half, half, 0f);
            v[2].position = center + new Vector3(half, half, 0f);
            v[3].position = center + new Vector3(half, -half, 0f);

            int index = vh.currentVertCount;
            for (int i = 0; i < 4; i++) vh.AddVert(v[i]);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }

    /// <summary>图表内的浮层提示框。</summary>
    public class ChartTooltip : MonoBehaviour
    {
        public RectTransform panel;
        public Text label;
        public Canvas canvas;
        [SerializeField] RectTransform bounds;

        public void Init(RectTransform boundsRect, Canvas targetCanvas)
        {
            bounds = boundsRect;
            canvas = targetCanvas;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public void Show(string content, Vector2 screenPosition)
        {
            if (panel == null || label == null) return;

            label.text = content;
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();

            Camera camera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = canvas.worldCamera;

            Vector2 local;
            if (bounds == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, screenPosition, camera, out local))
                return;

            Vector2 size = panel.sizeDelta;
            Rect area = bounds.rect;
            Vector2 position = local + new Vector2(14f, -14f);

            position.x = Mathf.Clamp(position.x, area.xMin + 2f, area.xMax - size.x - 2f);
            position.y = Mathf.Clamp(position.y, area.yMin + size.y + 2f, area.yMax - 2f);
            panel.localPosition = position;
        }

        public void Hide()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }
    }

    /// <summary>挂在可悬停的图元上，显示图表提示。</summary>
    public class ChartTooltipTrigger : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public ChartTooltip tooltip;
        public string content;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip != null) tooltip.Show(content, eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (tooltip != null) tooltip.Show(content, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null) tooltip.Hide();
        }

        void OnDisable()
        {
            if (tooltip != null) tooltip.Hide();
        }
    }
}
