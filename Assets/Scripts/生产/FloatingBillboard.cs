using UnityEngine;

public class FloatingBillboard : MonoBehaviour
{
    public Camera viewCamera;   // 拖入 相机 / Camera

    void LateUpdate()
    {
        if (viewCamera != null)
            transform.rotation = viewCamera.transform.rotation;
    }
}
