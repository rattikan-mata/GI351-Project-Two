using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public enum FacingDirection { Down, Up, Left, Right, DownLeft, DownRight, UpLeft, UpRight }

    #region Movement
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

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

    #region Melee Attack (ตีธรรมดาเเบบ Zomboid: ผลัก + สตันมอน)
    [Header("Melee Attack")]
    [SerializeField] private KeyCode meleeKey = KeyCode.Space; //ปุ่มตีธรรมดา (ปรับได้)
    [SerializeField] private bool allowMouseAttack = true;     //เปิด/ปิดให้คลิกซ้ายตีได้ด้วย
    [SerializeField] private float meleeRange = 1.2f;          //ระยะการตี
    [SerializeField] private int meleeDamage = 10;             //ดาเมจการตี
    [SerializeField] private float meleeCooldown = 0.5f;       //cooldown ระหว่างการตีแต่ละครั้ง
    [SerializeField] private float meleeKnockbackForce = 6f;   //ระยะ/แรงที่มอนกระเดนจากการผลัก (ปรับได้)
    [SerializeField] private float meleeStunDuration = 1.5f;   //เวลาสตันมอน (ปรับได้)
    [SerializeField] private LayerMask monsterLayer;           //Layer ของมอนที่จะโดนตี

    private float lastMeleeTime = -999f;
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
        HandleMeleeInput();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isKnockedBack) return; //กำลังโดนผลักกระเดนอยู่ -> ไม่รับ input การเดินปกติ
        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
    }

    // วาดวงกลมระยะ Melee Attack ในหน้าต่าง Scene View
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 facing = Application.isPlaying ? GetFacingVector() : Vector2.down;
        Vector2 center = (Vector2)transform.position + facing * (meleeRange * 0.5f);

        Gizmos.DrawWireSphere(center, meleeRange);
    }
    #endregion

    #region Movement Logic
    private void ReadMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // เดินทแยงได้
        moveInput = new Vector2(h, v);
        currentVelocity = moveInput.normalized * moveSpeed;
    }

    private void UpdateFacingToMouse()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;

        if (dir == Vector2.zero) return; //เมาส์ทับตัวผู้เล่นพอดี -> คงทิศเดิมไว้

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

    #region Melee Logic
    //ตรวจปุ่มตี (Space หรือคลิกซ้าย) -> ตีมอนที่อยู่ในระยะด้านหน้า ผลักกระเดนเเละสตัน
    private void HandleMeleeInput()
    {
        bool pressed = Input.GetKeyDown(meleeKey) || (allowMouseAttack && Input.GetMouseButtonDown(0));
        if (!pressed) return;
        if (Time.time - lastMeleeTime < meleeCooldown) return;

        lastMeleeTime = Time.time;

        Vector2 origin = (Vector2)transform.position + GetFacingVector() * (meleeRange * 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, meleeRange, monsterLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Monster>(out var monster))
            {
                monster.TakeDamage(meleeDamage);

                Vector2 knockDir = ((Vector2)monster.transform.position - (Vector2)transform.position).normalized;
                monster.ApplyKnockback(knockDir, meleeKnockbackForce);
                monster.ApplyStun(meleeStunDuration);
            }
        }
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