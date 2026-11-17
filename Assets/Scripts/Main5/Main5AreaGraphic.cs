using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>折线下方的渐变面积填充（归一化坐标，y 自底向上），用于左 02 能耗趋势。</summary>
    public class Main5AreaGraphic : MaskableGraphic
    {
        [SerializeField] List<Vector2> points = new List<Vector2>();
        public Color topColor = new Color(0.435f, 0.827f, 1f, 0.30f);
        public Color bottomColor = new Color(0.435f, 0.827f, 1f, 0.02f);

        public void SetData(IList<Vector2> normalizedPoints, Color top, Color bottom)
        {
            points = new List<Vector2>(normalizedPoints);
            topColor = top;
            bottomColor = bottom;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Count < 2) return;

            Rect rect = rectTransform.rect;
            float baseY = rect.yMin;
            float topY = rect.yMax;

            int start = vh.currentVertCount;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i];
                float x = rect.xMin + Mathf.Clamp01(p.x) * rect.width;
                float y = rect.yMin + Mathf.Clamp01(p.y) * rect.height;

                float t = Mathf.InverseLerp(baseY, topY, y);
                UIVertex v = UIVertex.simpleVert;
                v.position = new Vector3(x, y, 0f);
                v.color = Color.Lerp(bottomColor, topColor, t);
                vh.AddVert(v);

                UIVertex b = UIVertex.simpleVert;
                b.position = new Vector3(x, baseY, 0f);
                b.color = bottomColor;
                vh.AddVert(b);
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int a = start + i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                vh.AddTriangle(a, c, b);
                vh.AddTriangle(b, c, d);
            }
        }
    }
}
