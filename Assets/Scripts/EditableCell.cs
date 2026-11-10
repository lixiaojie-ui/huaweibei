using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 可点击编辑的单元格。
    /// 平时显示 Label（纯文字，不是图片）；单击后在原地覆盖一个 InputField 进行修改，
    /// 回车 / 点空白处 / 点其它单元格时提交，Esc 取消。
    /// 拖动（超过阈值或表格滚动了）不会误触发编辑，保证表格可以正常拖拽滚动。
    /// </summary>
    public class EditableCell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("子物体引用（由工厂赋值）")]
        public Text labelText;
        public InputField inputField;
        public Text inputText;
        public Image background;

        [NonSerialized] public bool readOnly;
        [NonSerialized] public Func<string, string> validator;          // 返回 null 表示输入非法、回退原值
        [NonSerialized] public Action<EditableCell, string> styler;     // 根据文字刷新颜色

        /// <summary>提交后回调（仅运行时由控制器挂接）。</summary>
        public event Action<string> Changed;

        string current = string.Empty;
        bool editing;
        bool pressed;
        Vector2 pressPosition;
        float pressScrollValue;
        ScrollRect parentScroll;

        public string Value { get { return current; } }

        public void Setup(Text label, InputField field, Text editText, Image bg)
        {
            labelText = label;
            inputField = field;
            inputText = editText;
            background = bg;

            if (labelText != null) labelText.raycastTarget = false;
            if (inputField != null)
            {
                inputField.onEndEdit.AddListener(HandleEndEdit);
                inputField.gameObject.SetActive(false);
            }
        }

        /// <summary>外部刷新显示值（不触发 Changed 回调，也不算一次用户编辑）。</summary>
        public void SetValue(string value)
        {
            current = value ?? string.Empty;
            if (labelText != null) labelText.text = current;
            if (inputText != null) inputText.text = current;
            if (inputField != null) inputField.SetTextWithoutNotify(current);
            if (styler != null) styler(this, current);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 先结束其它单元格的编辑状态（点只读单元格也要收起正在编辑的输入框）
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null)
            {
                var other = es.currentSelectedGameObject.GetComponentInParent<EditableCell>();
                if (other != null && other != this) es.SetSelectedGameObject(null);
            }

            if (readOnly) return;

            pressed = true;
            pressPosition = eventData.position;
            if (parentScroll == null) parentScroll = GetComponentInParent<ScrollRect>();
            pressScrollValue = parentScroll != null ? parentScroll.verticalNormalizedPosition : 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pressed) return;
            pressed = false;

            // 拖动过表格：不进入编辑，交给 ScrollRect 滚动
            if (Vector2.Distance(eventData.position, pressPosition) > 12f) return;
            if (parentScroll != null &&
                Mathf.Abs(parentScroll.verticalNormalizedPosition - pressScrollValue) > 0.01f) return;

            BeginEdit();
        }

        public void BeginEdit()
        {
            if (readOnly || editing || inputField == null) return;

            editing = true;
            if (labelText != null) labelText.gameObject.SetActive(false);

            inputField.gameObject.SetActive(true);
            inputField.SetTextWithoutNotify(current);
            inputField.Select();
            inputField.ActivateInputField();

            if (Application.isPlaying && isActiveAndEnabled) StartCoroutine(CaretToEnd());
        }

        IEnumerator CaretToEnd()
        {
            yield return null;
            if (editing && inputField != null && inputField.isFocused)
            {
                inputField.caretPosition = inputField.text.Length;
                inputField.ForceLabelUpdate();
            }
        }

        void HandleEndEdit(string raw)
        {
            if (!editing) return;
            editing = false;

            string final = raw;
            if (validator != null)
            {
                string valid = validator(raw);
                final = valid != null ? valid : current;   // 非法输入回退
            }

            SetValue(final);

            if (inputField != null) inputField.gameObject.SetActive(false);
            if (labelText != null) labelText.gameObject.SetActive(true);

            // 编辑结束后清掉选中，避免自动滚动误以为用户还在操作
            var es = EventSystem.current;
            if (es != null && inputField != null && es.currentSelectedGameObject == inputField.gameObject)
                es.SetSelectedGameObject(null);

            if (Changed != null) Changed(current);
        }
    }
}
