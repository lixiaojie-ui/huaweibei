using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 订单滚动表格控制器。
/// 挂在表格面板上，运行后自动生成模拟订单并填充到 ScrollRect 的 Content 下。
/// 后续接真实数据：拿到数据列表后调用 Refresh(list) 即可，无需改动其他代码。
/// </summary>
public class OrderTableView : MonoBehaviour
{
    [Header("引用（需要在 Inspector 拖拽赋值）")]
    public OrderRow rowPrefab;          // OrderRow 行预制体
    public RectTransform content;       // ScrollRect 下的 Content

    [Header("模拟数据")]
    public int mockCount = 20;          // 启动时生成的模拟订单条数

    // 当前已生成的行，方便清理 / 后续扩展（刷新、复用等）
    readonly List<OrderRow> rows = new List<OrderRow>();

    void Start()
    {
        // 运行游戏自动生成多条模拟订单
        Refresh(OrderMockData.Generate(mockCount));
    }

    /// <summary>
    /// 刷新整个表格（先清空再重建）
    /// </summary>
    public void Refresh(List<OrderData> orders)
    {
        ClearRows();

        foreach (OrderData order in orders)
        {
            // 实例化到 Content 下，位置由 Vertical Layout Group 自动排布，无需手动设置坐标
            OrderRow row = Instantiate(rowPrefab, content);
            row.SetData(order);
            rows.Add(row);
        }
    }

    /// <summary>
    /// 清空当前所有行
    /// </summary>
    void ClearRows()
    {
        foreach (OrderRow row in rows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        rows.Clear();
    }
}
