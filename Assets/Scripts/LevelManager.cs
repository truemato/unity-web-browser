using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public RoomManager roomManager;

    [Header("Delay before rotation (seconds)")]
    public float delayBeforeRotation = 3f;

    private int _currentPanel = 0;
    private const int TotalPanels = 3;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitBrowser();

    [DllImport("__Internal")]
    private static extern void SyncCameraTransform(
        float px, float py, float pz,
        float rx, float ry, float rz, float rw);
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitBrowser();
#endif
    }

    void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Sync camera transform every frame
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 pos = cam.transform.position;
            Quaternion rot = cam.transform.rotation;
            SyncCameraTransform(pos.x, pos.y, pos.z,
                rot.x, rot.y, rot.z, rot.w);
        }
#endif
    }

    /// <summary>
    /// Called from JavaScript via SendMessage when a page arrival is detected.
    /// </summary>
    public void OnPageArrived(string panelIdStr)
    {
        int panelId;
        if (!int.TryParse(panelIdStr, out panelId)) return;
        if (panelId != _currentPanel) return;
        if (roomManager == null || roomManager.IsRotating) return;

        StartCoroutine(DelayedRotation());
    }

    private IEnumerator DelayedRotation()
    {
        yield return new WaitForSeconds(delayBeforeRotation);

        roomManager.RotateToNext();

        // Wait for rotation to finish
        yield return new WaitUntil(() => !roomManager.IsRotating);

        _currentPanel = (_currentPanel + 1) % TotalPanels;
    }
}
