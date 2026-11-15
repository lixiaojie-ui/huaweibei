using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Main4QC
{
    /// <summary>
    /// main4 专用轻量绘图组件：提供线段 / 虚线 / 矩形 / 折线 / 面积 / 散点 / 圆弧 / 单元格等基础图元。
    /// 所有坐标使用 RectTransform 归一化坐标（0~1），保证不同分辨率下不溢出。
    /// </summary>
    public class Main4ChartGraphic : MaskableGraphic
    {
        struct Tri
        {
            public Vector3 p;
            public Color32 c;
        }

        readonly List<Tri> _tris = new List<Tri>();
        float _progress = 1f;

        /// <summary>绘制进度（0~1），用于“图表从 0 开始逐根画出”的动效。</summary>
        public float Progress
        {
            get { return _progress; }
            set
            {
                float v = Mathf.Clamp01(value);
                if (Mathf.Approximately(v, _progress)) return;
                _progress = v;
                SetVerticesDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        // ---------------- 坐标 ----------------
        public Vector2 P(float nx, float ny)
        {
            Rect r = rectTransform.rect;
            return new Vector2(r.xMin + r.width * nx, r.yMin + r.height * ny);
        }

        public void Clear()
        {
            _tris.Clear();
            SetVerticesDirty();
        }

        // ---------------- 基础图元 ----------------
        void AddTri(Vector3 a, Vector3 b, Vector3 c, Color32 col)
        {
            _tris.Add(new Tri { p = a, c = col });
            _tris.Add(new Tri { p = b, c = col });
            _tris.Add(new Tri { p = c, c = col });
        }

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 col)
        {
            AddTri(a, b, c, col);
            AddTri(a, c, d, col);
        }

        public void Box(float nx0, float ny0, float nx1, float ny1, Color col)
        {
            Vector2 a = P(nx0, ny0), b = P(nx1, ny0), c = P(nx1, ny1), d = P(nx0, ny1);
            AddQuad(a, b, c, d, col);
        }

        public void Line(Vector2 a, Vector2 b, float width, Color col)
        {
            Vector2 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-dir.y, dir.x).normalized * (width * 0.5f);
            AddQuad(a - n, b - n, b + n, a + n, col);
        }

        public void NLine(float ax, float ay, float bx, float by, float width, Color col)
        {
            Line(P(ax, ay), P(bx, by), width, col);
        }

        public void Dashed(Vector2 a, Vector2 b, float width, Color col, float dash, float gap)
        {
            float len = Vector2.Distance(a, b);
            if (len < 0.01f) return;
            Vector2 dir = (b - a) / len;
            float t = 0f;
            while (t < len)
            {
                float e = Mathf.Min(t + dash, len);
                Line(a + dir * t, a + dir * e, width, col);
                t = e + gap;
            }
        }

        public void HDash(float ny, Color col, float width, float dash, float gap)
        {
            Dashed(P(0f, ny), P(1f, ny), width, col, dash, gap);
        }

        public void Dot(Vector2 c0, float radius, Color col, int seg = 14)
        {
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * 2f * i / seg;
                float a1 = Mathf.PI * 2f * (i + 1) / seg;
                AddTri(c0, c0 + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius,
                    c0 + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius, col);
            }
        }

        public void NDot(float nx, float ny, float radius, Color col, int seg = 14)
        {
            Dot(P(nx, ny), radius, col, seg);
        }

        /// <summary>圆弧（角度从 12 点方向顺时针为正）。</summary>
        public void Arc(Vector2 center, float radius, float thickness, float startDeg, float sweepDeg, Color col, int seg = 48)
        {
            float half = thickness * 0.5f;
            int steps = Mathf.Max(3, Mathf.RoundToInt(seg * Mathf.Abs(sweepDeg) / 360f));
            for (int i = 0; i < steps; i++)
            {
                float d0 = (startDeg + sweepDeg * i / steps) * Mathf.Deg2Rad;
                float d1 = (startDeg + sweepDeg * (i + 1) / steps) * Mathf.Deg2Rad;
                Vector2 v0 = new Vector2(Mathf.Sin(d0), Mathf.Cos(d0));
                Vector2 v1 = new Vector2(Mathf.Sin(d1), Mathf.Cos(d1));
                AddQuad(center + v0 * (radius - half), center + v0 * (radius + half),
                        center + v1 * (radius + half), center + v1 * (radius - half), col);
            }
        }

        public void Polyline(IList<Vector2> pts, float width, Color col)
        {
            for (int i = 0; i < pts.Count - 1; i++) Line(pts[i], pts[i + 1], width, col);
        }

        /// <summary>折线下方的半透明面积。</summary>
        public void AreaUnder(IList<Vector2> pts, float baselineY, Color col)
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                AddQuad(new Vector2(a.x, baselineY), new Vector2(b.x, baselineY), b, a, col);
            }
        }

        // ---------------- 网格与底框 ----------------
        public void GridLines(int cols, int rows, Color col, float width)
        {
            for (int i = 1; i < cols; i++) NLine((float)i / cols, 0f, (float)i / cols, 1f, width, col);
            for (int i = 1; i < rows; i++) NLine(0f, (float)i / rows, 1f, (float)i / rows, width, col);
        }

        public void FrameBox(Color col, float width)
        {
            NLine(0f, 0f, 1f, 0f, width, col);
            NLine(0f, 1f, 1f, 1f, width, col);
            NLine(0f, 0f, 0f, 1f, width, col);
            NLine(1f, 0f, 1f, 1f, width, col);
        }

        // ---------------- 提交网格 ----------------
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_tris.Count == 0) return;

            Color32 tint = color;
            for (int i = 0; i < _tris.Count; i++)
            {
                UIVertex v = UIVertex.simpleVert;
                v.position = _tris[i].p;
                Color32 c = _tris[i].c;
                v.color = new Color32(
                    (byte)(c.r * tint.r),
                    (byte)(c.g * tint.g),
                    (byte)(c.b * tint.b),
                    (byte)(c.a * tint.a));
                vh.AddVert(v);
            }

            for (int i = 0; i < _tris.Count; i += 3) vh.AddTriangle(i, i + 1, i + 2);
        }
    }
}
