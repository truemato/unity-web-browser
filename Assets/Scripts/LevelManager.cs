using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public RoomManager roomManager;
    public CameraZoom cameraZoom;

    [Header("Monitor transforms (assign in order 0, 1, 2)")]
    public Transform[] monitorTransforms;

    [Header("Monitor references (assign in order 0, 1, 2)")]
    public WebPanel[] webPanels;
    public MonitorDisplay[] monitorDisplays;

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
        // Start: show textures on all, then zoom in on panel 0
        ActivatePanel(_currentPanel);
        StartCoroutine(InitialZoomIn());
    }

    private IEnumerator InitialZoomIn()
    {
        // Zoom into panel 0
        if (cameraZoom != null && monitorTransforms != null && _currentPanel < monitorTransforms.Length)
        {
            yield return cameraZoom.ZoomIn(monitorTransforms[_currentPanel]);
        }

        if (autoRotateInterval > 0f)
        {
            StartCoroutine(AutoRotateLoop());
        }
    }

    private void ActivatePanel(int panelIndex)
    {
        for (int i = 0; i < TotalPanels; i++)
        {
            if (i == panelIndex)
            {
                if (webPanels != null && i < webPanels.Length && webPanels[i] != null)
                    webPanels[i].ForceShow();
                if (monitorDisplays != null && i < monitorDisplays.Length && monitorDisplays[i] != null)
                    monitorDisplays[i].ShowIframe();
            }
            else
            {
                if (webPanels != null && i < webPanels.Length && webPanels[i] != null)
                    webPanels[i].ForceHide();
                if (monitorDisplays != null && i < monitorDisplays.Length && monitorDisplays[i] != null)
                    monitorDisplays[i].ShowTexture();
            }
        }
    }

    private void DeactivateAllPanels()
    {
        for (int i = 0; i < TotalPanels; i++)
        {
            if (webPanels != null && i < webPanels.Length && webPanels[i] != null)
                webPanels[i].ForceHide();
            if (monitorDisplays != null && i < monitorDisplays.Length && monitorDisplays[i] != null)
                monitorDisplays[i].ShowTexture();
        }
    }

    private IEnumerator AutoRotateLoop()
    {
        yield return new WaitForSeconds(autoRotateInterval);

        while (true)
        {
            if (roomManager != null && !roomManager.IsRotating)
            {
                if (monitorDisplays != null && _currentPanel < monitorDisplays.Length && monitorDisplays[_currentPanel] != null)
                    monitorDisplays[_currentPanel].SetCompleted();

                // 1. Zoom out
                DeactivateAllPanels();
                if (cameraZoom != null)
                    yield return cameraZoom.ZoomOut();

                // 2. Rotate room
                roomManager.RotateToNext();
                yield return new WaitUntil(() => !roomManager.IsRotating);

                _currentPanel = (_currentPanel + 1) % TotalPanels;

                // 3. Zoom in to new monitor, then show iframe
                if (cameraZoom != null && monitorTransforms != null && _currentPanel < monitorTransforms.Length)
                    yield return cameraZoom.ZoomIn(monitorTransforms[_currentPanel]);

                ActivatePanel(_currentPanel);
            }
            yield return new WaitForSeconds(autoRotateInterval);
        }
    }

    public void OnPageArrived(string panelIdStr)
    {
        int panelId;
        if (!int.TryParse(panelIdStr, out panelId)) return;
        if (panelId != _currentPanel) return;
        if (roomManager == null || roomManager.IsRotating) return;

        if (monitorDisplays != null && panelId < monitorDisplays.Length && monitorDisplays[panelId] != null)
            monitorDisplays[panelId].SetCompleted();

        StartCoroutine(DelayedRotation());
    }

    private IEnumerator DelayedRotation()
    {
        yield return new WaitForSeconds(delayBeforeRotation);

        DeactivateAllPanels();

        // Zoom out
        if (cameraZoom != null)
            yield return cameraZoom.ZoomOut();

        // Rotate
        roomManager.RotateToNext();
        yield return new WaitUntil(() => !roomManager.IsRotating);

        _currentPanel = (_currentPanel + 1) % TotalPanels;

        // Zoom in
        if (cameraZoom != null && monitorTransforms != null && _currentPanel < monitorTransforms.Length)
            yield return cameraZoom.ZoomIn(monitorTransforms[_currentPanel]);

        ActivatePanel(_currentPanel);
    }
}
