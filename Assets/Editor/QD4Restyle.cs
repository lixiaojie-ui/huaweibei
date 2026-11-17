using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 只把 Canvas main4 的字号还原成原来的值，颜色/内容/层级/对象一律不动，不新增、不删除、不恢复。
    /// </summary>
    static class QD4Restyle
    {
        const string Key = "QD4FontRevert_v1";

        [MenuItem("Tools/质量检测/还原字号（不动其它）", false, 31)]
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

        static GameObject FindCanvas()
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != "Canvas main4") continue;
                if (t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded) return t.gameObject;
            }
            return null;
        }

        static void Apply()
        {
            GameObject canvas = FindCanvas();
            if (canvas == null) { EditorApplication.delayCall += Try; return; }

            const float scale = 1.18f;
            int count = 0;

            foreach (Text t in canvas.GetComponentsInChildren<Text>(true))
            {
                // 之前被单独指定过的，恢复成原始值
                if (t.name == "qdTitle") t.fontSize = 13;
                else if (t.name == "qdTitleEn") t.fontSize = 8;
                else if (t.name.StartsWith("qdTh") || t.name == "qdTableFoot") t.fontSize = 8;
                else if (t.name.StartsWith("qdTd")) t.fontSize = 9;
                else t.fontSize = Mathf.Max(6, Mathf.RoundToInt(t.fontSize / scale));

                EditorUtility.SetDirty(t);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            EditorSceneManager.SaveOpenScenes();
            EditorPrefs.SetInt(Key, 1);
            Debug.Log("[QD4] 字号已还原，共 " + count + " 个文本；颜色/内容/层级未改动。");
        }
    }
}
