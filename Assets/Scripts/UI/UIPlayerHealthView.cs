using UnityEngine;
using UnityEngine.UI;

public class UIPlayerHealthView : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Scrollbar playerScrollbar;

    [Header("Low Health")]
    [SerializeField] private Image vignetteImage;
    [Range(0.05f, 0.5f)]
    [SerializeField] private float lowHpThreshold = 0.3f;
    [SerializeField] private float pulseSpeed = 4f;

    private void Awake()
    {
        if (vignetteImage != null) vignetteImage.enabled = false;
    }

    public void UpdateHealthView(float currentHp, float maxHp)
    {
        if (maxHp <= 0f) return;

        float ratio = Mathf.Clamp01(currentHp / maxHp);

        if (playerScrollbar != null)
        {
            playerScrollbar.direction = Scrollbar.Direction.LeftToRight;
            playerScrollbar.value = 0f;
            playerScrollbar.size = ratio;
        }

        UpdateVignette(ratio);
    }

    private void UpdateVignette(float ratio)
    {
        if (vignetteImage == null) return;

        if (ratio <= lowHpThreshold && ratio > 0f)
        {
            float intensity = 1f - (ratio / lowHpThreshold);
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.15f, 0.65f, intensity * pulse);

            Color c = vignetteImage.color;
            c.a = alpha;
            vignetteImage.color = c;
            vignetteImage.enabled = true;
        }
        else
        {
            vignetteImage.enabled = false;
        }
    }
}