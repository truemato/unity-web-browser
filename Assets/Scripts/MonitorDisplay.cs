using UnityEngine;

/// <summary>
/// Manages switching between iframe (live site) and screenshot textures.
/// Attach to each Monitor quad alongside WebPanel.
/// Supports both URP Lit (_BaseMap) and custom shaders (_MainTex).
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
    private bool _useBaseMap = false;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            // Detect if shader uses _BaseMap (URP Lit) or _MainTex
            _useBaseMap = _material.HasProperty("_BaseMap");
        }
    }

    private void SetTexture(Texture2D tex)
    {
        if (_material == null || tex == null) return;
        if (_useBaseMap)
            _material.SetTexture("_BaseMap", tex);
        else
            _material.mainTexture = tex;
    }

    public void SetCompleted()
    {
        _completed = true;
        if (!_iframeActive)
            SetTexture(afterTexture);
    }

    public void ShowIframe()
    {
        _iframeActive = true;
        if (_renderer != null)
            _renderer.enabled = false;
    }

    public void ShowTexture()
    {
        _iframeActive = false;
        if (_renderer != null && _material != null)
        {
            SetTexture(_completed ? afterTexture : beforeTexture);
            _renderer.enabled = true;
        }
    }

    public bool IsCompleted => _completed;
}
