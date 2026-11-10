using System;
using System.Collections.Generic;
using System.Globalization;

namespace Daima
{
    /// <summary>订单明细一行。</summary>
    public class OrderRowData
    {
        public string orderId;
        public string model;
        public int quantity;
        public string delivery;     // yyyy-MM-dd
        public int stock;
        public string status;       // 派生
        public string warning;      // 派生

        DateTime DeliveryDate()
        {
            DateTime d;
            if (DateTime.TryParseExact(delivery, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out d))
                return d;
            return DateTime.Today;
        }

        /// <summary>
        /// 按交货日期、库存和数量重新推导状态与预警：
        /// 交期已过且库存够 -> 已完成；交期已过且库存不足 -> 已逾期；
        /// 交期在 3 天内 -> 生产中；更远 -> 待生产。
        /// 未完成时：库存 &lt; 数量 -> 库存不足，否则 6 天内交货 -> 临近交货。
        /// </summary>
        public void Refresh()
        {
            int offset = (int)(DeliveryDate().Date - DateTime.Today).TotalDays;

            if (offset < 0) status = stock >= quantity ? "已完成" : "已逾期";
            else if (offset <= 3) status = "生产中";
            else status = "待生产";

            if (status == "已完成") warning = "";
            else if (stock < quantity) warning = "库存不足";
            else if (offset <= 6) warning = "临近交货";
            else warning = "";
        }
    }

    /// <summary>AGV 实时调度一行。</summary>
    public class AgvRowData
    {
        public string id;
        public string task;
        public string route;
        public string status;
        public string battery;      // 形如 92%
        public int doneToday;
    }

    /// <summary>各产线日计划产量与日产能缺口一行。</summary>
    public class LoadRowData
    {
        public string date;         // MM-dd
        public int line1;           // 1号制粒线，吨/日
        public int line2;           // 2号制粒线，吨/日
        public int line3;           // 3号膨化线，吨/日
        public int capacity;        // 日有效产能，吨/日
        public int total;           // 派生：三条线合计
        public int gap;             // 派生：超出产能的缺口
        public float rate;          // 派生
        public string status;       // 派生

        public string RateText { get { return rate.ToString("0.0") + "%"; } }

        public void Refresh()
        {
            total = line1 + line2 + line3;
            gap = total > capacity ? total - capacity : 0;
            rate = capacity > 0 ? total * 100f / capacity : 0f;
            status = gap > 0 ? "缺口" : (rate >= 90f ? "饱和" : "正常");
        }
    }

    /// <summary>各工序配方执行进度一行。</summary>
    public class RecipeRowData
    {
        public string process;      // 产线/工序
        public int planned;         // 计划批次
        public int completed;       // 已完成批次
        public float rate;          // 派生：完成进度
        public string status;       // 派生

        public string RateText { get { return rate.ToString("0.0") + "%"; } }

        public void Refresh()
        {
            rate = planned > 0 ? completed * 100f / planned : 0f;
            status = rate >= 100f ? "已完成" : (rate >= 80f ? "正常推进" : (rate >= 60f ? "偏慢" : "滞后"));
        }
    }

    /// <summary>物料库存趋势一行。</summary>
    public class InventoryRowData
    {
        public string date;         // MM-dd
        public int raw;
        public int lineSide;
        public int finished;

        public int Total { get { return raw + lineSide + finished; } }
    }

    /// <summary>AGV 实时调度 KPI（today.agv）。</summary>
    public class AgvKpiData
    {
        public int running;         // 在线车辆
        public int total;           // 车辆总数
        public float avgLoad;       // 平均负载 %
        public float taskExec;      // 配送达成率 %
        public float onlineRate;    // 车辆完好率 %
        public int tasksExecuting;  // 执行中任务
    }

    /// <summary>产线产能 KPI（capacity_summary + today.capacity）。</summary>
    public class LoadKpiData
    {
        public float loadRate;          // 综合负荷率 %
        public float peak;              // 峰值 t
        public float effectiveCapacity; // 日有效产能 t
    }

    /// <summary>物料库存 KPI（today.kpi）。</summary>
    public class InventoryKpiData
    {
        public int raw;
        public int lineSide;
        public int finished;
        public int total;
    }

    /// <summary>
    /// 演示数据。数值是编的，但彼此满足业务规则：
    /// 订单的库存/交期与状态、预警一致；负荷率 = 日计划产量 / 日有效产能；
    /// 完成率 = 已完成 / 计划任务；库存合计 = 原材料 + 线边库存 + 成品。
    /// </summary>
    public static class DashboardMockData
    {
        public static List<OrderRowData> CreateOrders()
        {
            var list = new List<OrderRowData>();

            // 型号, 数量, 距今天数(+ 未来 / - 已过), 库存
            AddOrder(list, "PLC-S7 控制模块", 60, 2, 86);
            AddOrder(list, "SEN-T100 温度传感器", 300, 5, 640);
            AddOrder(list, "VFD-7.5kW 变频器", 120, 9, 160);
            AddOrder(list, "HYD-24 液压阀块", 80, 9, 120);
            AddOrder(list, "MOT-X500 伺服电机", 200, 20, 320);
            AddOrder(list, "SNR-6203 深沟球轴承", 450, 22, 1500);
            AddOrder(list, "SNR-6203 深沟球轴承", 300, 26, 1500);
            AddOrder(list, "MOT-X500 伺服电机", 180, 29, 320);
            AddOrder(list, "PLC-S7 控制模块", 80, 31, 86);
            AddOrder(list, "VFD-7.5kW 变频器", 60, 34, 160);
            AddOrder(list, "HYD-24 液压阀块", 100, 37, 120);
            AddOrder(list, "SNR-6203 深沟球轴承", 600, 40, 500);
            AddOrder(list, "SEN-T100 温度传感器", 240, 12, 640);
            AddOrder(list, "PLC-S7 控制模块", 90, 15, 86);
            AddOrder(list, "MOT-X500 伺服电机", 150, 18, 320);
            AddOrder(list, "SNR-6203 深沟球轴承", 360, 1, 420);
            AddOrder(list, "SEN-T100 温度传感器", 210, 3, 180);
            AddOrder(list, "VFD-7.5kW 变频器", 90, -2, 160);
            AddOrder(list, "HYD-24 液压阀块", 130, -1, 100);
            AddOrder(list, "PLC-S7 控制模块", 70, -4, 86);

            // 按交期排序，越紧急越靠前
            list.Sort((a, b) => string.CompareOrdinal(a.delivery, b.delivery));

            for (int i = 0; i < list.Count; i++)
            {
                list[i].orderId = "SO-" + DateTime.Today.Year + "-" + (1008 + i);
                list[i].Refresh();
            }

            return list;
        }

        static void AddOrder(List<OrderRowData> list, string model, int quantity, int dayOffset, int stock)
        {
            list.Add(new OrderRowData
            {
                model = model,
                quantity = quantity,
                stock = stock,
                delivery = DateTime.Today.AddDays(dayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            });
        }

        public static List<AgvRowData> CreateAgvStatus()
        {
            return new List<AgvRowData>
            {
                new AgvRowData { id = "AGV-01", task = "配送料箱", route = "原料库 → 线边仓", status = "配送中", battery = "92%", doneToday = 14 },
                new AgvRowData { id = "AGV-02", task = "配送料箱", route = "线边仓 → 装配线", status = "配送中", battery = "76%", doneToday = 12 },
                new AgvRowData { id = "AGV-03", task = "空载返程", route = "成品区 → 线边仓", status = "避让中", battery = "88%", doneToday = 15 },
                new AgvRowData { id = "AGV-04", task = "充电",     route = "充电桩",        status = "充电中", battery = "34%", doneToday = 9 },
                new AgvRowData { id = "AGV-05", task = "配送托盘", route = "原料库 → 装配线", status = "配送中", battery = "81%", doneToday = 13 },
                new AgvRowData { id = "AGV-06", task = "待命",     route = "线边仓",        status = "待命",   battery = "95%", doneToday = 16 },
                new AgvRowData { id = "AGV-07", task = "配送料箱", route = "装配线 → 成品区", status = "配送中", battery = "67%", doneToday = 11 },
                new AgvRowData { id = "AGV-08", task = "故障维修", route = "维修区",        status = "离线",   battery = "12%", doneToday = 6 }
            };
        }

        public static List<LoadRowData> CreateLoad()
        {
            // 2026-10-26 至 2026-11-01，三条产线日计划产量（吨/日），
            // 日计划合计 1393 t，日有效产能 205 t，综合负荷率 97.1%，10-30 缺口 30 t。
            string[] dates = { "10-26", "10-27", "10-28", "10-29", "10-30", "10-31", "11-01" };
            int[] line1 = { 78, 82, 90, 90, 90, 88, 86 };
            int[] line2 = { 62, 64, 70, 70, 85, 68, 66 };
            int[] line3 = { 36, 38, 45, 45, 60, 40, 40 };

            var list = new List<LoadRowData>();
            for (int i = 0; i < dates.Length; i++)
            {
                var row = new LoadRowData
                {
                    date = dates[i],
                    line1 = line1[i],
                    line2 = line2[i],
                    line3 = line3[i],
                    capacity = 205
                };
                row.Refresh();
                list.Add(row);
            }

            return list;
        }

        public static List<RecipeRowData> CreateRecipeProgress()
        {
            // 各工序当日配方执行：计划批次 / 已完成批次 / 进度。
            // 合计计划 48 批，完成 36 批，综合执行率 75.0%。
            var list = new List<RecipeRowData>
            {
                new RecipeRowData { process = "1#配料线", planned = 10, completed = 8 },
                new RecipeRowData { process = "2#配料线", planned = 12, completed = 9 },
                new RecipeRowData { process = "1#发酵罐", planned = 8, completed = 5 },
                new RecipeRowData { process = "2#发酵罐", planned = 8, completed = 6 },
                new RecipeRowData { process = "1#包装线", planned = 10, completed = 8 }
            };

            foreach (var row in list) row.Refresh();
            return list;
        }

        public static List<InventoryRowData> CreateInventory()
        {
            // 原料缓慢上涨、线边库存略降、成品小幅回升
            int[] raw      = { 1180, 1215, 1190, 1230, 1205, 1228, 1242 };
            int[] lineSide = { 720, 705, 715, 698, 702, 694, 698 };
            int[] finished = { 486, 492, 505, 498, 512, 515, 518 };

            var list = new List<InventoryRowData>();
            for (int i = 0; i < raw.Length; i++)
            {
                list.Add(new InventoryRowData
                {
                    date = DateTime.Today.AddDays((i - 6) * 2).ToString("MM-dd", CultureInfo.InvariantCulture),
                    raw = raw[i],
                    lineSide = lineSide[i],
                    finished = finished[i]
                });
            }
            return list;
        }
    }
}
