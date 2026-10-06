using UnityEngine;

public class BreakablePot : MonoBehaviour, IDamageable
{
    [Header("Pot Stats")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Drop Settings")]
    [Tooltip("รายชื่อ ItemData ที่ไหใบนี้มีโอกาสสุ่มดรอป")]
    [SerializeField] private ItemData[] possibleDrops;

    [Tooltip("Prefab ของ WorldItem สำหรับสร้างไอเทมบนพื้น (ลาก WorldItem Prefab มาใส่)")]
    [SerializeField] private GameObject worldItemPrefab;

    [Header("Effect & Sound")]
    [Tooltip("เอฟเฟกต์ตอนไหแตก (ถ้ามี)")]
    [SerializeField] private GameObject breakEffectPrefab;
    [Tooltip("ชื่อเสียงตอนไหแตก")]
    [SerializeField] private string breakSoundId = "pot_break";

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // ฟังก์ชันรับดาเมจตามระบบ IDamageable ของเกม
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            BreakPot();
        }
    }

    private void BreakPot()
    {
        // 1. เล่นเสียงไหแตก
        if (!string.IsNullOrEmpty(breakSoundId))
        {
            AudioManager.Instance?.PlaySFXAtPoint(breakSoundId, transform.position);
        }

        // 2. สร้างเอฟเฟกต์ไหแตก (ถ้ามี)
        if (breakEffectPrefab != null)
        {
            GameObject fx = Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1.5f);
        }

        // 3. สุ่มดรอปไอเทม (ถ้ามีรายการไอเทมและมี Prefab รองรับ)
        if (possibleDrops != null && possibleDrops.Length > 0 && worldItemPrefab != null)
        {
            // สุ่มเลือกไอเทม 1 ชนิดจากลิสต์
            ItemData droppedData = possibleDrops[Random.Range(0, possibleDrops.Length)];
            if (droppedData != null)
            {
                // สปอน WorldItem ออกมาที่ตำแหน่งของไห
                GameObject itemObj = Instantiate(worldItemPrefab, transform.position, Quaternion.identity);
                if (itemObj.TryGetComponent<WorldItem>(out var worldItem))
                {
                    // เซ็ตข้อมูลไอเทมและค่าความทนทานเต็มให้ WorldItem
                    worldItem.Setup(droppedData);
                }
            }
        }

        // 4. ทำลายตัวไหทิ้ง
        Destroy(gameObject);
    }
}