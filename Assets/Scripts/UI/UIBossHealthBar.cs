using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBossHealthBar : MonoBehaviour
{
    public static UIBossHealthBar Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject bossBarRoot;
    [SerializeField] private Scrollbar bossHpScrollbar;
    [SerializeField] private TextMeshProUGUI bossNameText;

    private BossController currentTrackedBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (bossBarRoot != null)
        {
            bossBarRoot.SetActive(false);
        }
    }

    private void Update()
    {
        // คอยดึงค่าเลือดของบอสตัวที่กำลังติดตามอยู่ตลอดเวลาทุกเฟรม
        if (currentTrackedBoss != null)
        {
            if (currentTrackedBoss.gameObject.activeInHierarchy && currentTrackedBoss.CurrentHP > 0)
            {
                UpdateBar(currentTrackedBoss.CurrentHP, currentTrackedBoss.MaxHP);
            }
            else
            {
                HideBossBar(currentTrackedBoss);
            }
        }
    }

    public void ShowBossBar(BossController boss, string bossName)
    {
        currentTrackedBoss = boss;
        if (bossBarRoot != null) bossBarRoot.SetActive(true);
        if (bossNameText != null) bossNameText.text = bossName;

        UpdateBar(boss.CurrentHP, boss.MaxHP);
    }

    public void HideBossBar(BossController boss)
    {
        if (currentTrackedBoss == boss)
        {
            currentTrackedBoss = null;
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
        }
    }

    // ฟังก์ชันสั่งอัปเดตจากภายนอกทันทีเมื่อบอสโดนดาเมจ
    public void OnBossTakeDamage(BossController boss)
    {
        if (currentTrackedBoss == boss)
        {
            UpdateBar(boss.CurrentHP, boss.MaxHP);
        }
    }

    private void UpdateBar(float currentHP, float maxHP)
    {
        if (bossHpScrollbar != null)
        {
            float fill = maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f;
            bossHpScrollbar.direction = Scrollbar.Direction.LeftToRight;
            bossHpScrollbar.value = 0f;
            bossHpScrollbar.size = fill;
        }
    }
}