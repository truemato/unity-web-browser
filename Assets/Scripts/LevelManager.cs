using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public RoomManager roomManager;

    [Header("Delay before rotation (seconds)")]
    public float delayBeforeRotation = 3f;

    [Header("Test mode: auto-rotate every N seconds (0 = off)")]
    public float autoRotateInterval = 3f;

    private int _currentPanel = 0;
    private const int TotalPanels = 3;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitBrowser();
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitBrowser();
#endif
        if (autoRotateInterval > 0f)
        {
            StartCoroutine(AutoRotateLoop());
        }
    }

    private IEnumerator AutoRotateLoop()
    {
        // Wait for initial display
        yield return new WaitForSeconds(autoRotateInterval);

        while (true)
        {
            if (roomManager != null && !roomManager.IsRotating)
            {
                roomManager.RotateToNext();
                yield return new WaitUntil(() => !roomManager.IsRotating);
                _currentPanel = (_currentPanel + 1) % TotalPanels;
            }
            yield return new WaitForSeconds(autoRotateInterval);
        }
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

        yield return new WaitUntil(() => !roomManager.IsRotating);

        _currentPanel = (_currentPanel + 1) % TotalPanels;
    }
}
