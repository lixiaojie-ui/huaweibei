using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 把场景里的「生产总览」从大屏 Canvas 里取出来，做成独立的世界空间广告牌：
    /// 位置自由摆放，运行时用 FloatingBillboard 跟着相机转。
    /// </summary>
    public static class MakeBillboardLabel
    {
        [MenuItem("大屏/把「生产总览」做成场景广告牌")]
        static void Run()
        {
            GameObject label = FindByName("生产总览");
            if (label == null)
            {
                Debug.LogWarning("[广告牌] 场景里没有找到「生产总览」");
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
                camera = Object.FindObjectOfType<Camera>();

            GameObject root = new GameObject("生产总览广告牌",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "创建生产总览广告牌");

            // 1 个 Canvas 单位 = 1 厘米，整体大小靠这个缩放调
            root.transform.localScale = Vector3.one * 0.01f;

            Vector3 position = label.transform.position;
            if (SceneView.lastActiveSceneView != null)
                position = SceneView.lastActiveSceneView.pivot;
            root.transform.position = position;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 10f;

            root.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 120f);

            FloatingBillboard billboard = root.AddComponent<FloatingBillboard>();
            billboard.viewCamera = camera;

            Undo.SetTransformParent(label.transform, root.transform, "移动生产总览");
            label.transform.localPosition = Vector3.zero;
            label.transform.localRotation = Quaternion.identity;
            label.transform.localScale = Vector3.one;

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }

            Debug.Log("[广告牌] 已生成「生产总览广告牌」在 " + position + "，拖动根节点即可换位置。");
        }

        static GameObject FindByName(string name)
        {
            Transform[] all = Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                    return all[i].gameObject;
            }
            return null;
        }
    }
}
