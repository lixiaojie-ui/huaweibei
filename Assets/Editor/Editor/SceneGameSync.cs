using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Daima.EditorTools
{
    /// <summary>
    /// 让 Scene 视图的显示尽量和 Game 视图一致：
    /// - 打开 Scene 视图的 Image Effects（后处理）、Fog、Skybox；
    /// - 把 Scene 视图相机对齐到 Main Camera（位置、朝向、FOV）。
    ///
    /// Game 视图的后处理（ColorGrading / Bloom）只对 Main Camera 生效，
    /// Scene 视图默认不显示，所以两边颜色会不一样。
    /// </summary>
    [InitializeOnLoad]
    public static class SceneGameSync
    {
        const string AlignedKey = "Daima.SceneGameSync.AlignedThisSession";

        static SceneGameSync()
        {
            EditorApplication.delayCall += ApplyOnce;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += ApplyOnce;
        }

        [MenuItem("计划调度/Scene 视图匹配 Game 视图", false, 40)]
        static void MatchNow()
        {
            ApplyEffects();
            AlignToMainCamera();
            Debug.Log("[DashboardTables] 已开启 Scene 视图的 Image Effects / Fog / Skybox，并对齐到 Main Camera。");
        }

        /// <summary>如果 Scene 视图仍然不显示 PPv2 后处理，可以用它把 Game 视图的后期处理关掉。</summary>
        [MenuItem("计划调度/开关 Game 视图后期处理", false, 41)]
        static void TogglePostProcessing()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            Component layer = camera.GetComponent("PostProcessLayer");
            var behaviour = layer as Behaviour;
            if (behaviour == null)
            {
                Debug.LogWarning("[DashboardTables] Main Camera 上没有找到 PostProcessLayer。");
                return;
            }

            behaviour.enabled = !behaviour.enabled;
            Debug.Log("[DashboardTables] Game 视图后期处理：" + (behaviour.enabled ? "开" : "关") +
                      "。关掉后 Game 与 Scene 的原始颜色一致。");
        }

        static int retry;

        /// <summary>每次会话自动执行一次，之后可用菜单重新对齐。</summary>
        static void ApplyOnce()
        {
            if (SceneView.lastActiveSceneView == null)
            {
                if (retry++ < 10) EditorApplication.delayCall += ApplyOnce;
                return;
            }

            retry = 0;
            ApplyEffects();

            if (SessionState.GetBool(AlignedKey, false)) return;
            SessionState.SetBool(AlignedKey, true);
            AlignToMainCamera();
        }

        static void ApplyEffects()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;

            SceneView.SceneViewState state = view.sceneViewState;
            state.showImageEffects = true;   // Scene 视图的 Image Effects / 后处理开关
            state.showFog = true;
            state.showSkybox = true;

            view.Repaint();
        }

        static void AlignToMainCamera()
        {
            SceneView view = SceneView.lastActiveSceneView;
            Camera camera = Camera.main;
            if (view == null || camera == null) return;

            view.AlignViewToObject(camera.transform);
            if (view.camera != null) view.camera.fieldOfView = camera.fieldOfView;

            view.Repaint();
        }
    }
}
