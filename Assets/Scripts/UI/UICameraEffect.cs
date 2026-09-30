using System.Collections;
using UnityEngine;

public class UICameraEffect : MonoBehaviour
{
    [SerializeField] private Camera targetCam;
    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (targetCam == null) targetCam = Camera.main;
        if (targetCam != null) originalLocalPos = targetCam.transform.localPosition;
    }

    public void Shake(float duration = 3f, float magnitude = 0.25f)
    {
        if (targetCam == null) return;
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            targetCam.transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        targetCam.transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
}