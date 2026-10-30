using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 订单状态枚举
/// </summary>
public enum OrderStatus
{
    InProduction,   // 生产中
    Pending,        // 待生产
    Completed,      // 已完成
    Overdue         // 已逾期
}

/// <summary>
/// 预警类型枚举（None 表示无预警，对应标签直接隐藏）
/// </summary>
public enum OrderWarningType
{
    None,           // 无预警
    StockLow,       // 库存不足（红）
    DeliveryNear    // 临近交货（橙）
}

/// <summary>
/// 订单数据模型（纯数据类，不继承 MonoBehaviour，可放心序列化）
/// </summary>
[Serializable]
public class OrderData
{
    public string orderId;              // 订单号
    public string productModel;         // 产品型号
    public int quantity;                // 数量
    public string deliveryTime;         // 交货时间（字符串形式，方便后续换成 DateTime）
    public int stock;                   // 库存
    public OrderStatus status;          // 状态
    public OrderWarningType warningType;// 预警类型
    public string warning;              // 预警文本（为空时界面隐藏预警标签）

    /// <summary>
    /// 状态对应的中文显示文本
    /// </summary>
    public string GetStatusText()
    {
        switch (status)
        {
            case OrderStatus.InProduction: return "生产中";
            case OrderStatus.Pending:      return "待生产";
            case OrderStatus.Completed:    return "已完成";
            case OrderStatus.Overdue:      return "已逾期";
            default:                       return "未知";
        }
    }
}

/// <summary>
/// 模拟订单数据生成器。
/// 后续接真实数据时不需要改这里，直接调用 OrderTableView.Refresh(真实列表) 即可。
/// </summary>
public static class OrderMockData
{
    // 可选的产品型号池
    static readonly string[] Models =
    {
        "DT-Sensor-A1", "DT-Motor-B2", "DT-Camera-C3",
        "DT-Gateway-D4", "DT-Robot-E5", "DT-PLC-F6"
    };

    /// <summary>
    /// 随机生成 count 条订单数据
    /// </summary>
    public static List<OrderData> Generate(int count)
    {
        List<OrderData> list = new List<OrderData>(count);
        System.Random rng = new System.Random(Guid.NewGuid().GetHashCode());

        for (int i = 0; i < count; i++)
        {
            // 随机状态
            OrderStatus status = (OrderStatus)rng.Next(0, 4);

            // 随机预警：约一半订单无预警
            OrderWarningType warningType = (OrderWarningType)rng.Next(0, 4); // 0~3，0/3 都算无预警概率更高
            if (warningType == OrderWarningType.None || (int)warningType > 2)
                warningType = OrderWarningType.None;

            int stock = rng.Next(0, 500);
            int quantity = rng.Next(10, 1000);

            // 业务规则示例：库存小于订单数量的 20% 时强制库存不足预警
            if (stock < quantity * 0.2f)
                warningType = OrderWarningType.StockLow;

            OrderData data = new OrderData
            {
                orderId = "SO" + DateTime.Now.ToString("yyyyMMdd") + (1000 + i),
                productModel = Models[rng.Next(Models.Length)],
                quantity = quantity,
                deliveryTime = DateTime.Now.AddDays(rng.Next(-5, 30)).ToString("yyyy-MM-dd"),
                stock = stock,
                status = status,
                warningType = warningType,
                warning = GetWarningText(warningType)
            };
            list.Add(data);
        }
        return list;
    }

    /// <summary>
    /// 预警类型对应的显示文本
    /// </summary>
    static string GetWarningText(OrderWarningType type)
    {
        switch (type)
        {
            case OrderWarningType.StockLow:     return "库存不足";
            case OrderWarningType.DeliveryNear: return "临近交货";
            default:                            return "";   // 无预警返回空串
        }
    }
}
