using UnityEngine;

/// <summary>
/// Manages switching between iframe (live site) and screenshot textures.
/// Attach to each Monitor quad alongside WebPanel.
/// The quad must have a material with Custom/DoubleSidedUnlit shader assigned.
/// </summary>
public class MonitorDisplay : MonoBehaviour
{
    [Header("Screenshot textures (assign in Inspector)")]
    public Texture2D beforeTexture;
    public Texture2D afterTexture;

    private MeshRenderer _renderer;
    private Material _material;
    private bool _completed = false;
    private bool _iframeActive = false;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null)
        {
            // Use whatever material is already on the quad (instanced)
            _material = _renderer.material;
        }
    }

    /// <summary>
    /// Mark this panel as completed (switches to afterTexture).
    /// </summary>
    public void SetCompleted()
    {
        _completed = true;
        if (!_iframeActive && _material != null)
        {
            _material.mainTexture = afterTexture;
        }
    }

    /// <summary>
    /// Show iframe, hide the screenshot texture.
    /// </summary>
    public void ShowIframe()
    {
        _iframeActive = true;
        if (_renderer != null)
            _renderer.enabled = false;
    }

    /// <summary>
    /// Hide iframe, show the appropriate screenshot texture.
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

    public bool IsCompleted => _completed;
}
