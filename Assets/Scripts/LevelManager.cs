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

    [Header("Rotation mode")]
    [Tooltip("ON: rotate only when puzzle cleared (PAGE_ARRIVED). OFF: auto-rotate on timer.")]
    public bool requireClearToRotate = true;

    [Header("Delay before rotation after clear (seconds)")]
    public float delayBeforeRotation = 3f;

    [Header("Auto-rotate interval (only used when requireClearToRotate is OFF, 0 = off)")]
    public float autoRotateInterval = 0f;

    private int _currentPanel = 0;
    private const int TotalPanels = 3;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitBrowser();
#endif

    void Start()
    {
        Application.runInBackground = true;
#if UNITY_WEBGL && !UNITY_EDITOR
        InitBrowser();
#endif
        ActivatePanel(_currentPanel);
        TryZoomIn(_currentPanel);

        // Auto-rotate only when not requiring clear
        if (!requireClearToRotate && autoRotateInterval > 0f)
        {
            StartCoroutine(AutoRotateLoop());
        }
    }

    private void TryZoomIn(int panelIndex)
    {
        if (cameraZoom != null && monitorTransforms != null
            && panelIndex < monitorTransforms.Length && monitorTransforms[panelIndex] != null)
        {
            cameraZoom.ZoomIn(monitorTransforms[panelIndex]);
        }
    }

    private IEnumerator TryZoomOutAndWait()
    {
        if (cameraZoom != null)
            yield return cameraZoom.ZoomOut();
    }

    private IEnumerator TryZoomInAndWait(int panelIndex)
    {
        if (cameraZoom != null && monitorTransforms != null
            && panelIndex < monitorTransforms.Length && monitorTransforms[panelIndex] != null)
        {
            yield return cameraZoom.ZoomIn(monitorTransforms[panelIndex]);
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
            if (roomManager != null && !roomManager.IsRotating
                && (cameraZoom == null || !cameraZoom.IsZooming))
            {
                if (monitorDisplays != null && _currentPanel < monitorDisplays.Length && monitorDisplays[_currentPanel] != null)
                    monitorDisplays[_currentPanel].SetCompleted();

                DeactivateAllPanels();
                yield return StartCoroutine(TryZoomOutAndWait());

                roomManager.RotateToNext();
                yield return new WaitUntil(() => !roomManager.IsRotating);

                _currentPanel = (_currentPanel + 1) % TotalPanels;

                yield return StartCoroutine(TryZoomInAndWait(_currentPanel));
                ActivatePanel(_currentPanel);
            }
            yield return new WaitForSeconds(autoRotateInterval);
        }
    }

    /// <summary>
    /// Called from JavaScript via SendMessage when a puzzle is cleared.
    /// </summary>
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
        yield return StartCoroutine(TryZoomOutAndWait());

        roomManager.RotateToNext();
        yield return new WaitUntil(() => !roomManager.IsRotating);

        _currentPanel = (_currentPanel + 1) % TotalPanels;

        yield return StartCoroutine(TryZoomInAndWait(_currentPanel));
        ActivatePanel(_currentPanel);
    }
}
