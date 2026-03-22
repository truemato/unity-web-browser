using UnityEngine;
using System.Collections;

/// <summary>
/// Smoothly zooms the camera toward the active monitor so the iframe fills the screen,
/// then zooms back before rotation.
/// </summary>
public class CameraZoom : MonoBehaviour
{
    [Header("Zoom settings")]
    public float zoomDuration = 0.5f;
    public float marginMultiplier = 1.05f; // slightly further than exact fit

    private Vector3 _originalPos;
    private Quaternion _originalRot;
    private Camera _cam;
    private bool _isZooming = false;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _originalPos = transform.position;
        _originalRot = transform.rotation;
    }

    public bool IsZooming => _isZooming;

    /// <summary>
    /// Immediately set the camera to the zoomed-in position (no animation).
    /// </summary>
    public void SetZoomedIn(Transform monitor)
    {
        if (monitor == null) return;

        Vector3 monitorScale = monitor.lossyScale;
        float monitorHeight = monitorScale.y;
        float monitorWidth = monitorScale.x;

        float vFov = _cam.fieldOfView * Mathf.Deg2Rad;
        float distForHeight = (monitorHeight * 0.5f) / Mathf.Tan(vFov * 0.5f);

        float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * _cam.aspect);
        float distForWidth = (monitorWidth * 0.5f) / Mathf.Tan(hFov * 0.5f);

        float requiredDist = Mathf.Max(distForHeight, distForWidth) * marginMultiplier;

        Vector3 toMonitor = (monitor.position - _originalPos).normalized;
        transform.position = monitor.position - toMonitor * requiredDist;
        transform.rotation = Quaternion.LookRotation(toMonitor, Vector3.up);
    }

    /// <summary>
    /// Zoom in toward a monitor transform so it fills the viewport.
    /// </summary>
    public Coroutine ZoomIn(Transform monitor)
    {
        return StartCoroutine(ZoomCoroutine(monitor, true));
    }

    /// <summary>
    /// Zoom back to the original camera position.
    /// </summary>
    public Coroutine ZoomOut()
    {
        return StartCoroutine(ZoomCoroutine(null, false));
    }

    private IEnumerator ZoomCoroutine(Transform monitor, bool zoomingIn)
    {
        _isZooming = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 endPos;
        Quaternion endRot;

        if (zoomingIn && monitor != null)
        {
            // Calculate distance where monitor fills the screen
            Vector3 monitorScale = monitor.lossyScale;
            float monitorHeight = monitorScale.y;
            float monitorWidth = monitorScale.x;

            float vFov = _cam.fieldOfView * Mathf.Deg2Rad;
            float distForHeight = (monitorHeight * 0.5f) / Mathf.Tan(vFov * 0.5f);

            // Also check horizontal fit
            float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * _cam.aspect);
            float distForWidth = (monitorWidth * 0.5f) / Mathf.Tan(hFov * 0.5f);

            float requiredDist = Mathf.Max(distForHeight, distForWidth) * marginMultiplier;

            // Move toward monitor along the line from camera to monitor
            Vector3 toMonitor = (monitor.position - _originalPos).normalized;
            endPos = monitor.position - toMonitor * requiredDist;
            endRot = Quaternion.LookRotation(toMonitor, Vector3.up);
        }
        else
        {
            endPos = _originalPos;
            endRot = _originalRot;
        }

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomDuration);
            float eased = t * t * (3f - 2f * t); // smoothstep

            transform.position = Vector3.Lerp(startPos, endPos, eased);
            transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
        _isZooming = false;
    }
}
