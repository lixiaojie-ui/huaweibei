using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 订单表格单行视图。
/// 挂在 OrderRow 预制体根节点上，负责把一条 OrderData 填充到 UI。
/// </summary>
public class OrderRow : MonoBehaviour
{
    [Header("普通文本列")]
    public TMP_Text orderIdText;    // 订单号
    public TMP_Text modelText;      // 产品型号
    public TMP_Text quantityText;   // 数量
    public TMP_Text deliveryText;   // 交货时间
    public TMP_Text stockText;      // 库存

    [Header("状态标签")]
    public TMP_Text statusText;         // 状态文字
    public Image statusBackground;      // 状态标签背景图

    [Header("预警标签")]
    public GameObject warningLabel;     // 预警标签整体（容器下的 Label 子物体，隐藏它而不是隐藏整列，保证列宽不错位）
    public TMP_Text warningText;        // 预警文字
    public Image warningBackground;     // 预警标签背景图

    [Header("状态标签颜色")]
    public Color inProductionColor = new Color(0.20f, 0.47f, 0.90f); // 生产中-蓝
    public Color pendingColor      = new Color(0.95f, 0.60f, 0.10f); // 待生产-橙
    public Color completedColor    = new Color(0.25f, 0.70f, 0.35f); // 已完成-绿
    public Color overdueColor      = new Color(0.90f, 0.28f, 0.28f); // 已逾期-红

    [Header("预警标签颜色")]
    public Color stockLowColor     = new Color(0.90f, 0.28f, 0.28f); // 库存不足-红
    public Color deliveryNearColor = new Color(0.95f, 0.60f, 0.10f); // 临近交货-橙

    /// <summary>
    /// 填充一行数据
    /// </summary>
    public void SetData(OrderData data)
    {
        // 普通文本列
        orderIdText.text  = data.orderId;
        modelText.text    = data.productModel;
        quantityText.text = data.quantity.ToString();
        deliveryText.text = data.deliveryTime;
        stockText.text    = data.stock.ToString();

        // 状态标签：文字 + 背景色
        statusText.text = data.GetStatusText();
        statusBackground.color = GetStatusColor(data.status);

        // 预警标签：内容为空时整体隐藏（只隐藏 Label 子物体，列容器保留，列宽不变）
        if (data.warningType == OrderWarningType.None || string.IsNullOrEmpty(data.warning))
        {
            warningLabel.SetActive(false);
        }
        else
        {
            warningLabel.SetActive(true);
            warningText.text = data.warning;
            warningBackground.color = GetWarningColor(data.warningType);
        }
    }

    /// <summary>
    /// 状态 -> 背景色映射
    /// </summary>
    Color GetStatusColor(OrderStatus status)
    {
        switch (status)
        {
            case OrderStatus.InProduction: return inProductionColor;
            case OrderStatus.Pending:      return pendingColor;
            case OrderStatus.Completed:    return completedColor;
            case OrderStatus.Overdue:      return overdueColor;
            default:                       return Color.gray;
        }
    }

    /// <summary>
    /// 预警类型 -> 背景色映射
    /// </summary>
    Color GetWarningColor(OrderWarningType type)
    {
        switch (type)
        {
            case OrderWarningType.StockLow:     return stockLowColor;
            case OrderWarningType.DeliveryNear: return deliveryNearColor;
            default:                            return Color.gray;
        }
    }
}
