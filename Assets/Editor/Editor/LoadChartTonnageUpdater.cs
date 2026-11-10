using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Daima;

namespace Daima.EditorTools
{
    /// <summary>
    /// 将 Canvas main1 / Canvas right / Image3 的负荷图更新为
    /// “各产线日计划产量与日产能缺口（吨）”。
    /// 只重建图表，不改标题，避免覆盖手动修改的标题。
    /// 菜单：计划调度/更新右1产线负荷图（吨/日）
    /// </summary>
    public static class LoadChartTonnageUpdater
    {
        [MenuItem("计划调度/更新右1产线负荷图（吨/日）", false, 20)]
        static void ApplyMenu()
        {
            Apply(true);
        }

        static void Apply(bool interactive)
        {
            RectTransform image3 = DashboardTablesMenu.FindRect("Image3", "Canvas right");
            if (image3 == null)
            {
                Debug.LogWarning("[LoadChart] 未找到 Canvas main1 / Canvas right / Image3，跳过更新。");
                return;
            }

            Scene scene = image3.gameObject.scene;
            bool wasDirty = scene.isDirty;

            // 只清掉 Image3 里旧的负荷图根节点，保留 Image3 本身与 right-frame1
            for (int i = image3.childCount - 1; i >= 0; i--)
            {
                Transform child = image3.GetChild(i);
                if (child.name == "__Table_订单负荷与产能匹配" ||
                    child.name == "订单负荷与产能匹配" ||
                    child.name == "__Table_产线日计划与产能缺口")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            Font font = DashboardTablesController.LoadBuiltinFont();
            RectTransform root = DashboardChartFactory.BuildLoadChart(
                image3, DashboardMockData.CreateLoad(), font);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "更新产线负荷图");

            EditorSceneManager.MarkSceneDirty(scene);

            if (!wasDirty)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[LoadChart] 已更新 Canvas main1 / Canvas right / Image3 产线负荷图，并保存场景。");
            }
            else
            {
                Debug.Log("[LoadChart] 已更新 Canvas main1 / Canvas right / Image3 产线负荷图；场景原有未保存修改，请按 Ctrl+S 保存。");
            }

            if (interactive) Selection.activeGameObject = image3.gameObject;
        }
    }
}
