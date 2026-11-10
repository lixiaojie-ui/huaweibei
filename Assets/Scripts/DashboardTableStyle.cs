using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 单元格的数据职责，决定校验方式和配色规则。
    /// </summary>
    public enum CellRole
    {
        Text,     // 普通文本
        Number,   // 数值（居中）
        Date,     // 日期（yyyy-MM-dd）
        Status,   // 状态（按文字自动配色）
        Warning,  // 预警（库存不足 / 临近交货）
        Battery,  // 电量（按百分比配色）
        Rate      // 完成率 / 负载率
    }

    /// <summary>
    /// 一列的定义：表头文字、宽度权重、单元格职责。
    /// 宽度用 flex 权重分配，表格会跟随 Image1~Image5 的尺寸自适应。
    /// </summary>
    public class ColumnDef
    {
        public readonly string title;
        public readonly float flex;
        public readonly CellRole role;
        public readonly TextAnchor align;

        public ColumnDef(string title, float flex, CellRole role)
        {
            this.title = title;
            this.flex = flex;
            this.role = role;

            // 文本/日期靠左，数值和状态居中
            bool left = role == CellRole.Text || role == CellRole.Date;
            align = left ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
        }
    }

    /// <summary>
    /// 表格整体样式（深色大屏风格，无背景图，只用文字和细分隔线）。
    /// </summary>
    public class TableStyle
    {
        public Font font;
        public int fontSize = 15;
        public float rowHeight = 32f;
        public float headerHeight = 30f;
        public float padding = 12f;

        // ---- 深蓝玻璃主题（与背景同色系，浅色文字）----
        // 面板底：深蓝半透明，贴近背景色，不再发白
        public static readonly Color PanelFill = new Color(0.055f, 0.180f, 0.300f, 0.23921569f);
        // 青色描边
        public static readonly Color PanelEdge = new Color(0.25f, 0.75f, 0.95f, 0.75f);
        // 顶部 1px 高光线
        public static readonly Color PanelHighlight = new Color(0.75f, 0.95f, 1f, 0.45f);

        public readonly Color text = new Color(0.905f, 0.950f, 0.980f, 1f);   // #E7F2FA
        public readonly Color header = new Color(0.550f, 0.850f, 1.000f, 1f); // #8CD9FF
        public readonly Color grid = new Color(0.700f, 0.880f, 1.000f, 0.10f);
        public readonly Color muted = new Color(0.620f, 0.740f, 0.850f, 1f);
        public readonly Color accent = new Color(0.240f, 0.850f, 1.000f, 1f); // #3CD9FF
        public readonly Color editBackground = new Color(0.050f, 0.140f, 0.240f, 0.98f);

        public static readonly Color Success = new Color(0.240f, 0.870f, 0.550f, 1f);
        public static readonly Color Info = new Color(0.300f, 0.660f, 1.000f, 1f);
        public static readonly Color Warn = new Color(0.980f, 0.700f, 0.200f, 1f);
        public static readonly Color Danger = new Color(1.000f, 0.420f, 0.420f, 1f);
        public static readonly Color MutedColor = new Color(0.620f, 0.720f, 0.820f, 1f);

        /// <summary>
        /// 状态文字 -> 颜色（先匹配异常，再匹配进行中，最后匹配正常）。
        /// </summary>
        public static Color StatusColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return MutedColor;

            if (value.Contains("逾期") || value.Contains("超载") ||
                value.Contains("离线") || value.Contains("故障"))
                return Danger;

            if (value.Contains("待生产") ||
                value.Contains("充电") ||
                value.Contains("饱和") || value.Contains("滞后") || value.Contains("避让"))
                return Warn;

            if (value.Contains("生产中") || value.Contains("配送中") || value.Contains("进行中"))
                return Info;

            if (value.Contains("已完成") || value.Contains("待命") ||
                value.Contains("正常") || value.Contains("在线"))
                return Success;

            return MutedColor;
        }

        /// <summary>
        /// 预警文字 -> 颜色。
        /// </summary>
        public static Color WarningColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return MutedColor;
            if (value.Contains("库存不足")) return Danger;
            if (value.Contains("临近交货")) return Warn;
            return MutedColor;
        }
    }
}
