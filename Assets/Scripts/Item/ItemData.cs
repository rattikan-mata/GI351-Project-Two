using UnityEngine;

/// <summary>
/// ข้อมูลไอเทม 1 ชนิด สร้างเป็น Asset ผ่าน Assets > Create > Game > Item Data
/// ใช้ตัวเดียวกันได้ทั้งของบนพื้น (WorldItem) และของในกระเป๋า (Inventory 4 ช่องของ PlayerController)
/// ไอเทมประเภทอาวุธ (MeleeSpin/ShotgunArc/SniperShot/MeleePunch/HealOverTime) มีความคงทน ใช้ไปเรื่อยๆ จนพังได้
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public enum ItemType
    {
        Ammo,           // เติมกระสุนข้าวสาร (ใช้ครั้งเดียวหมด)
        HealPotion,     // ฮีลทันที (ใช้ครั้งเดียวหมด)
        TradeItem,      // ของแลกที่ต้นไม้ (ใช้ครั้งเดียวหมด)
        SummonToken,    // เรียกพระ (ใช้ครั้งเดียวหมด)
        MeleeSpin,      // มีดพร้า - หมุนรอบตัว ดาเมจรอบตัว (มีความคงทน)
        ShotgunArc,     // ข้าวสารเสก - ยิงกระจายเป็นมุม เหมือนช็อตกัน (มีความคงทน)
        SniperShot,     // หนังสติ๊ - ยิงตรงระยะไกล (มีความคงทน)
        HealOverTime,   // หงส์เขียว - ฮีลค่อยๆ ทยอยขึ้น (มีความคงทน)
        MeleePunch      // หมัดพระ - ตีตรงหน้า ผลักกระเดน ไม่มีสตัน (มีความคงทน)
    }

    #region Basic Info
    [Header("Basic Info")]
    public string itemName = "Item";

    [Tooltip("ไอคอนโชว์ในกระเป๋า/UI (ต่อยอดทำ UI ทีหลังได้)")]
    public Sprite icon;
    #endregion

    #region Type & Effect
    [Header("Type")]
    public ItemType itemType = ItemType.Ammo;

    [Tooltip("Ammo = จำนวนกระสุนที่เติม / HealPotion = เลือดที่ฮีล / TradeItem = จำนวนของแลกที่ได้")]
    public int amount = 1;

    [Header("SummonToken เท่านั้น")]
    [Tooltip("ตัวที่จะถูกเรียกออกมาตอนใช้ไอเทมนี้ (ใช้เฉพาะ itemType = SummonToken)")]
    public GameObject summonPrefab;
    [Tooltip("ตำแหน่งเกิดของตัวที่เรียกออกมา เทียบจากตัวผู้เล่น")]
    public Vector2 summonOffset = Vector2.left;
    #endregion

    #region Durability (เฉพาะไอเทมประเภทอาวุธ 5 ชนิด)
    [Header("Durability (ความคงทน - เฉพาะไอเทมประเภทอาวุธ)")]
    [Tooltip("ความคงทนสูงสุด ถ้าเป็น 0 = ไม่มีความคงทน ใช้ครั้งเดียวหมด (สำหรับ Ammo/HealPotion/TradeItem/SummonToken)")]
    public int maxDurability = 0;

    [Tooltip("เสียความคงทนเท่าไรต่อการใช้ 1 ครั้ง (0 = ไม่มีวันพัง)")]
    public int durabilityLossPerUse = 1;
    #endregion

    #region Common Weapon Stats
    [Header("Common Weapon Stats (ใช้ร่วมกันหลายประเภท)")]
    public float useCooldown = 0.5f;   // cooldown ระหว่างการใช้แต่ละครั้ง
    public int damage = 10;            // ดาเมจ
    public float knockbackForce = 6f;  // แรงผลักมอนกระเดน
    public float stunDuration = 0f;    // เวลาสตันมอน (0 = ไม่สตัน)
    #endregion

    #region Melee Punch / Spin (หมัดพระ / มีดพร้า)
    [Header("Melee Range (หมัดพระ ใช้ค่านี้)")]
    public float meleeRange = 1.2f;    // ระยะตีตรงหน้า

    [Header("Spin Radius (มีดพร้า ใช้ค่านี้)")]
    public float spinRadius = 2f;      // ระยะ AoE รอบตัวตอนหมุน
    #endregion

    #region Shotgun Arc (ข้าวสารเสก)
    [Header("Shotgun Arc (ข้าวสารเสก)")]
    public GameObject arcProjectilePrefab; // ใช้ Prefab ที่มี Projectile.cs
    public float arcProjectileSpeed = 10f;
    public float arcAngle = 45f;           // มุมกระจายทั้งหมด (องศา) ปรับได้
    public int pelletCount = 5;            // จำนวนกระสุนที่ยิงกระจายออกไป
    public float arcRange = 6f;            // ระยะสูงสุดของกระสุนแต่ละนัด ปรับได้
    #endregion

    #region Sniper Shot (หนังสติ๊)
    [Header("Sniper Shot (หนังสติ๊)")]
    public GameObject sniperProjectilePrefab; // ใช้ Prefab ที่มี Projectile.cs
    public float sniperSpeed = 20f;
    public float sniperRange = 15f;           // ระยะไกล ปรับได้
    #endregion

    #region Heal Over Time (หงส์เขียว)
    [Header("Heal Over Time (หงส์เขียว)")]
    public int healTotalAmount = 30;      // เลือดรวมที่จะฮีลทั้งหมด
    public float healDuration = 5f;       // ระยะเวลาที่ใช้ฮีลจนครบ
    public float healTickInterval = 0.5f; // ฮีลทีละนิดทุกกี่วินาที
    #endregion
}