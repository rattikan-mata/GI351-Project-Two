using System.Collections;
using UnityEngine;
using TMPro;

public class UICombatTextSpawner : MonoBehaviour
{
    [SerializeField] private GameObject combatTextPrefab;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float lifeDuration = 0.8f;

    public void Spawn(Vector3 worldPos, int amount, bool isHeal)
    {
        if (combatTextPrefab == null) return;

        GameObject textObj = Instantiate(combatTextPrefab, worldPos + Vector3.up * 1f, Quaternion.identity);
        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>();

        if (tmp != null)
        {
            tmp.text = isHeal ? $"+{amount}" : $"-{amount}";
            tmp.color = isHeal ? Color.green : Color.red;
        }

        StartCoroutine(FadeRoutine(textObj, tmp));
    }

    private IEnumerator FadeRoutine(GameObject obj, TMP_Text tmp)
    {
        float elapsed = 0f;
        Color initialColor = tmp != null ? tmp.color : Color.white;

        while (elapsed < lifeDuration)
        {
            if (obj == null) yield break;
            obj.transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            if (tmp != null)
            {
                float alpha = Mathf.Clamp01(1f - (elapsed / lifeDuration));
                tmp.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }
}