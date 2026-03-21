using UnityEngine;

/// <summary>
/// Manages switching between iframe (live site) and screenshot textures.
/// Attach to each Monitor quad alongside WebPanel.
/// </summary>
public class MonitorDisplay : MonoBehaviour
{
    [Header("Screenshot textures (assign in Inspector)")]
    public Texture2D beforeTexture;  // site appearance before browsing
    public Texture2D afterTexture;   // site appearance after level clear

    private MeshRenderer _renderer;
    private Material _material;
    private bool _completed = false;
    private bool _iframeActive = false;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        // Create an Unlit/Texture material so screenshots display correctly
        _material = new Material(Shader.Find("Unlit/Texture"));
        if (_renderer != null)
        {
            _renderer.material = _material;
        }
    }

    /// <summary>
    /// Mark this panel as completed (switches to afterTexture).
    /// </summary>
    public void SetCompleted()
    {
        _completed = true;
        // If currently showing texture, update immediately
        if (!_iframeActive && _material != null)
        {
            _material.mainTexture = afterTexture;
        }
    }

    /// <summary>
    /// Show iframe, hide the screenshot texture.
    /// Called by LevelManager when room stops and this monitor faces camera.
    /// </summary>
    public void ShowIframe()
    {
        _iframeActive = true;
        if (_renderer != null)
            _renderer.enabled = false;
    }

    /// <summary>
    /// Hide iframe, show the appropriate screenshot texture.
    /// Called by LevelManager when room starts rotating.
    /// </summary>
    public void ShowTexture()
    {
        _iframeActive = false;
        if (_renderer != null && _material != null)
        {
            _material.mainTexture = _completed ? afterTexture : beforeTexture;
            _renderer.enabled = true;
        }
    }
}
