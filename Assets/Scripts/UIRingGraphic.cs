using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 纯 UI 的圆环/仪表盘图元：按数值比例把圆环切成若干段，不依赖任何贴图。
    /// 角度：0 度为右侧，逆时针为正。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIRingGraphic : MaskableGraphic
    {
        [SerializeField] List<float> values = new List<float> { 1f };
        [SerializeField] List<Color> colors = new List<Color> { Color.white };
        [SerializeField] float ringWidth = 0.18f;   // 环宽 / 半径
        [SerializeField] float startAngle = 90f;
        [SerializeField] float sweepDegrees = 360f;
        [SerializeField] float gapDegrees = 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            // 圆环/仪表盘同样是装饰图元，不接收点击，避免挡住下层按钮
            raycastTarget = false;
        }

        public void SetData(IList<float> newValues, IList<Color> newColors,
                            float widthRatio, float start, float sweep, float gap)
        {
            values = new List<float>(newValues);
            colors = new List<Color>(newColors);
            ringWidth = widthRatio;
            startAngle = start;
            sweepDegrees = sweep;
            gapDegrees = gap;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (values == null || values.Count == 0) return;

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f) return;

            float inner = radius * (1f - Mathf.Clamp01(ringWidth));

            float total = 0f;
            for (int i = 0; i < values.Count; i++) total += Mathf.Max(0f, values[i]);
            if (total <= 0f) return;

            float angle = startAngle;
            for (int i = 0; i < values.Count && i < colors.Count; i++)
            {
                float span = sweepDegrees * Mathf.Max(0f, values[i]) / total;
                float a0 = angle + gapDegrees * 0.5f;
                float a1 = angle + span - gapDegrees * 0.5f;
                if (a1 > a0) AddArc(vh, center, inner, radius, a0, a1, colors[i]);
                angle += span;
            }
        }

        void AddArc(VertexHelper vh, Vector2 center, float inner, float outer,
                    float a0, float a1, Color color)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / 4f));
            int start = vh.currentVertCount;

            for (int i = 0; i <= steps; i++)
            {
                float deg = Mathf.Lerp(a0, a1, i / (float)steps);
                float rad = deg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                var outerVertex = new UIVertex();
                outerVertex.position = center + dir * outer;
                outerVertex.color = color;

                var innerVertex = new UIVertex();
                innerVertex.position = center + dir * inner;
                innerVertex.color = color;

                vh.AddVert(outerVertex);
                vh.AddVert(innerVertex);
            }

            for (int i = 0; i < steps; i++)
            {
                int b = start + i * 2;
                vh.AddTriangle(b, b + 1, b + 3);
                vh.AddTriangle(b, b + 3, b + 2);
            }
        }
    }
}
