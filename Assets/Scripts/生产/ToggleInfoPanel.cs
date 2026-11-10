using UnityEngine;
using UnityEngine.UI;

public class ToggleInfoPanel : MonoBehaviour
{
    public GameObject panel;   // 拖入 Info Panel

    public void Toggle()
    {
        if (panel == null)
        {
            Debug.LogWarning("[ToggleInfoPanel] panel 没有绑定，点击无效。");
            return;
        }

        bool show = !panel.activeSelf;
        panel.SetActive(show);
        Debug.Log("[ToggleInfoPanel] '" + panel.name + "' → " + (show ? "显示" : "隐藏") +
            " activeSelf=" + panel.activeSelf +
            " activeInHierarchy=" + panel.activeInHierarchy);

        if (show)
        {
            Transform parent = panel.transform.parent;
            while (parent != null)
            {
                Canvas parentCanvas = parent.GetComponent<Canvas>();
                Debug.Log("[ToggleInfoPanel] 父级 '" + parent.name +
                    "' activeSelf=" + parent.gameObject.activeSelf +
                    " activeInHierarchy=" + parent.gameObject.activeInHierarchy +
                    (parentCanvas != null ? " Canvas(" + parentCanvas.renderMode + ")" : ""));
                parent = parent.parent;
            }

            Canvas canvas = panel.GetComponentInParent<Canvas>(true);
            Debug.Log("[ToggleInfoPanel] 所属Canvas=" +
                (canvas != null
                    ? canvas.name + "(" + canvas.renderMode + ", activeInHierarchy=" + canvas.gameObject.activeInHierarchy + ")"
                    : "null"));
        }

        if (!show) return;

        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
        {
            Debug.Log("[ToggleInfoPanel] 子图形 '" + graphic.name +
                "' activeInHierarchy=" + graphic.gameObject.activeInHierarchy +
                " color=" + graphic.color +
                " worldPos=" + graphic.rectTransform.position.ToString("F1") +
                " rectSize=" + graphic.rectTransform.rect.size);
        }

        foreach (TMPro.TMP_Text text in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            Debug.Log("[ToggleInfoPanel] TMP文字 '" + text.name + "' 内容='" + text.text + "'");
        }
    }
}
