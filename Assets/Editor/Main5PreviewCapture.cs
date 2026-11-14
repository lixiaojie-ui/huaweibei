using System;
using System.Collections;
using System.IO;
using Daima;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>临时预览：进入播放模式，逐页切换并截图到 Library/main5_preview。</summary>
[InitializeOnLoad]
static class Main5PreviewCapture
{
    const string RequestPath = "Library/main5_preview_request.txt";
    const string DonePath = "Library/main5_preview_done.txt";
    const string PendingKey = "Main5PreviewPending";
    const string Dir = "Library/main5_preview";

    static Main5PreviewCapture()
    {
        EditorApplication.delayCall += Tick;
    }

    static void Tick()
    {
        if (SessionState.GetBool(PendingKey, false))
        {
            if (EditorApplication.isPlaying)
            {
                SessionState.SetBool(PendingKey, false);
                GameObject host = new GameObject("Main5PreviewRunner");
                host.AddComponent<PreviewRunner>().StartCoroutine(RunAll(host));
            }
            else if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Tick;
            }
            return;
        }

        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Tick;
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Tick;
            return;
        }

        File.Delete(RequestPath);
        Directory.CreateDirectory(Dir);
        if (File.Exists(DonePath)) File.Delete(DonePath);

        EditorSceneManager.SaveOpenScenes();

        Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType != null)
        {
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, "Game", false);
            if (gameView != null) gameView.Focus();
        }

        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    static IEnumerator RunAll(GameObject host)
    {
        yield return new WaitForSeconds(1.0f);

        SectionPageSwitcher switcher = null;
        SectionPageSwitcher[] all = Resources.FindObjectsOfTypeAll<SectionPageSwitcher>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene.IsValid())
            {
                switcher = all[i];
                break;
            }
        }

        int count = switcher != null && switcher.pages != null ? switcher.pages.Length : 0;

        if (count == 0)
        {
            File.WriteAllText(DonePath, "FAIL: no switcher");
            EditorApplication.isPlaying = false;
            yield break;
        }

        // TilePanelSwitcher 启动时只保留 Canvas main1，这里只打开 main5
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.gameObject.scene.IsValid()) continue;
            string n = c.gameObject.name;
            if (n.StartsWith("Canvas main", StringComparison.Ordinal) && n != "Canvas main5")
                c.gameObject.SetActive(false);
        }

        Transform chain = switcher.transform;
        while (chain != null)
        {
            if (!chain.gameObject.activeSelf) chain.gameObject.SetActive(true);
            chain = chain.parent;
        }

        yield return null;
        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < count; i++)
        {
            switcher.Show(i, true);
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Dir + "/page" + (i + 1) + ".png"));
            yield return new WaitForSeconds(0.6f);
        }

        yield return new WaitForSeconds(0.6f);
        File.WriteAllText(DonePath, "OK " + count + " " + Screen.width + "x" + Screen.height);
        EditorApplication.isPlaying = false;
    }

    public class PreviewRunner : MonoBehaviour
    {
    }
}

