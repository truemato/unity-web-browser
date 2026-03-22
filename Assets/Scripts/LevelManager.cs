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

    [Header("Sound Effects")]
    public AudioClip zoomInSE;
    public AudioClip rotateStartSE;
    public AudioClip ambientSE;

    private int _currentPanel = 0;
    private const int TotalPanels = 3;
    private AudioSource _sfxSource;
    private AudioSource _ambientSource;
    private bool _leftM0 = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitBrowser();
#endif

    void Start()
    {
        Application.runInBackground = true;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        _ambientSource = gameObject.AddComponent<AudioSource>();
        _ambientSource.playOnAwake = false;
        _ambientSource.loop = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        InitBrowser();
#endif
        ActivatePanel(_currentPanel);

        // Start already zoomed in on first panel (no animation)
        SetZoomImmediate(_currentPanel);

        // Auto-rotate starts independently
        if (autoRotateInterval > 0f)
        {
            StartCoroutine(AutoRotateLoop());
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && _sfxSource != null)
            _sfxSource.PlayOneShot(clip);
    }

    private void StartAmbient()
    {
        if (ambientSE != null && _ambientSource != null && !_ambientSource.isPlaying)
        {
            _ambientSource.clip = ambientSE;
            _ambientSource.Play();
        }
    }

    private void SetZoomImmediate(int panelIndex)
    {
        if (cameraZoom != null && monitorTransforms != null
            && panelIndex < monitorTransforms.Length && monitorTransforms[panelIndex] != null)
        {
            cameraZoom.SetZoomedIn(monitorTransforms[panelIndex]);
        }
    }

    private IEnumerator TryZoomOutAndWait()
    {
        if (cameraZoom != null)
        {
            yield return cameraZoom.ZoomOut();
        }
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

    private IEnumerator RotateSequence()
    {
        DeactivateAllPanels();
        yield return StartCoroutine(TryZoomOutAndWait());

        // Play rotate SE
        PlaySFX(rotateStartSE);

        // Start ambient on first departure from M0
        if (!_leftM0)
        {
            _leftM0 = true;
            StartAmbient();
        }

        roomManager.RotateToNext();
        yield return new WaitUntil(() => !roomManager.IsRotating);

        _currentPanel = (_currentPanel + 1) % TotalPanels;

        yield return StartCoroutine(TryZoomInAndWait(_currentPanel));

        // Play zoom-in SE
        PlaySFX(zoomInSE);

        ActivatePanel(_currentPanel);
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

                yield return StartCoroutine(RotateSequence());
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
        yield return StartCoroutine(RotateSequence());
    }
}
