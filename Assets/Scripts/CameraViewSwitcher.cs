using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 视角切换：默认用 Main Camera 看场景；
    /// 点击径向菜单里的 TileButton3 时切换到另一台 Camera，
    /// 点击其它页签按钮（TileButton1/2/4/5、Button robot）时切回 Main Camera。
    /// 两台相机都挂了 MouseCameraController，切换后同样可以用鼠标旋转。
    /// </summary>
    public static class CameraViewSwitcher
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                Debug.LogWarning("[CameraViewSwitcher] 场景里没有找到 Main Camera，视角切换未启用。");
                return;
            }

            Camera alt = FindAltCamera(main);
            if (alt == null)
            {
                Debug.LogWarning("[CameraViewSwitcher] 场景里没有找到第二台 Camera，点击 TileButton3 不会切换视角。");
                return;
            }

            SetActive(main, alt, false);

            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null) continue;

                string buttonName = button.gameObject.name;
                if (buttonName == "TileButton3")
                    button.onClick.AddListener(() => SetActive(main, alt, true));
                else if (buttonName.StartsWith("TileButton") || buttonName == "Button robot")
                    button.onClick.AddListener(() => SetActive(main, alt, false));
            }
        }

        static Camera FindAltCamera(Camera main)
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>(true);

            foreach (Camera cam in cameras)
                if (cam != null && cam != main && cam.gameObject.name == "Camera")
                    return cam;

            foreach (Camera cam in cameras)
                if (cam != null && cam != main)
                    return cam;

            return null;
        }

        static void SetActive(Camera main, Camera alt, bool useAlt)
        {
            main.enabled = !useAlt;
            alt.enabled = useAlt;
            Debug.Log("[CameraViewSwitcher] 当前视角：" + (useAlt ? alt.gameObject.name : main.gameObject.name));
        }
    }
}
