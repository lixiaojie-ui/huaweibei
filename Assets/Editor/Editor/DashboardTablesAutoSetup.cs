using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Daima;

namespace Daima.EditorTools
{
    /// <summary>
    /// 打开包含大屏 Canvas 的场景时，自动生成/升级内容：
    /// Image1 订单表格 + Image2~5 四个图表（AGV 调度示意、负荷柱状图、配送进度条、库存折线图）。
    /// 这样不用按 Play，Scene / Game 视图里都能直接看到。
    /// 用菜单「计划调度/清除已生成的表格与图表」主动删掉后，会记录“不要再自动加”，不会反复出现。
    /// </summary>
    [InitializeOnLoad]
    public static class DashboardTablesAutoSetup
    {
        internal const string SuppressPrefix = "Daima.DashboardTables.Suppressed.";

        static DashboardTablesAutoSetup()
        {
            EditorApplication.delayCall += TryBuild;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += TryBuild;
        }

        static void TryBuild()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryBuild;
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path)) return;

            // 恢复被上一版“美化”改过的场景对象（面板底图、外框、标题颜色等）
            RestoreBaseline();

            // 用户主动清除过，就不再自动加
            if (EditorPrefs.GetBool(SuppressPrefix + scene.path, false)) return;

            DashboardTablesController controller =
                Object.FindObjectOfType<DashboardTablesController>(true);

            // 已经有内容：内容版本落后时自动升级成“订单表格 + 四个图表”
            if (controller != null)
            {
                if (controller.contentVersion != DashboardTablesController.CurrentContentVersion)
                {
                    controller.BuildAll();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log("[DashboardTables] 场景内容已更新为『Image1 订单表格 + Image2~5 四个图表』。" +
                              "确认无误后按 Ctrl+S 保存场景。");
                }
                return;
            }

            // 控制器脚本位于 Assets/Editor，属于编辑器程序集，无法自动挂载到场景对象上。
            // 场景内容已经是烘焙好的静态内容，不再自动生成，避免每次打开场景都留下空的 __DashboardTables。
            return;
        }

        /// <summary>
        /// 把上一版“美化”改动过的场景对象恢复原状：
        /// 重新打开面板底图、列外框、外框/圆弧，删除暗场遮罩，标题恢复白色。
        /// </summary>
        static void RestoreBaseline()
        {
            RestoreImage("Image1");
            RestoreImage("Image2");
            RestoreImage("Image3");
            RestoreImage("Image4");
            RestoreImage("Image5");
            RestoreImage("Image 123");
            RestoreImage("Image 456");

            GameObject center = GameObject.Find("Canvas");
            if (center != null)
            {
                RestoreActive(center.transform, "frame");
                RestoreActive(center.transform, "circle");
                RestoreActive(center.transform, "annulus");
            }

            GameObject backdrop = GameObject.Find("__Table_Backdrop");
            if (backdrop != null) Object.DestroyImmediate(backdrop);

            RestoreTitle("Text1");
            RestoreTitle("Text2");
            RestoreTitle("Text3");
            RestoreTitle("Text4");
            RestoreTitle("Text5");
        }

        static void RestoreImage(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return;

            Image image = go.GetComponent<Image>();
            if (image != null) image.enabled = true;
        }

        static void RestoreActive(Transform root, string name)
        {
            Transform child = root.Find(name);
            if (child != null) child.gameObject.SetActive(true);
        }

        static void RestoreTitle(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return;

            Text text = go.GetComponent<Text>();
            if (text != null) text.color = new Color(0.90f, 0.95f, 0.99f, 1f);   // 深蓝玻璃主题：浅色标题
        }
    }
}
