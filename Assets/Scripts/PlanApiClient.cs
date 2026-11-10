using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace Daima
{
    // ------------------------------------------------------------------ API DTO
    // 字段名与 /api/dashboard 返回的 JSON 完全一致（JsonUtility 按字段名反序列化）。

    [Serializable]
    public class PlanApiRoot
    {
        public string generated_at;
        public PlanApiToday today;
        public PlanApiOrder[] orders;
        public PlanApiLineCapacity[] line_capacity;
        public PlanApiCapacitySummary capacity_summary;
        public PlanApiProcess[] process_progress;
        public PlanApiInventory[] inventory;
        public PlanApiAlert[] alerts;
    }

    [Serializable]
    public class PlanApiToday
    {
        public string date;
        public PlanApiKpi kpi;
        public PlanApiCapacityToday capacity;
        public PlanApiAgv agv;
    }

    [Serializable]
    public class PlanApiKpi
    {
        public int inventory_total;
        public int raw_qty;
        public int line_side_qty;
        public int finished_qty;
        public int inbound_qty;
    }

    [Serializable]
    public class PlanApiCapacityToday
    {
        public float effective_tons;
        public float actual_tons;
        public float load_rate;
        public string note;
    }

    [Serializable]
    public class PlanApiAgv
    {
        public int total_agvs;
        public int running_agvs;
        public float online_rate;
        public float avg_load_pct;
        public float task_execution_pct;
        public int tasks_executing;
        public int tasks_planned;
        public int tasks_completed;
    }

    [Serializable]
    public class PlanApiOrder
    {
        public string order_no;
        public string product_name;
        public int quantity;
        public string due_date;
        public int available_stock;
        public string status;
        public string warning;
    }

    [Serializable]
    public class PlanApiLineCapacity
    {
        public string stat_date;
        public string line_id;
        public string line_name;
        public float effective_tons;
        public float output_tons;
    }

    [Serializable]
    public class PlanApiCapacitySummary
    {
        public float load_rate;
        public float peak_tons;
        public float effective_tons;
    }

    [Serializable]
    public class PlanApiProcess
    {
        public string process_name;
        public int planned_batches;
        public int completed_batches;
        public float completion_rate;
    }

    [Serializable]
    public class PlanApiInventory
    {
        public string stat_date;
        public int raw_qty;
        public int line_side_qty;
        public int finished_qty;
        public int total_qty;
    }

    [Serializable]
    public class PlanApiAlert
    {
        public string alert_time;
        public string alert_type;
        public string severity;
        public string message;
        public string status;
    }

    /// <summary>一次 API 拉取 + 字段映射后的整屏数据。</summary>
    public class DashboardSnapshot
    {
        public List<OrderRowData> orders = new List<OrderRowData>();
        public List<LoadRowData> loads = new List<LoadRowData>();
        public List<RecipeRowData> recipes = new List<RecipeRowData>();
        public List<InventoryRowData> inventories = new List<InventoryRowData>();
        public AgvKpiData agv = new AgvKpiData();
        public LoadKpiData load = new LoadKpiData();
        public InventoryKpiData inventoryKpi = new InventoryKpiData();
        public string agvAlert = "";
        public string generatedAt = "";
        public int orderCount, loadDayCount, recipeCount, inventoryDayCount;
    }

    /// <summary>
    /// Canvas main1 数据库对接：GET /api/dashboard，字段一一映射到现有 UI 数据模型。
    /// 接口失败时回退到 Library 里的最近一次成功缓存，再失败才回退演示数据。
    /// </summary>
    public static class PlanApiClient
    {
        public const string Url = "http://10.176.98.147:8765/api/dashboard?days=7";

        static string CachePath
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/plan_dashboard_cache.json")); }
        }

        /// <summary>最近一次成功映射的快照（供菜单/日志使用）。</summary>
        public static DashboardSnapshot LastSnapshot { get; private set; }

        /// <summary>最近一次失败原因（成功时为空）。</summary>
        public static string LastError { get; private set; }

        public static DashboardSnapshot LoadSnapshot()
        {
            LastError = null;

            string json = Fetch();
            if (string.IsNullOrEmpty(json))
                json = ReadCache();

            if (string.IsNullOrEmpty(json))
            {
                LastError = "接口与本地缓存均不可用";
                LastSnapshot = null;
                return null;
            }

            try
            {
                PlanApiRoot root = JsonUtility.FromJson<PlanApiRoot>(json);
                if (root == null) throw new Exception("JSON 解析结果为空");
                DashboardSnapshot snap = Map(root);
                LastSnapshot = snap;
                return snap;
            }
            catch (Exception e)
            {
                LastError = "JSON 解析失败：" + e.Message;
                LastSnapshot = null;
                return null;
            }
        }

        static string Fetch()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "GET";
                request.Timeout = 8000;
                request.ReadWriteTimeout = 8000;
                request.UserAgent = "UnityDashboard/1.0";
                request.Proxy = null;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(json)) WriteCache(json);
                    return json;
                }
            }
            catch (Exception e)
            {
                LastError = "接口请求失败：" + e.Message;
                return null;
            }
        }

        static void WriteCache(string json)
        {
            try
            {
                string dir = Path.GetDirectoryName(CachePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(CachePath, json, new UTF8Encoding(false));
            }
            catch { /* 缓存失败不影响主流程 */ }
        }

        static string ReadCache()
        {
            try
            {
                if (File.Exists(CachePath))
                {
                    LastError = "接口不可用，已使用本地缓存：" + LastError;
                    return File.ReadAllText(CachePath, Encoding.UTF8);
                }
            }
            catch { }
            return null;
        }

        // -------------------------------------------------------------- 映射

        static DashboardSnapshot Map(PlanApiRoot root)
        {
            var snap = new DashboardSnapshot { generatedAt = root.generated_at ?? "" };

            MapOrders(root, snap);
            MapLoads(root, snap);
            MapRecipes(root, snap);
            MapInventory(root, snap);
            MapAgv(root, snap);

            snap.orderCount = snap.orders.Count;
            snap.loadDayCount = snap.loads.Count;
            snap.recipeCount = snap.recipes.Count;
            snap.inventoryDayCount = snap.inventories.Count;
            return snap;
        }

        static void MapOrders(PlanApiRoot root, DashboardSnapshot snap)
        {
            if (root.orders == null) return;

            foreach (PlanApiOrder o in root.orders)
            {
                if (o == null) continue;
                snap.orders.Add(new OrderRowData
                {
                    orderId = Safe(o.order_no, "—"),
                    model = Safe(o.product_name, "—"),
                    quantity = o.quantity,
                    delivery = FormatDate(o.due_date),
                    stock = o.available_stock,
                    status = NormalizeStatus(o.status),
                    warning = NormalizeWarning(o.warning)
                });
            }
        }

        static void MapLoads(PlanApiRoot root, DashboardSnapshot snap)
        {
            if (root.line_capacity == null || root.line_capacity.Length == 0) return;

            var dates = new List<string>();
            var map = new Dictionary<string, LoadRowData>();

            foreach (PlanApiLineCapacity row in root.line_capacity)
            {
                if (row == null || string.IsNullOrEmpty(row.stat_date)) continue;

                LoadRowData load;
                if (!map.TryGetValue(row.stat_date, out load))
                {
                    load = new LoadRowData { date = ShortDate(row.stat_date) };
                    map[row.stat_date] = load;
                    dates.Add(row.stat_date);
                }

                switch (LineKey(row))
                {
                    case "L1": load.line1 = Mathf.RoundToInt(row.output_tons); break;
                    case "L2": load.line2 = Mathf.RoundToInt(row.output_tons); break;
                    case "L3": load.line3 = Mathf.RoundToInt(row.output_tons); break;
                }
                load.capacity += Mathf.RoundToInt(row.effective_tons);
            }

            dates.Sort(StringComparer.Ordinal);
            foreach (string date in dates)
            {
                LoadRowData row = map[date];
                row.Refresh();
                snap.loads.Add(row);
            }

            PlanApiCapacitySummary summary = root.capacity_summary;
            if (summary != null)
            {
                snap.load.loadRate = summary.load_rate;
                snap.load.peak = summary.peak_tons;
            }
            snap.load.effectiveCapacity = root.today != null && root.today.capacity != null
                ? root.today.capacity.effective_tons
                : (snap.loads.Count > 0 ? snap.loads[0].capacity : 0f);

            if (snap.load.loadRate <= 0f && snap.loads.Count > 0)
            {
                int plan = 0, cap = 0;
                foreach (LoadRowData r in snap.loads) { plan += r.total; cap += r.capacity; }
                snap.load.loadRate = cap > 0 ? plan * 100f / cap : 0f;
            }
        }

        static string LineKey(PlanApiLineCapacity row)
        {
            string id = (row.line_id ?? "").ToUpperInvariant();
            if (id.Contains("GRAN-01")) return "L1";
            if (id.Contains("GRAN-02")) return "L2";
            if (id.Contains("EXP-01")) return "L3";

            string name = row.line_name ?? "";
            if (name.Contains("1号制粒")) return "L1";
            if (name.Contains("2号制粒")) return "L2";
            if (name.Contains("3号膨化")) return "L3";
            return "";
        }

        static void MapRecipes(PlanApiRoot root, DashboardSnapshot snap)
        {
            if (root.process_progress == null) return;

            foreach (PlanApiProcess p in root.process_progress)
            {
                if (p == null) continue;
                var row = new RecipeRowData
                {
                    process = Safe(p.process_name, "—"),
                    planned = p.planned_batches,
                    completed = p.completed_batches,
                    rate = p.completion_rate
                };
                row.Refresh();
                snap.recipes.Add(row);
            }
        }

        static void MapInventory(PlanApiRoot root, DashboardSnapshot snap)
        {
            if (root.inventory != null)
            {
                var rows = new List<PlanApiInventory>(root.inventory);
                rows.Sort((a, b) => string.CompareOrdinal(a.stat_date, b.stat_date));
                foreach (PlanApiInventory p in rows)
                {
                    if (p == null) continue;
                    snap.inventories.Add(new InventoryRowData
                    {
                        date = ShortDate(p.stat_date),
                        raw = p.raw_qty,
                        lineSide = p.line_side_qty,
                        finished = p.finished_qty
                    });
                }
            }

            PlanApiKpi kpi = root.today != null ? root.today.kpi : null;
            if (kpi != null)
            {
                snap.inventoryKpi.raw = kpi.raw_qty;
                snap.inventoryKpi.lineSide = kpi.line_side_qty;
                snap.inventoryKpi.finished = kpi.finished_qty;
                snap.inventoryKpi.total = kpi.inventory_total;
            }
            else if (snap.inventories.Count > 0)
            {
                InventoryRowData last = snap.inventories[snap.inventories.Count - 1];
                snap.inventoryKpi.raw = last.raw;
                snap.inventoryKpi.lineSide = last.lineSide;
                snap.inventoryKpi.finished = last.finished;
                snap.inventoryKpi.total = last.Total;
            }
        }

        static void MapAgv(PlanApiRoot root, DashboardSnapshot snap)
        {
            PlanApiAgv agv = root.today != null ? root.today.agv : null;
            if (agv != null)
            {
                snap.agv.running = agv.running_agvs;
                snap.agv.total = agv.total_agvs;
                snap.agv.avgLoad = agv.avg_load_pct;
                snap.agv.taskExec = agv.task_execution_pct;
                snap.agv.onlineRate = agv.online_rate;
                snap.agv.tasksExecuting = agv.tasks_executing;
            }

            snap.agvAlert = FindAgvAlert(root.alerts);
        }

        static string FindAgvAlert(PlanApiAlert[] alerts)
        {
            if (alerts == null) return "";

            PlanApiAlert best = null;
            foreach (PlanApiAlert a in alerts)
            {
                if (a == null) continue;
                string type = (a.alert_type ?? "").ToLowerInvariant();
                if (!type.Contains("agv")) continue;
                if (best == null || string.CompareOrdinal(a.alert_time, best.alert_time) > 0)
                    best = a;
            }

            return best != null ? Safe(best.message, "") : "";
        }

        // -------------------------------------------------------------- 字段清洗

        static string Safe(string value, string fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            string v = value.Trim();
            return v.Length == 0 ? fallback : v;
        }

        static string FormatDate(string raw)
        {
            DateTime date;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return Safe(raw, "—");
        }

        static string ShortDate(string iso)
        {
            string s = Safe(iso, "");
            return s.Length >= 10 ? s.Substring(5, 5) : s;
        }

        /// <summary>production→生产中、pending→待生产、completed→已完成；中文原样保留。</summary>
        public static string NormalizeStatus(string raw)
        {
            string s = Safe(raw, "");
            switch (s.ToLowerInvariant())
            {
                case "production":
                case "producing":
                case "running":
                    return "生产中";
                case "pending":
                    return "待生产";
                case "completed":
                case "done":
                    return "已完成";
                case "overdue":
                    return "已逾期";
            }
            if (s == "生产中" || s == "待生产" || s == "已完成" || s == "已逾期") return s;
            return s.Length > 0 ? s : "待生产";
        }

        /// <summary>HIGH→库存不足、MEDIUM→临近交货、LOW/正常→隐藏（空串）。</summary>
        public static string NormalizeWarning(string raw)
        {
            string s = Safe(raw, "");
            switch (s.ToLowerInvariant())
            {
                case "high":
                case "urgent":
                    return "库存不足";
                case "medium":
                case "mid":
                    return "临近交货";
                case "low":
                    return "";
            }
            if (s == "库存不足" || s == "临近交货") return s;
            if (s == "正常" || s == "—" || s == "-" || s == "无") return "";
            return s;
        }
    }
}
