using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>HTML 图表 tooltip 的 uGUI 复刻：深色圆角框、标题、左右对齐的行。</summary>
    [AddComponentMenu("Main5/Tooltip")]
    public class Main5Tooltip : MonoBehaviour
    {
        public RectTransform root;
        public RectTransform canvasRect;
        public UIRoundRect background;
        public Text title;
        public Text[] rowLabels = new Text[0];
        public Text[] rowValues = new Text[0];
        public float uiScale = 1f;

        const float PadX = 11f;
        const float PadY = 8f;
        const float TitleHeight = 16f;
        const float TitleGap = 4f;
        const float RowHeight = 19f;
        const float Gap = 18f;

        public void Show(string titleText, string[] labels, string[] values, string[] valueColors, Vector2 screenPosition)
        {
            if (root == null) return;

            root.gameObject.SetActive(true);
            root.SetAsLastSibling();

            if (title != null)
            {
                title.text = titleText;
                title.gameObject.SetActive(!string.IsNullOrEmpty(titleText));
            }

            int count = Mathf.Min(labels != null ? labels.Length : 0, rowLabels.Length);
            float maxRowWidth = 0f;

            float padX = PadX * uiScale;
            float padY = PadY * uiScale;
            float titleH = TitleHeight * uiScale;
            float titleGap = TitleGap * uiScale;
            float rowH = RowHeight * uiScale;
            float gap = Gap * uiScale;

            for (int i = 0; i < rowLabels.Length; i++)
            {
                bool on = i < count;
                if (rowLabels[i] != null) rowLabels[i].gameObject.SetActive(on);
                if (rowValues[i] != null) rowValues[i].gameObject.SetActive(on);
                if (!on) continue;

                rowLabels[i].text = labels[i];
                rowValues[i].text = values[i];

                Color c = ParseColor(valueColors != null && i < valueColors.Length ? valueColors[i] : "#eaf6ff");
                rowValues[i].color = c;

                float lw = rowLabels[i].preferredWidth;
                float vw = rowValues[i].preferredWidth;
                maxRowWidth = Mathf.Max(maxRowWidth, lw + gap + vw);
            }

            float titleWidth = title != null && title.gameObject.activeSelf ? title.preferredWidth : 0f;
            float width = Mathf.Max(96f * uiScale, padX * 2f + Mathf.Max(maxRowWidth, titleWidth));
            float height = padY * 2f + (title != null && title.gameObject.activeSelf ? titleH + titleGap : 0f)
                           + count * rowH;

            root.sizeDelta = new Vector2(width, height);
            if (background != null)
            {
                background.rectTransform.sizeDelta = new Vector2(width, height);
                background.Refresh();
            }

            if (title != null)
            {
                SetRect(title.rectTransform, padX, padY, width - padX * 2f, titleH);
            }

            for (int i = 0; i < count; i++)
            {
                float top = padY + (title != null && title.gameObject.activeSelf ? titleH + titleGap : 0f) + i * rowH;
                float valueWidth = rowValues[i].preferredWidth;
                float labelWidth = Mathf.Max(0f, width - padX * 2f - gap - valueWidth);

                SetRect(rowLabels[i].rectTransform, padX, top, labelWidth, rowH);
                SetRect(rowValues[i].rectTransform, width - padX - valueWidth, top, valueWidth, rowH);
            }

            Vector2 local;
            if (canvasRect == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out local))
            {
                return;
            }

            Vector2 half = canvasRect.rect.size * 0.5f;
            Vector2 pos = local + new Vector2(14f * uiScale, -14f * uiScale);
            pos.x = Mathf.Clamp(pos.x, -half.x + 4f, half.x - width - 4f);
            pos.y = Mathf.Clamp(pos.y, -half.y + height + 4f, half.y - 4f);
            root.anchoredPosition = pos;
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        static void SetRect(RectTransform rt, float x, float top, float w, float h)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -top);
        }

        static Color ParseColor(string hex)
        {
            Color c;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c)) return c;
            return Color.white;
        }
    }
}
