using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>一行表格的运行时引用。</summary>
    public class RowView
    {
        public RectTransform rect;
        public EditableCell[] cells;
    }

    /// <summary>一张表格的运行时引用。</summary>
    public class TableView
    {
        public RectTransform root;
        public ScrollRect scroll;
        public RectTransform content;
        public ColumnDef[] columns;
        public TableStyle style;
        public readonly List<RowView> rows = new List<RowView>();

        public EditableCell Cell(int row, int column)
        {
            return rows[row].cells[column];
        }
    }

    /// <summary>
    /// 用代码搭出 uGUI 表格：
    /// 表头 + 细分隔线 + ScrollRect(视口内是若干行)，每格都是可点击编辑的 EditableCell。
    /// 不使用任何背景图/精灵，只有文字、透明点击区和 1 像素分隔线。
    /// </summary>
    public static class DashboardTableFactory
    {
        public static TableView Create(RectTransform parent, string name, ColumnDef[] columns, int rowCount, TableStyle style)
        {
            var view = new TableView { columns = columns, style = style };

            var root = CreateRect(name, parent);
            view.root = root;
            Stretch(root, style.padding, style.padding, style.padding, style.padding);

            BuildHeader(root, columns, style);
            BuildDivider(root, style);
            BuildScroll(root, style, view);

            for (int r = 0; r < rowCount; r++)
                view.rows.Add(BuildRow(view.content, r, columns, style));

            return view;
        }

        static void BuildHeader(RectTransform root, ColumnDef[] columns, TableStyle style)
        {
            var header = CreateRect("Header", root);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, style.headerHeight);

            var layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 2f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;   // 宽度按 flexibleWidth 权重分配
            layout.childForceExpandHeight = true;

            for (int c = 0; c < columns.Length; c++)
            {
                ColumnDef col = columns[c];
                var cell = CreateRect("H_" + col.title, header);
                var element = cell.gameObject.AddComponent<LayoutElement>();
                element.flexibleWidth = col.flex;
                element.preferredWidth = 0f;
                element.minWidth = 0f;

                Text title = CreateText("Text", cell, style.font, style.fontSize, style.header, col.align, FontStyle.Bold);
                Stretch(title.rectTransform, 4f, 0f, 4f, 0f);
                title.text = col.title;
            }
        }

        static void BuildDivider(RectTransform root, TableStyle style)
        {
            var line = CreateRect("HeaderLine", root);
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.anchoredPosition = new Vector2(0f, -style.headerHeight);
            line.sizeDelta = new Vector2(0f, 1f);

            var image = line.gameObject.AddComponent<Image>();
            image.color = style.grid;
            image.raycastTarget = false;
        }

        static void BuildScroll(RectTransform root, TableStyle style, TableView view)
        {
            var scrollRT = CreateRect("Scroll", root);
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = new Vector2(0f, -style.headerHeight - 1f);

            var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();

            var viewport = CreateRect("Viewport", scrollRT);
            Stretch(viewport, 0f, 0f, 0f, 0f);

            // 透明点击层：让空白处也能拖动/滚轮滚动，但不画任何东西
            var surface = viewport.gameObject.AddComponent<Image>();
            surface.color = Color.clear;
            surface.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 32f;
            scroll.elasticity = 0.1f;

            view.scroll = scroll;
            view.content = content;
        }

        static RowView BuildRow(RectTransform content, int index, ColumnDef[] columns, TableStyle style)
        {
            var rowRT = CreateRect("Row_" + (index + 1).ToString("00"), content);

            var element = rowRT.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = style.rowHeight;
            element.minHeight = style.rowHeight;
            element.flexibleHeight = 0f;

            var layout = rowRT.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 2f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;   // 宽度按 flexibleWidth 权重分配
            layout.childForceExpandHeight = true;

            var row = new RowView { rect = rowRT, cells = new EditableCell[columns.Length] };
            for (int c = 0; c < columns.Length; c++)
                row.cells[c] = BuildCell(rowRT, columns[c], style);

            // 行底 1 像素分隔线（不参与横向排版）
            var line = CreateRect("Line", rowRT);
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.anchoredPosition = Vector2.zero;
            line.sizeDelta = new Vector2(0f, 1f);
            var lineElement = line.gameObject.AddComponent<LayoutElement>();
            lineElement.ignoreLayout = true;
            var lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = style.grid;
            lineImage.raycastTarget = false;

            return row;
        }

        static EditableCell BuildCell(RectTransform row, ColumnDef column, TableStyle style)
        {
            var cellRT = CreateRect("C_" + column.title, row);

            var element = cellRT.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = column.flex;
            element.preferredWidth = 0f;
            element.minWidth = 0f;

            // 透明点击区（不是背景图），同时用于状态色块
            var background = cellRT.gameObject.AddComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = true;

            var label = CreateText("Label", cellRT, style.font, style.fontSize, style.text, column.align, FontStyle.Normal);
            Stretch(label.rectTransform, 4f, 1f, 4f, 1f);

            // 编辑时覆盖在单元格上的输入框（默认隐藏）
            var editorRT = CreateRect("Editor", cellRT);
            Stretch(editorRT, 0f, 0f, 0f, 0f);

            var editorBackground = editorRT.gameObject.AddComponent<Image>();
            editorBackground.color = style.editBackground;
            editorBackground.raycastTarget = true;

            var input = editorRT.gameObject.AddComponent<InputField>();
            input.targetGraphic = editorBackground;
            input.transition = Selectable.Transition.None;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Standard;
            input.customCaretColor = true;
            input.caretColor = style.accent;
            input.caretWidth = 1;
            input.selectionColor = new Color(0.30f, 0.65f, 1f, 0.35f);
            input.navigation = new Navigation { mode = Navigation.Mode.None };

            Text inputText = CreateText("Text", editorRT, style.font, style.fontSize, style.text, column.align, FontStyle.Normal);
            Stretch(inputText.rectTransform, 4f, 1f, 4f, 1f);
            inputText.supportRichText = false;

            input.textComponent = inputText;
            input.text = string.Empty;
            editorRT.gameObject.SetActive(false);

            var cell = cellRT.gameObject.AddComponent<EditableCell>();
            cell.Setup(label, input, inputText, background);
            return cell;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Text CreateText(string name, Transform parent, Font font, int size, Color color,
                               TextAnchor align, FontStyle fontStyle)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = fontStyle;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            return text;
        }

        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
