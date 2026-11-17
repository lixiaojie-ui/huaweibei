using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 圆角矩形：支持每角半径、描边、水平/垂直渐变。
    /// 用于安环能耗页的卡片、按钮、导航项、标签、图表柱与散点。
    /// </summary>
    [AddComponentMenu("UI/Main5 Round Rect")]
    public class UIRoundRect : MaskableGraphic
    {
        public float radius = 4f;
        [Tooltip("左上、右上、右下、左下；全 0 时使用 radius")]
        public Vector4 radii = Vector4.zero;
        public float borderWidth = 0f;
        public Color borderColor = Color.white;
        public bool gradient = false;
        public Color color2 = Color.white;
        public bool verticalGradient = false;

        [Range(1, 16)] public int segments = 6;

        public void SetFill(Color c)
        {
            color = c;
            SetVerticesDirty();
        }

        public void SetFill(Color c, Color c2)
        {
            color = c;
            color2 = c2;
            gradient = true;
            SetVerticesDirty();
        }

        public void SetBorder(Color c, float w)
        {
            borderColor = c;
            borderWidth = w;
            SetVerticesDirty();
        }

        public void SetRadius(float r)
        {
            radius = r;
            SetVerticesDirty();
        }

        public void Refresh()
        {
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0.01f || r.height <= 0.01f) return;

            Vector4 rr = radii;
            if (Mathf.Approximately(rr.x, 0f) && Mathf.Approximately(rr.y, 0f) &&
                Mathf.Approximately(rr.z, 0f) && Mathf.Approximately(rr.w, 0f))
            {
                rr = new Vector4(radius, radius, radius, radius);
            }

            float maxR = Mathf.Min(r.width, r.height) * 0.5f;
            rr.x = Mathf.Clamp(rr.x, 0f, maxR);
            rr.y = Mathf.Clamp(rr.y, 0f, maxR);
            rr.z = Mathf.Clamp(rr.z, 0f, maxR);
            rr.w = Mathf.Clamp(rr.w, 0f, maxR);

            List<Vector2> outer = BuildOutline(r, rr);
            if (outer.Count < 4) return;

            if (borderWidth > 0.01f)
            {
                float bw = Mathf.Min(borderWidth, maxR);
                Rect inner = new Rect(r.xMin + bw, r.yMin + bw,
                                      Mathf.Max(0.01f, r.width - bw * 2f),
                                      Mathf.Max(0.01f, r.height - bw * 2f));
                Vector4 ir = new Vector4(Mathf.Max(0f, rr.x - bw), Mathf.Max(0f, rr.y - bw),
                                         Mathf.Max(0f, rr.z - bw), Mathf.Max(0f, rr.w - bw));
                List<Vector2> innerPts = BuildOutline(inner, ir);

                if (innerPts.Count == outer.Count)
                {
                    int ringStart = vh.currentVertCount;
                    UIVertex bv = UIVertex.simpleVert;
                    bv.uv0 = Vector2.zero;
                    bv.color = borderColor;

                    for (int i = 0; i < outer.Count; i++)
                    {
                        bv.position = new Vector3(outer[i].x, outer[i].y, 0f);
                        vh.AddVert(bv);
                        bv.position = new Vector3(innerPts[i].x, innerPts[i].y, 0f);
                        vh.AddVert(bv);
                    }

                    for (int i = 0; i < outer.Count; i++)
                    {
                        int a = ringStart + i * 2;
                        int b = a + 1;
                        int c = ringStart + ((i + 1) % outer.Count) * 2;
                        int d = c + 1;
                        vh.AddTriangle(a, c, b);
                        vh.AddTriangle(c, d, b);
                    }
                }
            }

            int fanStart = vh.currentVertCount;
            UIVertex cv = UIVertex.simpleVert;
            cv.uv0 = Vector2.zero;
            cv.position = new Vector3(r.center.x, r.center.y, 0f);
            cv.color = SampleColor(r.center, r);
            vh.AddVert(cv);

            for (int i = 0; i < outer.Count; i++)
            {
                UIVertex v = UIVertex.simpleVert;
                v.uv0 = Vector2.zero;
                v.position = new Vector3(outer[i].x, outer[i].y, 0f);
                v.color = SampleColor(outer[i], r);
                vh.AddVert(v);
            }

            for (int i = 0; i < outer.Count; i++)
            {
                int a = fanStart + 1 + i;
                int b = fanStart + 1 + ((i + 1) % outer.Count);
                vh.AddTriangle(fanStart, a, b);
            }
        }

        Color SampleColor(Vector2 p, Rect r)
        {
            if (!gradient) return color;

            float t = verticalGradient
                ? Mathf.InverseLerp(r.yMin, r.yMax, p.y)
                : Mathf.InverseLerp(r.xMin, r.xMax, p.x);
            return Color.Lerp(color, color2, t);
        }

        /// <summary>顺时针生成轮廓：左上、右上、右下、左下。</summary>
        List<Vector2> BuildOutline(Rect r, Vector4 rad)
        {
            var pts = new List<Vector2>(4 * (segments + 1));

            AddArc(pts, new Vector2(r.xMin + rad.x, r.yMax - rad.x), rad.x, 180f, 90f);
            AddArc(pts, new Vector2(r.xMax - rad.y, r.yMax - rad.y), rad.y, 90f, 0f);
            AddArc(pts, new Vector2(r.xMax - rad.z, r.yMin + rad.z), rad.z, 0f, -90f);
            AddArc(pts, new Vector2(r.xMin + rad.w, r.yMin + rad.w), rad.w, -90f, -180f);

            return pts;
        }

        void AddArc(List<Vector2> pts, Vector2 center, float rad, float fromDeg, float toDeg)
        {
            int n = Mathf.Max(1, segments);
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)n) * Mathf.Deg2Rad;
                pts.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad);
            }
        }
    }
}
