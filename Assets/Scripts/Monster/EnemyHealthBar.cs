using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Scrollbar hpScrollbar;
    [SerializeField] private Vector3 localOffset = new Vector3(0, 1.75f, 0);

    private void Start()
    {
        transform.localPosition = localOffset;
    }

    private void LateUpdate()
    {
        transform.localPosition = localOffset;
        transform.rotation = Quaternion.identity;
    }

    public void SetHP(float currentHP, float maxHP)
    {
        if (hpScrollbar != null)
        {
            float normalized = (maxHP > 0f) ? Mathf.Clamp01(currentHP / maxHP) : 0f;
            hpScrollbar.size = normalized;
            hpScrollbar.value = normalized;
        }
    }
}