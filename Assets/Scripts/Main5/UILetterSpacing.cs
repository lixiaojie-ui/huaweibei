using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>给 Legacy Text 增加字间距（逐字平移顶点，可选保持居中）。</summary>
    [AddComponentMenu("UI/Main5 Letter Spacing")]
    public class UILetterSpacing : BaseMeshEffect
    {
        public float spacing = 0f;
        public bool center = true;

        public void SetSpacing(float value, bool keepCenter)
        {
            spacing = value;
            center = keepCenter;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0 || Mathf.Approximately(spacing, 0f)) return;

            int chars = vh.currentVertCount / 4;
            if (chars <= 0) return;

            float offset = center ? -(chars - 1) * spacing * 0.5f : 0f;
            UIVertex v = new UIVertex();

            for (int i = 0; i < chars; i++)
            {
                float dx = offset + i * spacing;
                if (Mathf.Approximately(dx, 0f)) continue;

                for (int k = 0; k < 4; k++)
                {
                    int index = i * 4 + k;
                    if (index >= vh.currentVertCount) break;
                    vh.PopulateUIVertex(ref v, index);
                    v.position += new Vector3(dx, 0f, 0f);
                    vh.SetUIVertex(v, index);
                }
            }
        }
    }
}
