using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>雷达图面积填充：顶点色从中心到外圈渐变，纯网格绘制。</summary>
    public class UIRadarFillGraphic : MaskableGraphic
    {
        [SerializeField] List<Vector2> points = new List<Vector2>();
        [SerializeField] Vector2 center = new Vector2(0.5f, 0.5f);
        [SerializeField] Color centerColor = new Color(0.235f, 0.851f, 1f, 0.30f);
        [SerializeField] Color edgeColor = new Color(0.235f, 0.851f, 1f, 0.03f);

        public void SetData(IList<Vector2> pts, Vector2 ctr, Color cCol, Color eCol)
        {
            points = new List<Vector2>(pts);
            center = ctr;
            centerColor = cCol;
            edgeColor = eCol;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Count < 3) return;

            Rect rect = rectTransform.rect;

            var centerVert = UIVertex.simpleVert;
            centerVert.color = centerColor;
            centerVert.position = new Vector3(rect.xMin + center.x * rect.width,
                                              rect.yMin + center.y * rect.height, 0f);
            int start = vh.currentVertCount;
            vh.AddVert(centerVert);

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 pt = points[i];
                var v = UIVertex.simpleVert;
                v.color = edgeColor;
                v.position = new Vector3(rect.xMin + pt.x * rect.width,
                                         rect.yMin + pt.y * rect.height, 0f);
                vh.AddVert(v);
            }

            for (int i = 0; i < points.Count; i++)
            {
                int a = start + 1 + i;
                int b = start + 1 + (i + 1) % points.Count;
                vh.AddTriangle(start, a, b);
            }
        }
    }
}
