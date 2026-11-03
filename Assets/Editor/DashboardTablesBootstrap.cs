using UnityEngine;

namespace Daima
{
    /// <summary>
    /// 兜底入口：如果场景里没有手动放置 DashboardTablesController，
    /// 运行时自动找到 Image1~Image5 并创建控制器，保证按 Play 就能看到表格。
    /// （推荐还是用菜单「计划调度/生成五个表格」把表格直接生成到场景里。）
    /// </summary>
    public static class DashboardTablesBootstrap
    {
        // 注意：DashboardTablesController 位于 Assets/Editor，属于编辑器程序集，运行时无法挂载。
        // 场景里的内容已经是烘焙好的，不再需要这个兜底；否则每次 Play 都会留下一个空的 __DashboardTables。
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (Object.FindObjectOfType<DashboardTablesController>() != null) return;

            RectTransform image1 = FindImage("Image1", "Canvas left");
            RectTransform image2 = FindImage("Image2", "Canvas left");
            RectTransform image3 = FindImage("Image3", "Canvas right");
            RectTransform image4 = FindImage("Image4", "Canvas right");
            RectTransform image5 = FindImage("Image5", "Canvas right");

            if (image1 == null || image2 == null || image3 == null ||
                image4 == null || image5 == null)
                return;

            var root = new GameObject("__DashboardTables");
            var controller = root.AddComponent<DashboardTablesController>();
            controller.image1 = image1;
            controller.image2 = image2;
            controller.image3 = image3;
            controller.image4 = image4;
            controller.image5 = image5;
            controller.font = DashboardTablesController.LoadBuiltinFont();

            Debug.Log("[DashboardTables] 场景中没有内容控制器，已自动创建并生成订单表格与四个图表。");
        }

        static RectTransform FindImage(string name, string ancestor)
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
