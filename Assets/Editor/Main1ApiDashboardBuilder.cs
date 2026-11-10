using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Daima
{
    /// <summary>
    /// 从数据库接口刷新 Canvas main1：订单明细 / 各产线日产能缺口 / 各工序配方进度 /
    /// AGV 实时调度 / 物料库存趋势。生成后立即保存场景。
    /// </summary>
    public static class Main1ApiDashboardBuilder
    {
        [MenuItem("大屏/刷新 main1 数据库数据")]
        public static void RefreshMain1()
        {
            DashboardTablesController controller = FindController();
            if (controller == null)
            {
                Debug.LogError("[main1] 场景里没有找到 DashboardTablesController（__DashboardTables）。");
                return;
            }

            controller.BuildAll();

            UnityEngine.SceneManagement.Scene scene = controller.gameObject.scene;
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            DashboardSnapshot snapshot = PlanApiClient.LastSnapshot;
            if (snapshot != null)
            {
                Debug.Log(string.Format(
                    "[main1] 刷新完成：订单 {0} 条 · 产线 {1} 天 · 工序 {2} 条 · 库存 {3} 天 · 生成时间 {4}",
                    snapshot.orderCount, snapshot.loadDayCount, snapshot.recipeCount,
                    snapshot.inventoryDayCount, snapshot.generatedAt));
            }
            else
            {
                Debug.LogWarning("[main1] 刷新完成，但数据库接口不可用（" + PlanApiClient.LastError +
                                 "），当前为演示数据。");
            }
        }

        static DashboardTablesController FindController()
        {
            DashboardTablesController[] all = Resources.FindObjectsOfTypeAll<DashboardTablesController>();
            foreach (DashboardTablesController c in all)
            {
                if (c == null) continue;
                if (!c.gameObject.scene.IsValid()) continue; // 排除预制体资源
                return c;
            }

            // 兜底：直接在当前场景根节点 / 对象名里找
            UnityEngine.SceneManagement.Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.IsValid())
            {
                foreach (GameObject root in active.GetRootGameObjects())
                {
                    DashboardTablesController c = root.GetComponentInChildren<DashboardTablesController>(true);
                    if (c != null) return c;
                }
            }

            GameObject go = GameObject.Find("__DashboardTables");
            return go != null ? go.GetComponent<DashboardTablesController>() : null;
        }
    }
}
