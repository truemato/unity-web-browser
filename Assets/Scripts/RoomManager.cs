using UnityEngine;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    [Header("Rotation settings")]
    public float rotationAngle = 120f;
    public float rotationDuration = 5f;

    private bool _isRotating = false;
    private float _currentYAngle = 0f;

    /// <summary>
    /// Rotate the cylinder 120° with ease-in/ease-out.
    /// </summary>
    public void RotateToNext()
    {
        if (_isRotating) return;
        StartCoroutine(RotateCoroutine());
    }

    private IEnumerator RotateCoroutine()
    {
        _isRotating = true;

        float startAngle = _currentYAngle;
        float endAngle = _currentYAngle + rotationAngle;
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);

            // Ease in/out (smoothstep)
            float eased = t * t * (3f - 2f * t);

            float angle = Mathf.Lerp(startAngle, endAngle, eased);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            yield return null;
        }

        _currentYAngle = endAngle;
        transform.rotation = Quaternion.Euler(0f, _currentYAngle, 0f);
        _isRotating = false;
    }

    public bool IsRotating => _isRotating;
}
