using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// Canvas main4 的卡片底色改成与 Canvas main1 一致（0.05,0.14,0.24,0.98）。
    /// </summary>
    static class QD4CardBgAlign
    {
        const string Key = "QD4_CardBgAlign_v2";
        static readonly Color Main1CardBg = new Color(0.055f, 0.180f, 0.300f, 0.239f);

        [MenuItem("Tools/质量检测/卡片底色对齐 main1", false, 50)]
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
            GameObject root = FindCanvas();
            if (root == null) { EditorApplication.delayCall += Try; return; }

            int changed = 0;
            foreach (Image im in root.GetComponentsInChildren<Image>(true))
            {
                if (!im.gameObject.name.StartsWith("Card-")) continue;
                if (im.color == Main1CardBg) continue;
                im.color = Main1CardBg;
                EditorUtility.SetDirty(im);
                changed++;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            EditorPrefs.SetInt(Key, 1);
            Debug.Log("[QD] Canvas main4 卡片底色已对齐 main1，改动 " + changed + " 张。");
        }
    }
}
