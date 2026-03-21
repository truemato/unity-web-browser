using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public RoomManager roomManager;

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
        // Start: show iframe on panel 0, show textures on others
        ActivatePanel(_currentPanel);

        if (autoRotateInterval > 0f)
        {
            StartCoroutine(AutoRotateLoop());
        }
    }

    /// <summary>
    /// Show iframe on the given panel, show textures on all others.
    /// </summary>
    private void ActivatePanel(int panelIndex)
    {
        for (int i = 0; i < TotalPanels; i++)
        {
            if (i == panelIndex)
            {
                // Active panel: iframe visible, texture hidden
                if (webPanels != null && i < webPanels.Length && webPanels[i] != null)
                    webPanels[i].ForceShow();
                if (monitorDisplays != null && i < monitorDisplays.Length && monitorDisplays[i] != null)
                    monitorDisplays[i].ShowIframe();
            }
            else
            {
                // Inactive panels: iframe hidden, texture visible
                if (webPanels != null && i < webPanels.Length && webPanels[i] != null)
                    webPanels[i].ForceHide();
                if (monitorDisplays != null && i < monitorDisplays.Length && monitorDisplays[i] != null)
                    monitorDisplays[i].ShowTexture();
            }
        }
    }

    /// <summary>
    /// Hide all iframes, show all textures (during rotation).
    /// </summary>
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
                // Mark current panel as completed (iframe was shown → after texture)
                if (monitorDisplays != null && _currentPanel < monitorDisplays.Length && monitorDisplays[_currentPanel] != null)
                    monitorDisplays[_currentPanel].SetCompleted();

                // Before rotating: hide all iframes, show textures
                DeactivateAllPanels();
                yield return null; // one frame for iframe to hide

                roomManager.RotateToNext();
                yield return new WaitUntil(() => !roomManager.IsRotating);

                _currentPanel = (_currentPanel + 1) % TotalPanels;

                // After rotation done: show iframe on new current panel
                ActivatePanel(_currentPanel);
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

        // Mark this panel as completed (switches to afterTexture)
        if (monitorDisplays != null && panelId < monitorDisplays.Length && monitorDisplays[panelId] != null)
            monitorDisplays[panelId].SetCompleted();

        StartCoroutine(DelayedRotation());
    }

    private IEnumerator DelayedRotation()
    {
        yield return new WaitForSeconds(delayBeforeRotation);

        // Hide all iframes, show textures
        DeactivateAllPanels();
        yield return null;

        roomManager.RotateToNext();
        yield return new WaitUntil(() => !roomManager.IsRotating);

        _currentPanel = (_currentPanel + 1) % TotalPanels;
        ActivatePanel(_currentPanel);
    }
}
