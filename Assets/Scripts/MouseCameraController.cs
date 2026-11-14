using UnityEngine;

namespace Daima
{
    /// <summary>
    /// 运行时鼠标视角控制：按住右键绕场景中心点旋转（上下左右），
    /// 滚轮沿视线拉近/拉远，WASD 平移中心点、Q/E 升降，Shift 加速。
    /// 采用“绕中心点公转”而不是原地转头，画面边缘不会被拉伸变形。
    /// </summary>
    public sealed class MouseCameraController : MonoBehaviour
    {
        [Header("旋转")]
        public float rotateSensitivity = 3f;
        public float pitchMin = -85f;
        public float pitchMax = 85f;

        [Header("缩放（沿视线移动，不改视野角）")]
        public float zoomSpeed = 10f;
        public float minDistance = 2f;
        public float maxDistance = 500f;

        [Header("旋转中心")]
        [Tooltip("自动用场景模型中心作为旋转中心；找不到时退回到初始视线前方。")]
        public bool autoDetectCenter = true;
        [Tooltip("作为旋转中心的模型根节点名称。")]
        public string centerRootName = "展示车间场景";
        [Tooltip("自动检测失败时，中心点放在相机前方多远。")]
        public float defaultDistance = 120f;

        [Header("平移")]
        public float moveSpeed = 40f;
        public float boostMultiplier = 3f;

        Vector3 pivot;
        float yaw;
        float pitch;
        float distance;
        Camera cam;
        bool initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        void Update()
        {
            EnsureInitialized();

            // 相机组件被切换关闭时，不响应鼠标，避免操作到看不见的那台相机
            if (cam == null || !cam.enabled) return;

            bool changed = false;

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * rotateSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * rotateSensitivity;
                pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
                changed = true;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
                changed = true;
            }

            float h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            float u = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            if (h != 0f || v != 0f || u != 0f)
            {
                bool boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                float speed = moveSpeed * (boost ? boostMultiplier : 1f);
                Vector3 dir = transform.right * h + transform.forward * v + Vector3.up * u;
                pivot += dir * (speed * Time.unscaledDeltaTime);
                changed = true;
            }

            if (changed) ApplyOrbit();
        }

        void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            cam = GetComponent<Camera>();

            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            Vector3 position = transform.position;
            Vector3 forward = transform.forward;

            // 中心点取场景模型中心在初始视线方向上的深度，保证初始画面不变
            float initialDistance = defaultDistance;
            Vector3 center;
            if (autoDetectCenter && TryGetContentCenter(out center))
            {
                float depth = Vector3.Dot(center - position, forward);
                if (depth >= minDistance)
                    initialDistance = depth;
            }

            distance = Mathf.Clamp(initialDistance, minDistance, maxDistance);
            pivot = position + forward * distance;

            ApplyOrbit();
        }

        void ApplyOrbit()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rotation;
            transform.position = pivot - rotation * Vector3.forward * distance;
        }

        bool TryGetContentCenter(out Vector3 center)
        {
            center = transform.position + transform.forward * defaultDistance;

            if (string.IsNullOrEmpty(centerRootName))
                return false;

            GameObject root = GameObject.Find(centerRootName);
            if (root == null)
                return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;
            Bounds bounds = new Bounds();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

                Bounds rendererBounds = renderer.bounds;
                if (rendererBounds.size.sqrMagnitude < 0.0001f) continue;

                if (!hasBounds)
                {
                    bounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }

            if (!hasBounds)
                return false;

            center = bounds.center;
            return true;
        }

        static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
