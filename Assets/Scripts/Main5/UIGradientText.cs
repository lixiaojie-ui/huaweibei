using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>按文字矩形高度做三段竖直渐变（用于大屏主标题）。</summary>
    [AddComponentMenu("UI/Main5 Gradient Text")]
    public class UIGradientText : BaseMeshEffect
    {
        public Color top = Color.white;
        public Color middle = Color.white;
        public Color bottom = Color.white;

        [Range(0.01f, 0.99f)] public float middleAt = 0.52f;

        public void SetColors(Color bottomColor, Color midColor, Color topColor, float mid)
        {
            bottom = bottomColor;
            middle = midColor;
            top = topColor;
            middleAt = mid;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            Rect rect = graphic.rectTransform.rect;
            if (rect.height <= 0.01f) return;

            UIVertex v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);

                float t = Mathf.InverseLerp(rect.yMin, rect.yMax, v.position.y);
                Color g = t < middleAt
                    ? Color.Lerp(bottom, middle, t / middleAt)
                    : Color.Lerp(middle, top, (t - middleAt) / (1f - middleAt));

                v.color = new Color(v.color.r * g.r, v.color.g * g.g,
                                    v.color.b * g.b, v.color.a * g.a);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
