using System.Collections;
using UnityEngine;

public class CameraFlyTo : MonoBehaviour
{
    public Transform viewPoint;   // 要飞到的空物体
    public Transform viewCamera;  // Main Camera
    public float duration = 0.7f; // 飞行时间（秒）

    public void Go()
    {
        if (viewPoint == null || viewCamera == null) return;
        StopAllCoroutines();
        StartCoroutine(Fly());
    }

    IEnumerator Fly()
    {
        Vector3 fromPos = viewCamera.position;
        Quaternion fromRot = viewCamera.rotation;

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            viewCamera.position = Vector3.Lerp(fromPos, viewPoint.position, k);
            viewCamera.rotation = Quaternion.Slerp(fromRot, viewPoint.rotation, k);
            yield return null;
        }

        viewCamera.position = viewPoint.position;
        viewCamera.rotation = viewPoint.rotation;
    }
}
