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
    private static extern void CreateIframe(string url,
        float posX, float posY, float posZ,
        float rotX, float rotY, float rotZ, float rotW,
        int width, int height, int panelId);

    [DllImport("__Internal")]
    private static extern void SyncIframeTransform(int panelId,
        float posX, float posY, float posZ,
        float rotX, float rotY, float rotZ, float rotW);

    [DllImport("__Internal")]
    private static extern void UpdateIframeURL(int panelId, string url);

    [DllImport("__Internal")]
    private static extern void DestroyIframe(int panelId);
#endif

    private bool _created = false;

    void Start()
    {
        CreatePanel();
    }

    void LateUpdate()
    {
        if (!_created) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Sync iframe position every frame (needed for room rotation)
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        SyncIframeTransform(panelId, pos.x, pos.y, pos.z,
            rot.x, rot.y, rot.z, rot.w);
#endif
    }

    public void CreatePanel()
    {
        if (_created) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        CreateIframe(siteURL,
            pos.x, pos.y, pos.z,
            rot.x, rot.y, rot.z, rot.w,
            iframeWidth, iframeHeight, panelId);
#endif

        _created = true;
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
