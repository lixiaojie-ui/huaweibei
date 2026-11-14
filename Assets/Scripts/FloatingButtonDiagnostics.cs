using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>临时诊断：点击命中了哪些 UI、是否被遮挡。排查完删除。</summary>
    public static class FloatingButtonDiagnostics
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            GameObject go = new GameObject("[FloatingButtonDiagnostics]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        sealed class Runner : MonoBehaviour
        {
            int frames;
            bool? lastWorkshopView;
            Camera workshop;

            void Update()
            {
                if (workshop == null)
                {
                    foreach (Camera cam in Object.FindObjectsOfType<Camera>(true))
                        if (cam != null && cam.gameObject.name == "Camera")
                        {
                            workshop = cam;
                            break;
                        }
                }

                if (++frames == 5)
                {
                    DumpOnce();
                }

                if (workshop != null)
                {
                    bool now = workshop.enabled;
                    if (lastWorkshopView != null && lastWorkshopView.Value != now)
                        Debug.Log("[BtnDiag] 视角切换：车间视角=" + now);
                    lastWorkshopView = now;
                }

                if (Input.GetMouseButtonDown(0))
                    LogClick();
            }

            void DumpOnce()
            {
                Debug.Log("[BtnDiag] EventSystem=" + (EventSystem.current != null ? EventSystem.current.name : "null"));

                foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>(true))
                {
                    if (canvas.renderMode != RenderMode.WorldSpace) continue;

                    Transform t = canvas.transform;
                    GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                    Debug.Log("[BtnDiag] WorldCanvas '" + t.name +
                        "' active=" + canvas.gameObject.activeInHierarchy +
                        " worldCamera=" + (canvas.worldCamera != null ? canvas.worldCamera.name : "null") +
                        " raycaster=" + (raycaster != null ? "有" : "没有") +
                        " sortOrder=" + canvas.sortingOrder);

                    foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
                    {
                        int count = button.onClick.GetPersistentEventCount();
                        string detail = "";
                        for (int i = 0; i < count; i++)
                            detail += " [" + i + "] target=" +
                                (button.onClick.GetPersistentTarget(i) != null
                                    ? button.onClick.GetPersistentTarget(i).name
                                    : "null") +
                                " method=" + button.onClick.GetPersistentMethodName(i);

                        Debug.Log("[BtnDiag] Button '" + button.name +
                            "' active=" + button.gameObject.activeSelf +
                            " onClick=" + count + "个:" + detail);
                    }
                }

                Debug.Log("[BtnDiag] 诊断完成");
            }

            void LogClick()
            {
                EventSystem es = EventSystem.current;
                if (es == null)
                {
                    Debug.Log("[BtnDiag] 点击 @ " + Input.mousePosition + " 但场景没有 EventSystem");
                    return;
                }

                PointerEventData data = new PointerEventData(es) { position = Input.mousePosition };
                List<RaycastResult> results = new List<RaycastResult>();
                es.RaycastAll(data, results);

                string hits = "";
                int shown = Mathf.Min(results.Count, 6);
                for (int i = 0; i < shown; i++)
                {
                    Canvas c = results[i].gameObject.GetComponentInParent<Canvas>();
                    hits += " | " + results[i].gameObject.name +
                        "(" + (c != null ? c.renderMode + "," + c.name : "无Canvas") + ")";
                }

                Debug.Log("[BtnDiag] 点击 @ " + Input.mousePosition + " 命中" + results.Count + "个:" + hits);
            }
        }
    }
}
