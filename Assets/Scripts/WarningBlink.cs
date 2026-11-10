using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 预警文字红色闪烁：仅当单元格文字为“库存不足”时生效，
    /// “临近交货”等其他预警保持常亮，不改变原有配色。
    /// </summary>
    [DisallowMultipleComponent]
    public class WarningBlink : MonoBehaviour
    {
        public Text label;

        public Color bright = new Color(1f, 0.42f, 0.42f, 1f);
        public Color dim = new Color(1f, 0.42f, 0.42f, 0.25f);
        public float speed = 5f;

        bool wasDanger;

        void OnDisable()
        {
            wasDanger = false;
        }

        void Update()
        {
            if (label == null) return;

            bool danger = !string.IsNullOrEmpty(label.text) && label.text.Contains("库存不足");
            if (!danger)
            {
                // 由单元格样式负责恢复普通预警/空值的颜色
                wasDanger = false;
                return;
            }

            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
            label.color = Color.Lerp(dim, bright, t);
            wasDanger = true;
        }
    }
}
