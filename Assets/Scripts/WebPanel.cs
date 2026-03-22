using UnityEngine;
using System.Runtime.InteropServices;

public class WebPanel : MonoBehaviour
{
    [Header("URL of the static site to display")]
    public string siteURL = "about:blank";

    [Header("Panel ID (0, 1, 2)")]
    public int panelId = 0;

    [Header("iframe pixel size")]
    public int iframeWidth = 1920;
    public int iframeHeight = 1080;


#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CreateIframe(string url, int panelId, int pixelWidth, int pixelHeight);

    [DllImport("__Internal")]
    private static extern void UpdateIframeRect(int panelId, float left, float top, float width, float height, bool visible);

    [DllImport("__Internal")]
    private static extern void UpdateIframeURL(int panelId, string url);

    [DllImport("__Internal")]
    private static extern void DestroyIframe(int panelId);
#endif

    private bool _created = false;
    private Camera _cam;
    private bool _forceHidden = false;

    void Start()
    {
        _cam = Camera.main;
#if UNITY_WEBGL && !UNITY_EDITOR
        CreateIframe(siteURL, panelId, iframeWidth, iframeHeight);
        // Start hidden until LevelManager says to show
        UpdateIframeRect(panelId, 0, 0, 0, 0, false);
        _forceHidden = true;
#endif
        _created = true;
    }

    /// <summary>
    /// Force-hide the iframe (used during rotation).
    /// </summary>
    public void ForceHide()
    {
        _forceHidden = true;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (_created) UpdateIframeRect(panelId, 0, 0, 0, 0, false);
#endif
    }

    /// <summary>
    /// Allow the iframe to be shown (position tracked in LateUpdate).
    /// </summary>
    public void ForceShow()
    {
        _forceHidden = false;
    }

    void LateUpdate()
    {
        if (!_created || _cam == null || _forceHidden) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Get the quad's 4 corners in world space and project to screen
        Vector3 scale = transform.lossyScale;
        float halfW = scale.x * 0.5f;
        float halfH = scale.y * 0.5f;

        Vector3 right = transform.right;
        Vector3 up = transform.up;

        Vector3 topLeft = transform.position - right * halfW + up * halfH;
        Vector3 topRight = transform.position + right * halfW + up * halfH;
        Vector3 bottomLeft = transform.position - right * halfW - up * halfH;
        Vector3 bottomRight = transform.position + right * halfW - up * halfH;

        Vector3 tlScreen = _cam.WorldToScreenPoint(topLeft);
        Vector3 trScreen = _cam.WorldToScreenPoint(topRight);
        Vector3 blScreen = _cam.WorldToScreenPoint(bottomLeft);
        Vector3 brScreen = _cam.WorldToScreenPoint(bottomRight);

        // Check if behind camera
        if (tlScreen.z < 0 || trScreen.z < 0 || blScreen.z < 0 || brScreen.z < 0)
        {
            UpdateIframeRect(panelId, 0, 0, 0, 0, false);
            return;
        }

        // Calculate bounding box in screen space
        float minX = Mathf.Min(tlScreen.x, trScreen.x, blScreen.x, brScreen.x);
        float maxX = Mathf.Max(tlScreen.x, trScreen.x, blScreen.x, brScreen.x);
        float minY = Mathf.Min(tlScreen.y, trScreen.y, blScreen.y, brScreen.y);
        float maxY = Mathf.Max(tlScreen.y, trScreen.y, blScreen.y, brScreen.y);

        // Normalize to 0-1 range (Unity screen coords bottom-left origin)
        float sw = Screen.width;
        float sh = Screen.height;
        float normLeft = minX / sw;
        float normTop = 1f - (maxY / sh);  // flip Y for CSS top-left origin
        float normWidth = (maxX - minX) / sw;
        float normHeight = (maxY - minY) / sh;

        UpdateIframeRect(panelId, normLeft, normTop, normWidth, normHeight, true);
#endif
    }

    public void SetURL(string url)
    {
        siteURL = url;
#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateIframeURL(panelId, url);
#endif
    }

    void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (_created) DestroyIframe(panelId);
#endif
    }
}
