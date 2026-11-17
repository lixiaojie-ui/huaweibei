using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 把 Canvas main2 的 Graphic 颜色还原成备份场景里的原值（按层级路径匹配）。
    /// </summary>
    static class QDRestoreMain2b
    {
        const string Key = "QD_RestoreMain2_v2";
        const string BackupScene = "Assets/Scenes/QD_restore_2108.unity";
        const string CanvasName = "Canvas main2";

        [MenuItem("Tools/配色/main2 还原备份里的原始配色", false, 48)]
        static void RunMenu() { Apply(); }

        [InitializeOnLoadMethod]
        static void Auto()
        {
            if (EditorPrefs.GetInt(Key, 0) >= 1) return;
            EditorApplication.delayCall += Try;
        }

        static void Try()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Try;
                return;
            }
            Apply();
        }

        static GameObject FindCanvas(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == CanvasName) return go;
            }
            return null;
        }

        static string PathOf(Transform t, Transform stop)
        {
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                if (p == stop) break;
                p = p.parent;
            }
            return path;
        }

        static void Apply()
        {
            Scene active = SceneManager.GetActiveScene();
            GameObject target = FindCanvas(active);
            if (target == null) { EditorApplication.delayCall += Try; return; }

            if (!System.IO.File.Exists(BackupScene))
            {
                Debug.LogWarning("[QD] 找不到备份场景 " + BackupScene);
                return;
            }

            Scene backup = EditorSceneManager.OpenScene(BackupScene, OpenSceneMode.Additive);
            GameObject source = FindCanvas(backup);
            if (source == null)
            {
                EditorSceneManager.CloseScene(backup, true);
                Debug.LogWarning("[QD] 备份场景里没有 " + CanvasName);
                return;
            }

            var colors = new Dictionary<string, Color>();
            foreach (Graphic g in source.GetComponentsInChildren<Graphic>(true))
            {
                colors[PathOf(g.transform, source.transform)] = g.color;
            }

            int restored = 0;
            foreach (Graphic g in target.GetComponentsInChildren<Graphic>(true))
            {
                Color c;
                if (!colors.TryGetValue(PathOf(g.transform, target.transform), out c)) continue;
                if (c == g.color) continue;
                g.color = c;

                var rr = g as UIRoundRect;
                if (rr != null) rr.Refresh();

                EditorUtility.SetDirty(g);
                restored++;
            }

            EditorSceneManager.CloseScene(backup, true);

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveOpenScenes();
            EditorPrefs.SetInt(Key, 1);
            Debug.Log("[QD] Canvas main2 已还原备份原始配色，恢复 " + restored + " 处。");
        }
    }
}
