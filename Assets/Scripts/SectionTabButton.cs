using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>main5 右侧分段按钮的选中态样式。</summary>
    public class SectionTabButton : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Image accent;
        public Text label;
        public Text number;

        public Color selectedBg = new Color(0.075f, 0.263f, 0.360f, 0.72f);
        public Color normalBg = new Color(0.027f, 0.086f, 0.145f, 0.58f);
        public Color selectedAccent = new Color(0.235f, 0.851f, 1f, 1f);
        public Color normalAccent = new Color(0.235f, 0.851f, 1f, 0.20f);
        public Color selectedText = new Color(0.890f, 0.965f, 0.996f, 1f);
        public Color normalText = new Color(0.486f, 0.576f, 0.647f, 1f);
        public Color selectedNumber = new Color(0.235f, 0.851f, 1f, 0.95f);
        public Color normalNumber = new Color(0.235f, 0.851f, 1f, 0.50f);

        public void SetSelected(bool selected)
        {
            if (background != null) background.color = selected ? selectedBg : normalBg;
            if (accent != null) accent.color = selected ? selectedAccent : normalAccent;
            if (label != null) label.color = selected ? selectedText : normalText;
            if (number != null) number.color = selected ? selectedNumber : normalNumber;
        }
    }
}
