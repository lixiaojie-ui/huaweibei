using System.Collections;
using UnityEngine;

namespace Daima
{
    /// <summary>
    /// 点击按钮时，把当前正在渲染的相机平滑移动到场景里某个视角点（位置 + 朝向）。
    /// 按钮 OnClick 里调用 GoTo(Transform)，参数拖入 ViewPoint_1 之类的空物体。
    /// </summary>
    public sealed class CameraSwitchToPoint : MonoBehaviour
    {
        public float duration = 0.7f;

        [Tooltip("要飞到的视角点；OnClick 也可以直接把视角点拖进 GoTo 的参数。")]
        public Transform viewPoint;

        /// <summary>无参版本，供 OnClick 直接调用，使用上面的 viewPoint。</summary>
        public void Go()
        {
            Debug.Log("[CameraSwitchToPoint] Go，视角点=" + (viewPoint != null ? viewPoint.name : "空"), this);
            GoTo(viewPoint);
        }

        public void GoTo(Transform point)
        {
            // 先停掉上一次飞行，再判断参数；这样即使这次没给视角点，
            // 也不会让上一次的飞行继续跑到旧的视角去。
            StopAllCoroutines();

            if (point == null)
            {
                Debug.LogWarning("[CameraSwitchToPoint] 没有设置视角点（viewPoint / GoTo 参数为空）。", this);
                return;
            }
            StartCoroutine(MoveTo(point));
        }

        /// <summary>
        /// 取消还在进行的飞行，并把鼠标控制恢复到相机当前所处的位姿。
        /// 切换到“不移动相机”的页签时调用，避免相机还在飞上一个页签的视角。
        /// </summary>
        public void Cancel()
        {
            StopAllCoroutines();

            Camera cam = ActiveCamera();
            if (cam == null) return;

            MouseCameraController mouse = cam.GetComponent<MouseCameraController>();
            if (mouse != null)
            {
                mouse.SyncToCurrentView();
                mouse.enabled = true;
            }
        }

        IEnumerator MoveTo(Transform point)
        {
            // 用正在渲染的那台相机，避免 Main Camera 被关闭时 Camera.main 返回 null
            Camera cam = ActiveCamera();
            if (cam == null) yield break;

            MouseCameraController mouse = cam.GetComponent<MouseCameraController>();
            if (mouse != null) mouse.enabled = false;

            Vector3 fromPos = cam.transform.position;
            Quaternion fromRot = cam.transform.rotation;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                cam.transform.position = Vector3.Lerp(fromPos, point.position, k);
                cam.transform.rotation = Quaternion.Slerp(fromRot, point.rotation, k);
                yield return null;
            }
            cam.transform.SetPositionAndRotation(point.position, point.rotation);
            Debug.Log("[CameraSwitchToPoint] 到达 " + point.name + "，相机=" + cam.name +
                " pos=" + cam.transform.position + " euler=" + cam.transform.eulerAngles, this);

            if (mouse != null)
            {
                // 用新视角重建旋转中心，之后鼠标操作从新视角继续
                mouse.SyncToCurrentView();
                mouse.enabled = true;
            }
        }

        static Camera ActiveCamera()
        {
            // 场景里有两台相机（Main Camera / Camera），取当前启用、depth 最大的那台，
            // 也就是真正渲染在最上面的相机；不要依赖 allCameras 的返回顺序。
            Camera best = null;
            foreach (Camera c in Camera.allCameras)
            {
                if (c == null || !c.isActiveAndEnabled) continue;
                if (best == null || c.depth > best.depth) best = c;
            }
            return best != null ? best : Camera.main;
        }
    }
}
