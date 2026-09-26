using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public enum FacingDirection { Down, Up, Left, Right, DownLeft, DownRight, UpLeft, UpRight }

    #region Movement
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    // ใช้สลับสไปรต์ตัวผู้เล่นทั้งตัวเป็นท่าโจมตี (แบบ Terraria) ตอนกดใช้ไอเทม แล้วสลับกลับเองอัตโนมัติ
    private Coroutine attackSpriteCoroutine;

    // สถานะ "ก่อนเริ่มโจมตีจริงๆ" จำไว้แค่ครั้งเดียวตอนไม่มี attack sprite ค้างอยู่ (กันบัคตอนกดรัว
    // ที่เดิมจะไปจำสถานะที่ถูกท่าโจมตีเขียนทับไปแล้วซ้ำๆ จนคืนค่าผิดตอนจบ)
    private bool animatorWasEnabledBeforeAttack = true;
    private Sprite spriteBeforeAnyAttack;

    public FacingDirection CurrentFacing { get; private set; } = FacingDirection.Down;
    #endregion

    #region Shooting / Ammo
    [Header("Shooting")]
    [Tooltip("ปุ่มที่ใช้ยิง Projectile")]
    [SerializeField] private KeyCode fireKey = KeyCode.Space;//ปุ่มยิงกระสุน

    [Tooltip("ลาก Prefab ของกระสุนข้าวสารมาใส่")]
    [SerializeField] private GameObject projectilePrefab;//กระสุนที่ยิงออกไป

    [Tooltip("จุดที่กระสุนจะถูกยิงออกมา (ลาก Empty child ตำแหน่งหน้าผู้เล่นมาใส่ ถ้าไม่ใส่จะยิงจากตัวผู้เล่นเอง)")]
    [SerializeField] private Transform firePoint;//จุดยิง

    [SerializeField] private float firePointOffset = 0.5f;//ระยะห่างของจุดยิงจากตัวผู้เล่น

    [SerializeField] private float projectileSpeed = 8f;//ความเร็วของกระสุน
    [SerializeField] private int projectileDamage = 10;//ดาเมจของกระสุน
    [SerializeField] private float projectileRange = 10f;//ระยะสูงสุดที่กระสุนจะพุ่งไปก่อนหาย

    [SerializeField] private float fireCooldown = 0.3f; //cooldown ของการยิงกระสุน
    private float lastFireTime = -999f;

    // จำนวนกระสุนข้าวสารที่ถืออยู่ตอนนี้ -> ระบบเก็บกระสุน
    [Header("Ammo")]
    [SerializeField] private int currentAmmo = 0;
    public int CurrentAmmo => currentAmmo;
    public bool HasAmmo => currentAmmo > 0;
    #endregion

    #region Health
    [Header("Health")]
    [SerializeField] private int maxHP = 20;
    private int currentHP;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    // ระยะเวลาที่ผู้เล่นจะไม่สามารถโดนโจมตีซ้ำได้หลังจากโดนโจมตีครั้งล่าสุด (วินาที)
    [SerializeField] private float invincibilityDuration = 0.5f;
    private float invincibleUntil = 0f;
    private bool isDead = false;
    #endregion

    #region Trade Items
    [Header("Trade Items")]
    [SerializeField] private int tradeItemCount = 0;
    public int TradeItemCount => tradeItemCount;

    public void AddTradeItem(int amount)
    {
        tradeItemCount += amount;
    }

    public bool SpendTradeItems(int amount)
    {
        if (tradeItemCount < amount) return false;
        tradeItemCount -= amount;
        return true;
    }
    #endregion

    #region Inventory (4 ช่อง: F เก็บ/สลับ, G โยนทิ้ง, ปุ่มใช้ Item ปรับได้)
    [Header("Inventory")]
    [Tooltip("ปุ่มเก็บของจากพื้น ถ้ากระเป๋าเต็มจะสลับของในช่องที่เลือกอยู่กับของบนพื้นแทน")]
    [SerializeField] private KeyCode pickupKey = KeyCode.F;

    [Tooltip("ปุ่มโยนของในช่องที่เลือกอยู่ทิ้งลงพื้น")]
    [SerializeField] private KeyCode dropKey = KeyCode.G;

    [Tooltip("ปุ่มใช้ไอเทมในช่องที่เลือกอยู่ (แทนที่ปุ่มตีธรรมดาเดิม)")]
    [SerializeField] private KeyCode useItemKey = KeyCode.Space;

    [Tooltip("เปิด/ปิดให้คลิกซ้ายใช้ไอเทมได้ด้วย (นอกเหนือจาก Use Item Key ด้านบน)")]
    [SerializeField] private bool allowMouseUseItem = true;

    [Tooltip("ระยะที่กด F แล้วจะเก็บของบนพื้นได้")]
    [SerializeField] private float pickupRange = 1f;

    [Tooltip("Layer ของวัตถุ WorldItem บนพื้น")]
    [SerializeField] private LayerMask groundItemLayer;

    [Tooltip("ลาก Prefab เปล่าที่มีแค่ WorldItem.cs ไว้ ใช้ตอน Instantiate ของที่โยนทิ้ง (กด G)")]
    [SerializeField] private GameObject worldItemPrefab;

    [Tooltip("Layer ของมอนสเตอร์ ใช้กับไอเทมประเภท Melee/Spin (มีดพร้า/หมัดพระ)")]
    [SerializeField] private LayerMask monsterLayer;

    //ช่องไอเทมแต่ละช่อง เก็บทั้งตัว ItemData และความคงทนที่เหลืออยู่แยกจาก asset จริง
    [System.Serializable]
    public class InventorySlotData
    {
        public ItemData item;
        public int currentDurability;
    }

    private const int InventorySize = 4;
    private InventorySlotData[] inventorySlots;
    private int activeSlotIndex = 0;
    private float lastItemUseTime = -999f;

    // สถานะของสกิลที่ทำงานต่อเนื่อง (มีดพร้าหมุน / ข้าวสารสาดหลายเวฟ)
    private bool isSpinActive = false;       // มีดพร้ากำลังหมุนอยู่ -> กดซ้ำไม่ได้
    private bool isRiceCastActive = false;   // ข้าวสารกำลังสาดอยู่ -> กดซ้ำไม่ได้
    private bool facingLocked = false;       // ล็อกทิศหันหน้า (ข้าวสาร: หันหน้าไม่ได้ระหว่างสาด)
    private bool movementLocked = false;     // ล็อกการเดิน (เปิด/ปิดได้ใน ItemData)

    public InventorySlotData[] InventorySlots => inventorySlots; // ให้ UI กระเป๋าไปอ่านค่าต่อยอดทีหลังได้
    public int ActiveSlotIndex => activeSlotIndex;

    /// <summary>สลับช่องที่เลือกอยู่ด้วยปุ่มเลข 1-4 หรือเลื่อนสกอร์ลเมาส์แบบ Minecraft (วนกลับไปช่องแรก/ช่องสุดท้ายได้)</summary>
    private void HandleSlotSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) activeSlotIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) activeSlotIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) activeSlotIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) activeSlotIndex = 3;

        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            // เลื่อนขึ้น -> ไปช่องก่อนหน้า (วนกลับไปช่องสุดท้ายถ้าอยู่ช่องแรก)
            activeSlotIndex = (activeSlotIndex - 1 + InventorySize) % InventorySize;
        }
        else if (scroll < 0f)
        {
            // เลื่อนลง -> ไปช่องถัดไป (วนกลับไปช่องแรกถ้าอยู่ช่องสุดท้าย)
            activeSlotIndex = (activeSlotIndex + 1) % InventorySize;
        }
    }

    /// <summary>กด F: เก็บของบนพื้นที่ใกล้ที่สุด ถ้ากระเป๋ามีช่องว่างจะเก็บเข้าช่องว่างนั้น ถ้าเต็มจะสลับกับช่องที่เลือกอยู่</summary>
    private void HandlePickupInput()
    {
        if (!Input.GetKeyDown(pickupKey)) return;

        WorldItem nearest = FindNearestGroundItem();
        if (nearest == null || nearest.Data == null) return; // ไม่มีของให้เก็บแถวนี้

        int emptySlot = FindEmptySlot();
        if (emptySlot != -1)
        {
            // มีช่องว่าง -> เก็บเข้าช่องว่างเลย (พร้อมความคงทนที่เหลืออยู่) แล้วลบของบนพื้นทิ้ง
            inventorySlots[emptySlot].item = nearest.Data;
            inventorySlots[emptySlot].currentDurability = nearest.CurrentDurability;

            // Debug: โชว์ว่าหยิบอะไรมา เข้าช่องไหน durability เท่าไร
            Debug.Log($"[Player] หยิบไอเทม: {nearest.Data.itemName} (itemType = {nearest.Data.itemType}) เข้าช่อง {emptySlot + 1} durability = {nearest.CurrentDurability}/{nearest.Data.maxDurability}");

            Destroy(nearest.gameObject);
        }
        else
        {
            // กระเป๋าเต็ม -> สลับของในช่องที่เลือกอยู่ กับของบนพื้น (ใช้ WorldItem ตัวเดิม แค่เปลี่ยนข้อมูลข้างใน)
            InventorySlotData activeSlot = inventorySlots[activeSlotIndex];

            ItemData pickedUpItem = nearest.Data;
            int pickedUpDurability = nearest.CurrentDurability;

            ItemData itemToLeaveOnGround = activeSlot.item;
            int durabilityToLeaveOnGround = activeSlot.currentDurability;

            activeSlot.item = pickedUpItem;
            activeSlot.currentDurability = pickedUpDurability;

            nearest.Setup(itemToLeaveOnGround, durabilityToLeaveOnGround);

            // Debug: กระเป๋าเต็ม -> โชว์ว่าสลับอะไรกับอะไร ช่องไหน
            string leftName = itemToLeaveOnGround != null ? itemToLeaveOnGround.itemName : "(ช่องว่าง)";
            Debug.Log($"[Player] กระเป๋าเต็ม สลับของช่อง {activeSlotIndex + 1}: หยิบ {pickedUpItem.itemName} (itemType = {pickedUpItem.itemType}) durability {pickedUpDurability}/{pickedUpItem.maxDurability} ขึ้นมา, วาง {leftName} ทิ้งไว้แทน");
        }
    }

    /// <summary>กด G: โยนของในช่องที่เลือกอยู่ทิ้งลงพื้น ณ ตำแหน่งผู้เล่น (คงความคงทนที่เหลืออยู่ไว้)</summary>
    private void HandleDropInput()
    {
        if (!Input.GetKeyDown(dropKey)) return;

        InventorySlotData activeSlot = inventorySlots[activeSlotIndex];
        if (activeSlot.item == null) return; // ช่องว่างอยู่ ไม่มีอะไรให้โยน
        if (worldItemPrefab == null) return;

        ItemData itemToDrop = activeSlot.item;
        int durabilityToDrop = activeSlot.currentDurability;

        activeSlot.item = null;
        activeSlot.currentDurability = 0;

        GameObject droppedObj = Instantiate(worldItemPrefab, transform.position, Quaternion.identity);
        if (droppedObj.TryGetComponent<WorldItem>(out var worldItem))
        {
            worldItem.Setup(itemToDrop, durabilityToDrop);
        }
    }

    /// <summary>
    /// กดปุ่มใช้ Item: ใช้ของในช่องที่เลือกอยู่ตามชนิดไอเทม
    /// ไอเทมที่มีความคงทน (maxDurability > 0) จะหักความคงทนแล้วใช้ต่อได้จนกว่าจะพัง
    /// ไอเทมที่ไม่มีความคงทน (Ammo/HealPotion/TradeItem/SummonToken) จะหายไปจากกระเป๋าทันทีหลังใช้ 1 ครั้ง
    /// </summary>
    private void HandleUseItemInput()
    {
        bool pressed = Input.GetKeyDown(useItemKey) || (allowMouseUseItem && Input.GetMouseButtonDown(0));
        if (!pressed) return;

        InventorySlotData activeSlot = inventorySlots[activeSlotIndex];
        ItemData item = activeSlot.item;
        if (item == null) return; // ช่องว่าง ไม่มีอะไรให้ใช้

        if (Time.time - lastItemUseTime < item.useCooldown) return;

        // ใช้ไม่สำเร็จ (เช่น สกิลเดิมยังทำงานอยู่ / ไม่มี Prefab) -> ไม่เสียความคงทน และไม่เข้าคูลดาวน์
        if (!ApplyItemEffect(item)) return;
        lastItemUseTime = Time.time;

        if (item.maxDurability > 0)
        {
            // ไอเทมมีความคงทน -> หักออก ใช้ต่อได้จนกว่าจะพัง (0 = ไม่มีวันพัง)
            if (item.durabilityLossPerUse > 0)
            {
                activeSlot.currentDurability -= item.durabilityLossPerUse;

                if (activeSlot.currentDurability <= 0)
                {
                    Debug.Log($"[Player] {item.itemName} พังแล้ว!");
                    activeSlot.item = null;
                    activeSlot.currentDurability = 0;
                }
            }
        }
        else
        {
            // ของใช้ครั้งเดียวหมด (Ammo/HealPotion/TradeItem/SummonToken)
            activeSlot.item = null;
            activeSlot.currentDurability = 0;
        }
    }

    private bool ApplyItemEffect(ItemData item)
    {
        // Debug: โชว์ทุกครั้งที่กดใช้ไอเทม ว่าเป็นไอเทมอะไร ตั้ง itemType เป็นอะไร ช่วยจับกรณีตั้ง itemType ผิดใน asset
        Debug.Log($"[Player] กดใช้ไอเทม: {item.itemName} (itemType = {item.itemType})");

        bool used = true;

        switch (item.itemType)
        {
            case ItemData.ItemType.Ammo:
                AddAmmo(item.amount);
                break;
            case ItemData.ItemType.HealPotion:
                Heal(item.amount);
                break;
            case ItemData.ItemType.TradeItem:
                AddTradeItem(item.amount);
                break;
            case ItemData.ItemType.SummonToken:
                if (item.summonPrefab != null)
                {
                    Instantiate(item.summonPrefab, (Vector2)transform.position + item.summonOffset, Quaternion.identity);
                }
                break;
            case ItemData.ItemType.MeleeSpin:
                used = UseMeleeSpin(item);
                break;
            case ItemData.ItemType.ShotgunArc:
                used = UseShotgunArc(item);
                break;
            case ItemData.ItemType.SniperShot:
                UseSniperShot(item);
                break;
            case ItemData.ItemType.MeleePunch:
                UseMeleePunch(item);
                break;
            case ItemData.ItemType.HealOverTime:
                UseHealOverTime(item);
                break;
        }

        // สลับสไปรต์ตัวผู้เล่นเป็นท่าโจมตีเฉพาะตอนที่ใช้ "สำเร็จจริง" เท่านั้น
        // (สำคัญ: ถ้าเรียกไว้ก่อนเช็คผลลัพธ์ สแปมกดตอนสกิลเดิมยังทำงานอยู่ เช่น มีดพร้ากำลังหมุน/ข้าวสารกำลังสาด
        // จะไปรีสตาร์ตนับเวลาโชว์สไปรต์ใหม่ทุกครั้งที่กด ทำให้สไปรต์ไม่มีวันกลับเป็นปกติ)
        if (used)
        {
            PlayAttackSpriteIfAny(item);
        }

        return used;
    }

    private WorldItem FindNearestGroundItem()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRange, groundItemLayer);
        WorldItem nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<WorldItem>(out var worldItem))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = worldItem;
                }
            }
        }
        return nearest;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].item == null) return i;
        }
        return -1;
    }

    /// <summary>
    /// สลับสไปรต์ตัวผู้เล่นทั้งตัวเป็นท่าโจมตี (เช่น ท่าถือมีดพร้า) ชั่วคราวแบบ Terraria
    /// ถ้า item.attackSprite ไม่ได้ตั้งไว้ จะไม่ทำอะไรเลย (ใช้สไปรต์ปกติของ Animator ต่อไปตามเดิม)
    /// </summary>
    private void PlayAttackSpriteIfAny(ItemData item)
    {
        if (item.attackSprite == null || spriteRenderer == null) return;

        // จำสถานะเดิมแค่ตอน "เริ่มโจมตีจริงๆ" (ยังไม่มี attack sprite ค้างอยู่) เท่านั้น
        // กันบัคตอนกดรัว: ถ้ากดซ้ำระหว่างที่ยังโชว์ท่าโจมตีค้างอยู่ จะไม่จำสถานะที่ถูกเขียนทับไปแล้วซ้ำ
        if (attackSpriteCoroutine == null)
        {
            if (anim != null) animatorWasEnabledBeforeAttack = anim.enabled;
            spriteBeforeAnyAttack = spriteRenderer.sprite;
        }

        if (attackSpriteCoroutine != null) StopCoroutine(attackSpriteCoroutine);

        float duration = GetAttackSpriteDuration(item);
        attackSpriteCoroutine = StartCoroutine(ShowAttackSpriteRoutine(item.attackSprite, duration));
    }

    /// <summary>
    /// ระยะเวลาที่จะโชว์ Attack Sprite: ถ้า item ติ๊ก Match Attack Sprite To Effect Duration ไว้
    /// จะยืดตามเวลาที่สกิลนั้นทำงานจริง (มีดพร้า = Spin Duration / ข้าวสารเสก = Arc Cast Duration ทั้งหมด)
    /// ไม่ติ๊ก หรือเป็นชนิดอื่น -> ใช้ Attack Sprite Duration ตามปกติ
    /// </summary>
    private float GetAttackSpriteDuration(ItemData item)
    {
        if (!item.matchAttackSpriteToEffectDuration) return item.attackSpriteDuration;

        switch (item.itemType)
        {
            case ItemData.ItemType.MeleeSpin: return item.spinDuration;
            case ItemData.ItemType.ShotgunArc: return item.arcCastDuration;
            default: return item.attackSpriteDuration;
        }
    }

    private IEnumerator ShowAttackSpriteRoutine(Sprite atkSprite, float duration)
    {
        bool hasAnimator = anim != null;

        // ปิด Animator ชั่วคราว กันมันเขียนทับสไปรต์ที่เราสั่งโชว์ทุกเฟรม
        if (hasAnimator) anim.enabled = false;

        spriteRenderer.sprite = atkSprite;

        yield return new WaitForSeconds(duration);

        if (hasAnimator)
        {
            // คืนค่าตามสถานะจริงก่อนเริ่มโจมตีครั้งแรก (ไม่ใช่ค่าที่แอบเป็น false จากการกดซ้อนระหว่างทาง)
            anim.enabled = animatorWasEnabledBeforeAttack;
        }
        else
        {
            // ไม่มี Animator -> คืนสไปรต์ตัวจริงก่อนเริ่มโจมตีครั้งแรก (ไม่ใช่สไปรต์ท่าโจมตีที่ค้างจากการกดซ้อน)
            spriteRenderer.sprite = spriteBeforeAnyAttack;
        }

        attackSpriteCoroutine = null;
    }
    #endregion

    #region Summon
    [Header("Summon")]
    [SerializeField] private KeyCode summonKey = KeyCode.Q;//ปุ่มกดใช้ไอเทม summon

    private int summonItemCount = 0;
    public int SummonItemCount => summonItemCount;

    private GameObject pendingSummonPrefab;//ตัวพระที่จะเรียก (ตั้งจากไอเทมที่เก็บล่าสุด แต่ละไอเทมเรียกตัวไม่เหมือนกันได้)
    private Vector2 pendingSummonOffset = Vector2.left;//ตำแหน่งเกิดของพระเทียบจากผู้เล่น (ตั้งจากไอเทม)

    //เพิ่มไอเทม summon เข้ากระเป๋า (เรียกจาก SummonItemPickup ตอนเก็บของที่มอนดรอป)
    public void AddSummonItem(int amount, GameObject allyPrefab, Vector2 spawnOffset)
    {
        summonItemCount += amount;
        if (allyPrefab != null) pendingSummonPrefab = allyPrefab;
        pendingSummonOffset = spawnOffset;
        Debug.Log($"[Player] เก็บไอเทม summon +{amount} -> มี {summonItemCount} ชิ้น"); //debug ดูจำนวนไอเทม summon ตอนเก็บ
    }

    //กดปุ่มใช้ไอเทม summon: ถ้ามีของจะหัก 1 ชิ้นเเล้วเรียกพระออกมาตามตำแหน่งที่ไอเทมกำหนด
    private void HandleSummonInput()
    {
        if (!Input.GetKeyDown(summonKey)) return;
        if (summonItemCount <= 0) return; //ไม่มีไอเทม summon ใช้ไม่ได้
        if (pendingSummonPrefab == null) return;

        summonItemCount--;
        Instantiate(pendingSummonPrefab, (Vector2)transform.position + pendingSummonOffset, Quaternion.identity);
        Debug.Log($"[Player] ใช้ไอเทม summon -> เหลือ {summonItemCount} ชิ้น"); //debug ดูไอเทม summon ที่เหลือตอนกดใช้
    }
    #endregion

    #region Knockback
    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f; //เวลาที่ผู้เล่นถูกผลักก่อนคุมตัวเองได้อีกครั้ง

    private bool isKnockedBack = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        inventorySlots = new InventorySlotData[InventorySize];
        for (int i = 0; i < InventorySize; i++)
        {
            inventorySlots[i] = new InventorySlotData();
        }
    }

    private void Start()
    {
        currentHP = maxHP;
    }

    private void Update()
    {
        if (isDead) return;

        ReadMovementInput();
        UpdateFacingToMouse();
        HandleShootInput();
        HandleSummonInput();
        HandleSlotSelectionInput();
        HandlePickupInput();
        HandleDropInput();
        HandleUseItemInput();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isKnockedBack) return; //กำลังโดนผลักกระเดนอยู่ -> ไม่รับ input การเดินปกติ
        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
    }

    // วาดวงกลมระยะเก็บของ (Pickup Range) ในหน้าต่าง Scene View
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
    #endregion

    #region Movement Logic
    private void ReadMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // เดินทแยงได้
        moveInput = new Vector2(h, v);
        if (movementLocked) moveInput = Vector2.zero; // ล็อกการเดินระหว่างสกิล (ถ้าเปิดไว้)
        currentVelocity = moveInput.normalized * moveSpeed;

        // ++ เพิ่มบล็อกโค้ดด้านล่างนี้ เพื่อสั่งเปิด/ปิด อนิเมชั่นเดิน ++
        if (anim != null)
        {
            // sqrMagnitude > 0 หมายถึงมีการกดปุ่มทิศทางอยู่ (ความเร็วไม่เป็น 0)
            anim.SetBool("isMoving", moveInput.sqrMagnitude > 0);
        }
    }

    private void UpdateFacingToMouse()
    {
        if (Camera.main == null) return;

        // ระหว่างข้าวสารกำลังสาด (หันหน้าไม่ได้) -> คงทิศเดิมไว้ ไม่อัปเดตตามเมาส์
        if (facingLocked)
        {
            UpdateFirePointPosition();
            return;
        }

        // ตัวแปรถูกประกาศและคำนวณไว้ตรงนี้แล้ว
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;

        if (dir == Vector2.zero) return;

        //แปลงทิศเวกเตอร์เป็น 8 ทิศ (ทุกๆ 45 องศา)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        int sector = Mathf.RoundToInt(angle / 45f) % 8;
        switch (sector)
        {
            case 0: CurrentFacing = FacingDirection.Right; break;
            case 1: CurrentFacing = FacingDirection.UpRight; break;
            case 2: CurrentFacing = FacingDirection.Up; break;
            case 3: CurrentFacing = FacingDirection.UpLeft; break;
            case 4: CurrentFacing = FacingDirection.Left; break;
            case 5: CurrentFacing = FacingDirection.DownLeft; break;
            case 6: CurrentFacing = FacingDirection.Down; break;
            case 7: CurrentFacing = FacingDirection.DownRight; break;
        }

        UpdateFirePointPosition();

        // ++ นำค่า dir ที่คำนวณไว้ด้านบนมาใช้ได้เลย ไม่ต้องประกาศใหม่ ++
        if (spriteRenderer != null)
        {
            // ถ้าเมาส์อยู่ฝั่งซ้าย (แกน x ติดลบ) ให้พลิกภาพ
            spriteRenderer.flipX = dir.x < 0;
        }
    }


    //ย้ายจุดยิงไปตามทิศที่ผู้เล่นหันอยู่ จะได้ยิงออกจากด้านหน้าเสมอ
    private void UpdateFirePointPosition()
    {
        if (firePoint == null) return;
        firePoint.position = (Vector2)transform.position + GetFacingVector() * firePointOffset;
    }

    private Vector2 GetFacingVector()
    {
        switch (CurrentFacing)
        {
            case FacingDirection.Up: return Vector2.up;
            case FacingDirection.Down: return Vector2.down;
            case FacingDirection.Left: return Vector2.left;
            case FacingDirection.Right: return Vector2.right;
            case FacingDirection.UpLeft: return new Vector2(-1f, 1f).normalized;
            case FacingDirection.UpRight: return new Vector2(1f, 1f).normalized;
            case FacingDirection.DownLeft: return new Vector2(-1f, -1f).normalized;
            case FacingDirection.DownRight: return new Vector2(1f, -1f).normalized;
            default: return Vector2.down;
        }
    }
    #endregion

    #region Shooting Logic
    private void HandleShootInput() //ตรวจสอบการกดปุ่มยิงและยิงกระสุนเเละเช็คว่ายังมีกระสุนไหม
    {
        if (!Input.GetKeyDown(fireKey)) return;
        if (Time.time - lastFireTime < fireCooldown) return;
        if (!HasAmmo) return; // ไม่มีกระสุนข้าวสาร ยิงไม่ได้
        if (projectilePrefab == null) return;

        lastFireTime = Time.time;
        currentAmmo--;

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (projObj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Init(GetFacingVector(), projectileSpeed, projectileDamage, projectileRange);
        }
    }

    // ระบบเติมกระสุนให้ผู้เล่น (สามารถเรียกจาก Item Pickup ได้)
    public void AddAmmo(int amount)
    {
        currentAmmo += amount;
        Debug.Log($"[Player] เก็บกระสุน +{amount} -> มีกระสุนทั้งหมด {currentAmmo} นัด"); //debug ดูจำนวนกระสุนตอนเก็บ
    }
    #endregion

    #region Weapon Use Logic (ตรรกะเฉพาะของอาวุธ/ไอเทมทั้ง 5 ชนิด)

    //มีดพร้า (สไตล์อัลติ Omar): ดาบหมุนวนรอบตัวต่อเนื่องหลายวินาที โดนมอนตัวไหนก็ตีตัวนั้น
    //ทุกค่าปรับได้ใน ItemData: spinDuration / spinRotationSpeed / spinBladeCount / spinRadius / spinBladeHitRadius / spinHitInterval
    private bool UseMeleeSpin(ItemData data)
    {
        if (isSpinActive) return false; // กำลังหมุนอยู่ ใช้ซ้อนไม่ได้
        StartCoroutine(SpinBladesRoutine(data));
        return true;
    }

    private IEnumerator SpinBladesRoutine(ItemData data)
    {
        isSpinActive = true;

        int bladeCount = Mathf.Max(1, data.spinBladeCount);

        // ตัวโชว์ภาพดาบ (ไม่ใส่ Prefab ก็ยังตีได้ แค่ไม่มีภาพ) ผูกเป็นลูกของผู้เล่น จะได้ไม่ค้างถ้าผู้เล่นถูกลบ
        GameObject[] visuals = new GameObject[bladeCount];
        if (data.spinBladePrefab != null)
        {
            for (int i = 0; i < bladeCount; i++)
            {
                visuals[i] = Instantiate(data.spinBladePrefab, transform.position, Quaternion.identity, transform);
            }
        }

        // จำเวลาที่ตีมอนแต่ละตัวล่าสุด เพื่อให้ตีซ้ำได้ทุก spinHitInterval วินาที (ไม่ตีรัวทุกเฟรม)
        var lastHitTime = new Dictionary<Monster, float>();

        float startTime = Time.time;
        float angle = 0f;

        while (Time.time - startTime < data.spinDuration && !isDead)
        {
            angle += data.spinRotationSpeed * Time.deltaTime;
            Vector2 center = transform.position;

            for (int i = 0; i < bladeCount; i++)
            {
                float a = angle + (360f / bladeCount) * i;
                Vector2 offset = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * data.spinRadius;
                Vector2 bladePos = center + offset;

                if (visuals[i] != null)
                {
                    visuals[i].transform.position = bladePos;
                    visuals[i].transform.rotation = Quaternion.Euler(0f, 0f, a);
                }

                Collider2D[] hits = Physics2D.OverlapCircleAll(bladePos, data.spinBladeHitRadius, monsterLayer);
                foreach (var hit in hits)
                {
                    if (!hit.TryGetComponent<Monster>(out var monster)) continue;

                    if (lastHitTime.TryGetValue(monster, out float lastTime) && Time.time - lastTime < data.spinHitInterval) continue;
                    lastHitTime[monster] = Time.time;

                    monster.TakeDamage(data.damage);

                    Vector2 dir = ((Vector2)monster.transform.position - center).normalized;
                    monster.ApplyKnockback(dir, data.knockbackForce);

                    if (data.stunDuration > 0f) monster.ApplyStun(data.stunDuration);
                }
            }

            yield return null;
        }

        for (int i = 0; i < bladeCount; i++)
        {
            if (visuals[i] != null) Destroy(visuals[i]);
        }

        isSpinActive = false;
    }

    //หมัดพระ: ตีตรงหน้าในระยะสั้น โดนได้แค่ตัวเดียว (ตัวที่ใกล้ผู้เล่นที่สุดในระยะ) ไม่มีดาเมจหมู่
    //ปรับจำนวนเป้าหมายได้ที่ ItemData.punchMaxTargets (1 = ตัวเดียว)
    private void UseMeleePunch(ItemData data)
    {
        Vector2 playerPos = transform.position;
        Vector2 origin = playerPos + GetFacingVector() * (data.meleeRange * 0.5f);

        // Debug: เห็นวงกลม hitbox จริงตอนต่อยใน Scene View เหมือนระบบของ BossController
        // (เช็คว่าระยะที่ตีโดนจริงตรงกับ Melee Range ที่ตั้งใน ItemData ไหม)
        DrawDebugCircle(origin, data.meleeRange, Color.yellow, 0.3f);

        // ภาพจริงที่เห็นในเกม (ไม่ใช่แค่ debug): สปอน Sprite hitbox ถ้าตั้ง Punch Indicator Prefab ไว้ใน ItemData
        SpawnAreaIndicator(origin, data.meleeRange, data.punchIndicatorColor, data.punchIndicatorDuration, data.punchIndicatorPrefab);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, data.meleeRange, monsterLayer);

        // รวบรวมมอนที่อยู่ในระยะ (กันซ้ำกรณีมอนตัวเดียวมีหลาย Collider)
        var targets = new List<Monster>();
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Monster>(out var monster) && !targets.Contains(monster))
            {
                targets.Add(monster);
            }
        }

        // เรียงจากใกล้ผู้เล่นที่สุดไปไกลสุด
        targets.Sort((a, b) =>
            ((Vector2)a.transform.position - playerPos).sqrMagnitude
            .CompareTo(((Vector2)b.transform.position - playerPos).sqrMagnitude));

        int count = Mathf.Min(Mathf.Max(1, data.punchMaxTargets), targets.Count);
        for (int i = 0; i < count; i++)
        {
            Monster monster = targets[i];
            monster.TakeDamage(data.damage);

            Vector2 dir = ((Vector2)monster.transform.position - playerPos).normalized;
            monster.ApplyKnockback(dir, data.knockbackForce);
            //ตั้งใจไม่เรียก ApplyStun ตรงนี้ -> หมัดพระเอาสตันออกตามที่ต้องการ
        }
    }

    // วาดวงกลมด้วย Debug.DrawLine ให้เห็น hitbox จริงตอน Play (เหมือนของ BossController.cs)
    // เห็นเฉพาะใน Scene View ตอนรัน Play Mode เท่านั้น ไม่โชว์ในเกมจริงตอน build
    private void DrawDebugCircle(Vector2 center, float radius, Color color, float duration, int segments = 24)
    {
        if (radius <= 0f) return;

        Vector3 prevPoint = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Debug.DrawLine(prevPoint, nextPoint, color, duration);
            prevPoint = nextPoint;
        }
    }

    // สปอนวัตถุโชว์ hitbox จริงชั่วคราว (เห็นในเกมจริง ไม่ใช่แค่ Debug) ปรับขนาดอัตโนมัติตามรัศมีที่ต้องการ
    // ใช้ได้กับ Sprite รูปอะไรก็ได้ (วงกลม สี่เหลี่ยม ฯลฯ) เพราะสเกลตาม sprite.bounds.size.x ให้เส้นผ่านศูนย์กลาง = radius x 2 เสมอ
    // เทคนิคเดียวกับ SpawnAreaIndicator ของ BossController.cs
    private void SpawnAreaIndicator(Vector3 position, float radius, Color tint, float duration, GameObject indicatorPrefab)
    {
        if (indicatorPrefab == null || radius <= 0f) return;

        GameObject obj = Instantiate(indicatorPrefab, position, Quaternion.identity);

        if (obj.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = tint;

            float nativeWidth = (sr.sprite != null) ? sr.sprite.bounds.size.x : 1f;
            if (nativeWidth > 0.0001f)
            {
                float scale = (radius * 2f) / nativeWidth; // รัศมี x 2 = เส้นผ่านศูนย์กลางที่ต้องการ
                obj.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else
        {
            Debug.LogWarning("[Player] Punch Indicator Prefab ไม่มี SpriteRenderer -> ปรับขนาดอัตโนมัติไม่ได้", obj);
        }

        Destroy(obj, duration);
    }

    //ข้าวสารเสก (สไตล์อัลติ Capheny): สาดเป็นเวฟๆ ต่อเนื่อง ระหว่างสาดหันหน้าไม่ได้ (ทิศล็อกตั้งแต่ตอนกด)
    //ปรับได้ใน ItemData: arcWavesPerSecond / arcCastDuration / arcAngle / pelletCount / arcRange / arcLockFacing / arcLockMovement
    private bool UseShotgunArc(ItemData data)
    {
        if (data.arcProjectilePrefab == null) return false;
        if (isRiceCastActive) return false; // กำลังสาดอยู่ ใช้ซ้อนไม่ได้

        StartCoroutine(ShotgunArcRoutine(data));
        return true;
    }

    private IEnumerator ShotgunArcRoutine(ItemData data)
    {
        isRiceCastActive = true;

        Vector2 lockedDir = GetFacingVector(); // จำทิศตอนกดใช้
        if (data.arcLockFacing) facingLocked = true;
        if (data.arcLockMovement) movementLocked = true;

        float wavesPerSecond = Mathf.Max(0.01f, data.arcWavesPerSecond);
        float interval = 1f / wavesPerSecond;
        int totalWaves = Mathf.Max(1, Mathf.RoundToInt(wavesPerSecond * data.arcCastDuration));

        // เช่น 2 เวฟ/วิ x 2 วิ = 4 เวฟ (ที่ 0, 0.5, 1.0, 1.5 วิ) แล้วปลดล็อกตอนครบ 2 วิ
        for (int w = 0; w < totalWaves && !isDead; w++)
        {
            Vector2 dir = data.arcLockFacing ? lockedDir : GetFacingVector();
            FireArcWave(data, dir);
            yield return new WaitForSeconds(interval);
        }

        facingLocked = false;
        movementLocked = false;
        isRiceCastActive = false;
    }

    // ยิง 1 เวฟ: กระสุนกระจายเป็นมุมเหมือน Shotgun
    private void FireArcWave(ItemData data, Vector2 baseDir)
    {
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        int count = Mathf.Max(1, data.pelletCount);
        float halfArc = data.arcAngle * 0.5f;
        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (float)i / (count - 1);
            float angle = baseAngle - halfArc + (data.arcAngle * t);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject projObj = Instantiate(data.arcProjectilePrefab, spawnPos, Quaternion.identity);
            if (projObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Init(dir, data.arcProjectileSpeed, data.damage, data.arcRange);
            }
        }
    }

    //หนังสติ๊ก: ยิงลูกระเบิดตรงไปข้างหน้า ชนแล้วระเบิด + มีกระสุนกระจายออกมา (ดูรายละเอียดใน GrenadeProjectile.cs)
    private void UseSniperShot(ItemData data)
    {
        if (data.sniperProjectilePrefab == null) return;

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        GameObject projObj = Instantiate(data.sniperProjectilePrefab, spawnPos, Quaternion.identity);

        if (projObj.TryGetComponent<GrenadeProjectile>(out var grenade))
        {
            grenade.Init(GetFacingVector(), data);
        }
        else if (projObj.TryGetComponent<Projectile>(out var projectile))
        {
            // เผื่อ Prefab เดิมที่ยังเป็น Projectile ธรรมดา -> ยิงตรงแบบเดิม
            projectile.Init(GetFacingVector(), data.sniperSpeed, data.damage, data.sniperRange);
        }
    }

    //หงส์เขียว: ฮีลค่อยๆ ทยอยขึ้นทีละนิด (Regen) ไม่ขึ้นทันทีทั้งก้อน
    private void UseHealOverTime(ItemData data)
    {
        // Debug: เช็คว่าค่าที่ตั้งใน ItemData asset ถูกส่งเข้ามาถูกต้องไหม (ถ้าค่าไหนเป็น 0 ทั้งที่ไม่ควร = ลืมกรอกใน Inspector)
        Debug.Log($"[Player] เริ่ม Heal Over Time: total={data.healTotalAmount}, duration={data.healDuration}s, tickInterval={data.healTickInterval}s, HP ตอนนี้ {currentHP}/{maxHP}");

        if (data.healDuration <= 0f || data.healTickInterval <= 0f || data.healTotalAmount <= 0)
        {
            Debug.LogWarning($"[Player] Heal Over Time ค่าไม่ถูกต้อง (duration/tickInterval/totalAmount เป็น 0 หรือติดลบ) -> เช็คช่อง Heal Total Amount / Heal Duration / Heal Tick Interval ใน ItemData asset ของ {data.itemName}");
        }

        StartCoroutine(HealOverTimeRoutine(data.healTotalAmount, data.healDuration, data.healTickInterval));
    }

    private IEnumerator HealOverTimeRoutine(int totalAmount, float duration, float tickInterval)
    {
        int ticks = Mathf.Max(1, Mathf.RoundToInt(duration / Mathf.Max(0.01f, tickInterval)));
        int healPerTick = Mathf.Max(1, totalAmount / ticks);

        for (int i = 0; i < ticks; i++)
        {
            Heal(healPerTick);
            // Debug: ดูทีละ tick ว่า coroutine ยังรันอยู่จริงไหม กับ HP ขึ้นเป็นขั้นๆ ตามที่ตั้งใจไหม
            Debug.Log($"[Player] Heal Over Time tick {i + 1}/{ticks}: +{healPerTick} -> HP {currentHP}/{maxHP}");
            yield return new WaitForSeconds(tickInterval);
        }

        Debug.Log("[Player] Heal Over Time จบรอบแล้ว");
    }
    #endregion

    #region Knockback Logic
    //ผู้เล่นโดนผลักกระเดน เรียกจาก Monster ตอนโดนตี
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (isDead) return;
        StopCoroutine(nameof(KnockbackRoutine));
        StartCoroutine(KnockbackRoutine(direction.normalized * force));
    }

    private IEnumerator KnockbackRoutine(Vector2 knockbackVelocity)
    {
        isKnockedBack = true;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            float t = 1f - (elapsed / knockbackDuration); //ค่อยๆ ลดแรงลงจนหยุด
            rb.MovePosition(rb.position + knockbackVelocity * t * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isKnockedBack = false;
    }
    #endregion

    #region Health Logic
    public void TakeDamage(int amount)
    {
        if (isDead || Time.time < invincibleUntil) return;

        currentHP -= amount;
        invincibleUntil = Time.time + invincibilityDuration;

        Debug.Log($"[Player] โดนดาเมจ {amount} -> เลือดเหลือ {Mathf.Max(currentHP, 0)}/{maxHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        Debug.Log($"[Player] ฮีล +{amount} -> เลือดเหลือ {currentHP}/{maxHP}"); //debug ดูเลือดหลังฮีล
    }

    private void Die()
    {
        isDead = true;
        currentVelocity = Vector2.zero;
        Debug.Log("[Player] Died.");
    }
    #endregion
}