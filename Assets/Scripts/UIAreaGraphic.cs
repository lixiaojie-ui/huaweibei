using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>折线下方的渐变面积填充（顶部较亮，底部透明）。</summary>
    public class UIAreaGraphic : MaskableGraphic
    {
        [SerializeField] List<Vector2> points = new List<Vector2>();
        [SerializeField] Color topColor = new Color(0.259f, 0.871f, 0.620f, 0.22f);
        [SerializeField] Color bottomColor = new Color(0.259f, 0.871f, 0.620f, 0.01f);

        public void SetData(IList<Vector2> pts, Color top, Color bottom)
        {
            points = new List<Vector2>(pts);
            topColor = top;
            bottomColor = bottom;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Count < 2) return;

            Rect rect = rectTransform.rect;
            int start = vh.currentVertCount;

            for (int i = 0; i < points.Count; i++)
            {
                var top = UIVertex.simpleVert;
                top.color = topColor;
                top.position = new Vector3(rect.xMin + points[i].x * rect.width,
                                           rect.yMin + points[i].y * rect.height, 0f);
                vh.AddVert(top);

                var bottom = UIVertex.simpleVert;
                bottom.color = bottomColor;
                bottom.position = new Vector3(top.position.x, rect.yMin, 0f);
                vh.AddVert(bottom);
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int a = start + i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                vh.AddTriangle(a, c, b);
                vh.AddTriangle(c, d, b);
            }
        }
    }
}
