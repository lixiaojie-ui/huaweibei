using Daima;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 把 Canvas main2 / Canvas main4 的配色统一到 main1、main5 的色调（深蓝底 + 青蓝主色 + 柔和绿/琥珀）。
    /// 只改颜色，不动字号、内容、层级，不新增/删除/恢复任何对象。
    /// </summary>
    static class QDPaletteUnify
    {
        const string Key = "QD_Palette_v1";

        // 源色 -> 目标色（alpha 默认保留原值）
        static readonly Color[] From =
        {
            new Color(0.93f, 0.97f, 1.00f), new Color(0.91f, 0.95f, 0.98f),
            new Color(0.75f, 0.88f, 0.95f), new Color(0.66f, 0.79f, 0.85f),
            new Color(0.51f, 0.64f, 0.71f), new Color(0.59f, 0.69f, 0.74f),
            new Color(0.21f, 0.90f, 0.73f), new Color(0.31f, 0.91f, 0.76f),
            new Color(0.26f, 0.87f, 0.62f), new Color(0.27f, 0.84f, 0.61f),
            new Color(0.96f, 0.76f, 0.38f), new Color(1.00f, 0.71f, 0.33f),
            new Color(1.00f, 0.77f, 0.42f), new Color(1.00f, 0.55f, 0.58f),
            new Color(0.23f, 0.85f, 1.00f), new Color(0.31f, 0.84f, 0.98f),
            new Color(0.56f, 0.92f, 0.99f), new Color(0.40f, 0.87f, 0.97f),
            new Color(0.35f, 0.65f, 0.80f), new Color(0.25f, 0.67f, 0.86f),
            new Color(0.07f, 0.20f, 0.33f), new Color(0.04f, 0.18f, 0.28f),
            new Color(0.04f, 0.09f, 0.13f)
        };

        static readonly Color[] To =
        {
            new Color(0.92f, 0.96f, 1.00f), new Color(0.92f, 0.96f, 1.00f),
            new Color(0.75f, 0.85f, 0.91f), new Color(0.66f, 0.79f, 0.86f),
            new Color(0.57f, 0.71f, 0.78f), new Color(0.66f, 0.79f, 0.86f),
            new Color(0.44f, 0.89f, 0.68f), new Color(0.44f, 0.89f, 0.68f),
            new Color(0.44f, 0.89f, 0.68f), new Color(0.44f, 0.89f, 0.68f),
            new Color(0.98f, 0.70f, 0.24f), new Color(0.98f, 0.70f, 0.24f),
            new Color(0.99f, 0.81f, 0.48f), new Color(1.00f, 0.42f, 0.42f),
            new Color(0.20f, 0.76f, 0.92f), new Color(0.24f, 0.59f, 0.87f),
            new Color(0.54f, 0.83f, 0.97f), new Color(0.44f, 0.83f, 1.00f),
            new Color(0.49f, 0.80f, 0.96f), new Color(0.49f, 0.80f, 0.96f),
            new Color(0.05f, 0.14f, 0.24f), new Color(0.05f, 0.14f, 0.24f),
            new Color(0.03f, 0.08f, 0.13f)
        };

        static readonly Color TextInk = new Color(0.92f, 0.96f, 1.00f);
        static readonly Color SepLine = new Color(0.49f, 0.80f, 0.96f);

        [MenuItem("Tools/配色/统一 main2 与 main4 配色到 main1·main5", false, 40)]
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

        static bool Map(Color c, out Color outColor)
        {
            for (int i = 0; i < From.Length; i++)
            {
                Color f = From[i];
                if (Mathf.Abs(c.r - f.r) < 0.02f && Mathf.Abs(c.g - f.g) < 0.02f && Mathf.Abs(c.b - f.b) < 0.02f)
                {
                    Color t = To[i];
                    outColor = new Color(t.r, t.g, t.b, c.a);
                    return true;
                }
            }
            outColor = c;
            return false;
        }

        static GameObject FindCanvas(string name)
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != name) continue;
                if (t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded) return t.gameObject;
            }
            return null;
        }

        static int Apply()
        {
            int changed = 0;
            int canvases = 0;

            foreach (string canvasName in new[] { "Canvas main2", "Canvas main4" })
            {
                GameObject root = FindCanvas(canvasName);
                if (root == null) continue;
                canvases++;

                foreach (Text t in root.GetComponentsInChildren<Text>(true))
                {
                    Color c = t.color;
                    if (Map(c, out Color nc)) { t.color = nc; changed++; }
                    else if (Mathf.Abs(c.r - 1f) < 0.02f && Mathf.Abs(c.g - 1f) < 0.02f && Mathf.Abs(c.b - 1f) < 0.02f)
                    {
                        t.color = new Color(TextInk.r, TextInk.g, TextInk.b, c.a);
                        changed++;
                    }
                    EditorUtility.SetDirty(t);
                }

                foreach (Image im in root.GetComponentsInChildren<Image>(true))
                {
                    Color c = im.color;
                    if (Map(c, out Color nc)) { im.color = nc; changed++; }
                    else if (c.a > 0f && c.a < 0.2f &&
                             Mathf.Abs(c.r - 1f) < 0.02f && Mathf.Abs(c.g - 1f) < 0.02f && Mathf.Abs(c.b - 1f) < 0.02f)
                    {
                        im.color = new Color(SepLine.r, SepLine.g, SepLine.b, c.a);
                        changed++;
                    }
                    EditorUtility.SetDirty(im);
                }

                foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
                {
                    var rr = g as UIRoundRect;
                    if (rr != null)
                    {
                        bool dirty = false;
                        if (Map(rr.color, out Color c1)) { rr.color = c1; dirty = true; }
                        if (Map(rr.color2, out Color c2)) { rr.color2 = c2; dirty = true; }
                        if (dirty) { rr.Refresh(); changed++; }
                        EditorUtility.SetDirty(rr);
                        continue;
                    }

                    var line = g as UILineGraphic;
                    if (line != null && Map(line.color, out Color lc))
                    {
                        line.color = lc;
                        changed++;
                        EditorUtility.SetDirty(line);
                    }
                }
            }

            if (canvases > 0)
            {
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                EditorPrefs.SetInt(Key, 1);
                Debug.Log("[QD] 配色已统一到 main1/main5 色调，改动 " + changed + " 处（" + canvases + " 个 Canvas）。");
            }
            else
            {
                EditorApplication.delayCall += Try;
            }
            return changed;
        }
    }
}
