using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Daima;

namespace Daima.EditorTools
{
    /// <summary>
    /// 将 Canvas main1 / Canvas right / Image4 改为“各工序配方执行进度”。
    /// 菜单：计划调度/更新右2工序配方执行图
    /// </summary>
    public static class RecipeChartUpdater
    {
        [MenuItem("计划调度/更新右2工序配方执行图", false, 21)]
        static void ApplyMenu()
        {
            Apply(true);
        }

        static void Apply(bool interactive)
        {
            RectTransform image4 = DashboardTablesMenu.FindRect("Image4", "Canvas right");
            if (image4 == null)
            {
                Debug.LogWarning("[RecipeChart] 未找到 Canvas main1 / Canvas right / Image4，跳过更新。");
                return;
            }

            Scene scene = image4.gameObject.scene;

            // 只清掉 Image4 里旧的进度图根节点
            for (int i = image4.childCount - 1; i >= 0; i--)
            {
                Transform child = image4.GetChild(i);
                if (child.name == "__Table_AGV配送任务进度" ||
                    child.name == "AGV配送任务进度" ||
                    child.name == "__Table_各工序配方执行进度")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            Font font = DashboardTablesController.LoadBuiltinFont();
            RectTransform root = DashboardChartFactory.BuildRecipeChart(
                image4, DashboardMockData.CreateRecipeProgress(), font);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "更新工序配方执行图");

            // 标题：Canvas right / Text4
            RectTransform titleRect = DashboardTablesMenu.FindRect("Text4", "Canvas right");
            if (titleRect != null)
            {
                var title = titleRect.GetComponent<Text>();
                if (title != null)
                {
                    Undo.RecordObject(title, "更新工序配方执行图标题");
                    title.text = "各工序配方执行进度";
                    EditorUtility.SetDirty(title);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RecipeChart] 已更新 Canvas main1 / Canvas right / Image4 工序配方执行图，并保存场景。");

            if (interactive) Selection.activeGameObject = image4.gameObject;
        }
    }
}
