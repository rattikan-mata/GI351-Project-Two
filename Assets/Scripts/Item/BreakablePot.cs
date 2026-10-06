using UnityEngine;

public class BreakablePot : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public class DropItem
    {
        [Tooltip("ข้อมูลไอเทม")]
        public ItemData itemData;

        [Tooltip("โอกาสดรอปเทียบกับชิ้นอื่น")]
        public float dropWeight = 1f;
    }

    [Header("Pot Stats")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Drop Settings")]
    [Tooltip("รายชื่อไอเทมและเรตการดรอป")]
    [SerializeField] private DropItem[] possibleDrops;

    [Tooltip("น้ำหนักโอกาสที่จะ 'ไม่ดรอปอะไรเลย' (ค่ายิ่งเยอะ ยิ่งมีโอกาสตีแล้วไหเปล่าๆ สูง ถ้าใส่ 0 คือดรอปชัวร์ทุกใบ)")]
    [SerializeField] private float nothingDropWeight = 1f;

    [Tooltip("Prefab ของ WorldItem สำหรับสร้างไอเทมบนพื้น")]
    [SerializeField] private GameObject worldItemPrefab;

    [Header("Effect & Sound")]
    [SerializeField] private GameObject breakEffectPrefab;
    [SerializeField] private string breakSoundId = "pot_break";

    private void Awake()
    {
        currentHealth = maxHealth;
    }

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
        if (!string.IsNullOrEmpty(breakSoundId))
        {
            AudioManager.Instance?.PlaySFXAtPoint(breakSoundId, transform.position);
        }

        if (breakEffectPrefab != null)
        {
            GameObject fx = Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1.5f);
        }

        ItemData droppedData = GetRandomDrop();
        if (droppedData != null && worldItemPrefab != null)
        {
            GameObject itemObj = Instantiate(worldItemPrefab, transform.position, Quaternion.identity);
            if (itemObj.TryGetComponent<WorldItem>(out var worldItem))
            {
                worldItem.Setup(droppedData);
            }
        }

        Destroy(gameObject);
    }

    private ItemData GetRandomDrop()
    {
        if (possibleDrops == null || possibleDrops.Length == 0) return null;

        float totalWeight = Mathf.Max(0f, nothingDropWeight);
        foreach (var drop in possibleDrops)
        {
            if (drop.itemData != null && drop.dropWeight > 0f)
            {
                totalWeight += drop.dropWeight;
            }
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;

        // ถ้ารอยสุ่มตกอยู่ในช่วงของ nothingDropWeight จะคืนค่าเป็น null (ไม่ดรอปของ)
        if (roll < Mathf.Max(0f, nothingDropWeight))
        {
            return null;
        }

        float cumulative = Mathf.Max(0f, nothingDropWeight);

        foreach (var drop in possibleDrops)
        {
            if (drop.itemData == null || drop.dropWeight <= 0f) continue;

            cumulative += drop.dropWeight;
            if (roll <= cumulative)
            {
                return drop.itemData;
            }
        }

        return null;
    }
}