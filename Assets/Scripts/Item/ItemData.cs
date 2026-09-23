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
        MeleeSpin,      // มีดพร้า - ดาบหมุนวนรอบตัวต่อเนื่องหลายวินาที (มีความคงทน)
        ShotgunArc,     // ข้าวสารเสก - สาดกระจายเป็นมุมหลายเวฟติดๆ กัน หันหน้าไม่ได้ระหว่างสาด (มีความคงทน)
        SniperShot,     // หนังสติ๊ก - ยิงลูกระเบิดตรงไปข้างหน้า ชนแล้วระเบิด + มีกระสุนกระจาย (มีความคงทน)
        HealOverTime,   // หงส์เขียว - ฮีลค่อยๆ ทยอยขึ้น (มีความคงทน)
        MeleePunch      // หมัดพระ - ตีตรงหน้า โดนตัวเดียว ผลักกระเด็น ไม่มีสตัน (มีความคงทน)
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
    [Tooltip("cooldown ระหว่างการใช้แต่ละครั้ง (นับจากตอนกดใช้)")]
    public float useCooldown = 0.5f;

    [Tooltip("ดาเมจ: มีดพร้า = ดาเมจต่อการโดน 1 ครั้ง / ข้าวสาร = ดาเมจต่อเม็ด / หนังสติ๊ก = ดาเมจระเบิด / หมัดพระ = ดาเมจหมัด")]
    public int damage = 10;

    [Tooltip("แรงผลักมอนกระเด็น")]
    public float knockbackForce = 6f;

    [Tooltip("เวลาสตันมอน (0 = ไม่สตัน) หมัดพระไม่ใช้ค่านี้")]
    public float stunDuration = 0f;
    #endregion

    #region Melee Punch (หมัดพระ)
    [Header("Melee Punch (หมัดพระ)")]
    [Tooltip("ระยะตีตรงหน้า")]
    public float meleeRange = 1.2f;

    [Tooltip("จำนวนมอนสูงสุดที่หมัดหนึ่งครั้งตีโดนได้ (1 = ตัวเดียว ไม่มีดาเมจหมู่ / เลือกตัวที่ใกล้ผู้เล่นที่สุด)")]
    public int punchMaxTargets = 1;
    #endregion

    #region Spin Blades (มีดพร้า - สไตล์อัลติ Omar)
    [Header("Spin Blades (มีดพร้า - ดาบหมุนรอบตัว)")]
    [Tooltip("ระยะห่างของดาบจากตัวผู้เล่น (รัศมีวงโคจร)")]
    public float spinRadius = 2f;

    [Tooltip("ระยะเวลาที่ดาบหมุนอยู่ (วินาที)")]
    public float spinDuration = 5f;

    [Tooltip("ความเร็วหมุน (องศาต่อวินาที) 360 = หมุนรอบละ 1 วินาที")]
    public float spinRotationSpeed = 360f;

    [Tooltip("จำนวนดาบที่หมุนอยู่ (กระจายเท่าๆ กันรอบตัว)")]
    public int spinBladeCount = 3;

    [Tooltip("รัศมีตรวจจับของดาบแต่ละเล่ม (ยิ่งใหญ่ยิ่งตีโดนง่าย)")]
    public float spinBladeHitRadius = 0.5f;

    [Tooltip("มอนตัวเดิมจะโดนตีซ้ำได้ทุกกี่วินาที")]
    public float spinHitInterval = 0.5f;

    [Tooltip("(ไม่บังคับ) Prefab ภาพดาบ 1 เล่ม ถ้าไม่ใส่จะไม่มีภาพ แต่ยังตีโดนตามปกติ")]
    public GameObject spinBladePrefab;
    #endregion

    #region Shotgun Arc (ข้าวสารเสก - สไตล์อัลติ Capheny)
    [Header("Shotgun Arc (ข้าวสารเสก - สาดหลายเวฟ)")]
    [Tooltip("ใช้ Prefab ที่มี Projectile.cs")]
    public GameObject arcProjectilePrefab;
    public float arcProjectileSpeed = 10f;

    [Tooltip("มุมกระจายทั้งหมดของ 1 เวฟ (องศา)")]
    public float arcAngle = 45f;

    [Tooltip("จำนวนกระสุนต่อ 1 เวฟ")]
    public int pelletCount = 5;

    [Tooltip("ระยะสูงสุดของกระสุนแต่ละนัด")]
    public float arcRange = 6f;

    [Tooltip("สาดกี่เวฟต่อวินาที (2 = ปาติดๆ กัน 2 ทีต่อวิ)")]
    public float arcWavesPerSecond = 2f;

    [Tooltip("ระยะเวลาสาดทั้งหมด (วินาที) จำนวนเวฟรวม = เวฟต่อวินาที x ระยะเวลา (2 x 2 = 4 เวฟ)")]
    public float arcCastDuration = 2f;

    [Tooltip("ติ๊กไว้ = ระหว่างสาดหันหน้าไม่ได้ (ทิศล็อกตามตอนกดใช้)")]
    public bool arcLockFacing = true;

    [Tooltip("ติ๊กไว้ = ระหว่างสาดเดินไม่ได้ด้วย")]
    public bool arcLockMovement = false;
    #endregion

    #region Sniper Shot (หนังสติ๊ก - ยิงลูกระเบิด)
    [Header("Sniper Shot (หนังสติ๊ก - ลูกระเบิด)")]
    [Tooltip("ใช้ Prefab ที่มี GrenadeProjectile.cs (ถ้าใส่ Prefab ที่มีแค่ Projectile.cs จะยิงตรงธรรมดาแบบเดิม)")]
    public GameObject sniperProjectilePrefab;
    public float sniperSpeed = 20f;

    [Tooltip("ระยะบินสูงสุดของลูกระเบิด")]
    public float sniperRange = 15f;

    [Header("Grenade Explosion (ระเบิด)")]
    [Tooltip("รัศมีระเบิด (ดาเมจระเบิดใช้ค่า Damage ด้านบน)")]
    public float explosionRadius = 2f;

    [Tooltip("ติ๊กไว้ = บินสุดระยะแล้วระเบิดเองด้วย / ไม่ติ๊ก = หายไปเฉยๆ ถ้าไม่ชนอะไร")]
    public bool explodeAtMaxRange = true;

    [Tooltip("(ไม่บังคับ) เอฟเฟกต์ระเบิด")]
    public GameObject explosionEffectPrefab;

    [Tooltip("เอฟเฟกต์ระเบิดอยู่กี่วินาทีก่อนถูกลบ")]
    public float explosionEffectLifetime = 1.5f;

    [Header("Grenade Fragments (กระสุนกระจายหลังระเบิด)")]
    [Tooltip("ใช้ Prefab ที่มี Projectile.cs")]
    public GameObject fragmentPrefab;

    [Tooltip("จำนวนกระสุนกระจาย (ยิงรอบทิศ 360 องศาเท่าๆ กัน)")]
    public int fragmentCount = 8;

    public float fragmentSpeed = 10f;
    public float fragmentRange = 4f;
    public int fragmentDamage = 5;

    [Tooltip("หมุนมุมเริ่มต้นของกระสุนกระจาย (องศา)")]
    public float fragmentAngleOffset = 0f;

    [Tooltip("ระยะที่กระสุนกระจายเกิดห่างจากจุดระเบิด")]
    public float fragmentSpawnOffset = 0.2f;

    [Tooltip("ติ๊กไว้ = กระสุนกระจายไม่โดนตัวที่โดนลูกระเบิดชนตรงๆ ซ้ำ (กันมอนตัวนั้นโดนดาเมจซ้อนหลายเม็ด)")]
    public bool fragmentsIgnoreDirectTarget = true;
    #endregion

    #region Heal Over Time (หงส์เขียว)
    [Header("Heal Over Time (หงส์เขียว)")]
    public int healTotalAmount = 30;      // เลือดรวมที่จะฮีลทั้งหมด
    public float healDuration = 5f;       // ระยะเวลาที่ใช้ฮีลจนครบ
    public float healTickInterval = 0.5f; // ฮีลทีละนิดทุกกี่วินาที
    #endregion
}