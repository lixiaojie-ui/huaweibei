using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Daima;

namespace Daima.EditorTools
{
    /// <summary>
    /// 把订单表格（Image1）和四个图表（Image2~Image5）直接生成到场景里（编辑器里也能看到，不只是 Play）。
    /// 生成后请保存场景（Ctrl+S）。
    /// </summary>
    public static class DashboardTablesMenu
    {
        [MenuItem("计划调度/生成订单表格与四个图表", false, 1)]
        static void GenerateTablesMenu()
        {
            if (!GenerateTables(true)) return;

            Debug.Log("[DashboardTables] 已生成 Image1 订单表格 + Image2~5 四个图表。请按 Ctrl+S 保存场景。" +
                      "如果 Scene 视图里看不到，选中 Canvas left / Canvas right 后按 F 框选，或切到 2D 视图。");
        }

        /// <summary>
        /// 生成（或重新生成）订单表格与四个图表。
        /// interactive 为 true 时找不到挂载点会弹窗；自动流程则静默返回 false。
        /// </summary>
        internal static bool GenerateTables(bool interactive)
        {
            RectTransform image1 = FindRect("Image1", "Canvas left");
            RectTransform image2 = FindRect("Image2", "Canvas left");
            RectTransform image3 = FindRect("Image3", "Canvas right");
            RectTransform image4 = FindRect("Image4", "Canvas right");
            RectTransform image5 = FindRect("Image5", "Canvas right");

            if (image1 == null || image2 == null || image3 == null ||
                image4 == null || image5 == null)
            {
                if (interactive)
                {
                    var missing = new List<string>();
                    if (image1 == null) missing.Add("Canvas left / Image1");
                    if (image2 == null) missing.Add("Canvas left / Image2");
                    if (image3 == null) missing.Add("Canvas right / Image3");
                    if (image4 == null) missing.Add("Canvas right / Image4");
                    if (image5 == null) missing.Add("Canvas right / Image5");

                    EditorUtility.DisplayDialog(
                        "没有找到挂载点",
                        "以下物体不存在，请先确认场景里的层级名称：\n\n" + string.Join("\n", missing),
                        "知道了");
                }
                return false;
            }

            DashboardTablesController controller =
                Object.FindObjectOfType<DashboardTablesController>(true);

            if (controller == null)
            {
                var root = new GameObject("__DashboardTables");
                Undo.RegisterCreatedObjectUndo(root, "创建表格控制器");
                controller = root.AddComponent<DashboardTablesController>();

                // 控制器脚本在 Assets/Editor 里，Unity 不允许挂到场景对象上；清理临时对象，避免留下空物体
                if (controller == null)
                {
                    Undo.DestroyObjectImmediate(root);
                    if (interactive)
                        EditorUtility.DisplayDialog("无法创建控制器",
                            "DashboardTablesController 位于 Assets/Editor，无法挂到场景对象上。\n" +
                            "请先把它移到 Assets/Scripts 再生成。", "知道了");
                    return false;
                }
            }

            Undo.RecordObject(controller, "生成订单表格与图表");

            controller.image1 = image1;
            controller.image2 = image2;
            controller.image3 = image3;
            controller.image4 = image4;
            controller.image5 = image5;
            controller.font = DashboardTablesController.LoadBuiltinFont();
            controller.autoScrollOrderTable = true;

            controller.BuildAll();

            foreach (RectTransform generated in controller.GeneratedRoots)
            {
                if (generated != null)
                    Undo.RegisterCreatedObjectUndo(generated.gameObject, "生成表格与图表");
            }

            EditorUtility.SetDirty(controller);

            var scene = controller.gameObject.scene;
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);

            if (interactive) Selection.activeGameObject = image1.gameObject;
            return true;
        }

        [MenuItem("计划调度/清除已生成的表格与图表", false, 2)]
        static void ClearTables()
        {
            DashboardTablesController controller =
                Object.FindObjectOfType<DashboardTablesController>(true);

            if (controller != null)
            {
                var scene = controller.gameObject.scene;

                Undo.RecordObject(controller, "清除表格与图表");
                controller.ClearAll();
                Undo.DestroyObjectImmediate(controller.gameObject);

                SuppressAutoBuild(scene);
                if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[DashboardTables] 已清除表格与图表（本场景不会再自动生成；需要时可用菜单重新生成）。");
                return;
            }

            // 场景里没有控制器时，按名字前缀兜底清理
            bool removed = false;
            Scene removedScene = default(Scene);
            RectTransform[] all = Object.FindObjectsOfType<RectTransform>(true);
            foreach (RectTransform rect in all)
            {
                if (!rect.name.StartsWith(DashboardTablesController.TablePrefix)) continue;

                var scene = rect.gameObject.scene;
                Undo.DestroyObjectImmediate(rect.gameObject);
                if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
                removedScene = scene;
                removed = true;
            }

            if (removed) SuppressAutoBuild(removedScene);
            Debug.Log(removed ? "[DashboardTables] 已清除残留内容。" : "[DashboardTables] 场景里没有找到已生成的内容。");
        }

        static void SuppressAutoBuild(Scene scene)
        {
            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
                EditorPrefs.SetBool(DashboardTablesAutoSetup.SuppressPrefix + scene.path, true);
        }

        /// <summary>场景里是否已经存在生成过的内容（按名字前缀判断）。</summary>
        internal static bool HasGeneratedTables()
        {
            RectTransform[] all = Object.FindObjectsOfType<RectTransform>(true);
            foreach (RectTransform rect in all)
            {
                if (rect.name.StartsWith(DashboardTablesController.TablePrefix)) return true;
            }
            return false;
        }

        internal static RectTransform FindRect(string name, string ancestor)
        {
            RectTransform[] all = Object.FindObjectsOfType<RectTransform>(true);
            foreach (RectTransform rect in all)
            {
                if (rect.name != name) continue;

                Transform parent = rect.parent;
                while (parent != null)
                {
                    if (parent.name == ancestor) return rect;
                    parent = parent.parent;
                }
            }
            return null;
        }
    }
}
