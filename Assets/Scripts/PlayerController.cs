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

    #region Health //ระบบเลือดของผู้เล่น
    [Header("Health")]
    [SerializeField] private int maxHP = 100;
    private int currentHP;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    // ระยะเวลาที่ผู้เล่นจะไม่สามารถโดนโจมตีซ้ำได้หลังจากโดนโจมตีครั้งล่าสุด (วินาที)
    [SerializeField] private float invincibilityDuration = 0.5f;
    private float invincibleUntil = 0f;
    private bool isDead = false;
    #endregion

    #region Trade Items (เก็บของ 2 ชิ้น -> แลกที่ต้นไม้เพื่อเพิ่มเลือด)
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

    #region Summon (เก็บไอเทมจากมอน -> กดใช้เรียกพระออกมาช่วยยิง)
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
        UpdateFacingDirection();
        HandleShootInput();
        HandleSummonInput();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
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

    private void UpdateFacingDirection()
    {
        if (moveInput == Vector2.zero) return; // ไม่ได้ขยับ -> คงทิศเดิมไว้ ไม่รีเซ็ต

        bool right = moveInput.x > 0f;
        bool left = moveInput.x < 0f;
        bool up = moveInput.y > 0f;
        bool down = moveInput.y < 0f;

        if (up && right) CurrentFacing = FacingDirection.UpRight;
        else if (up && left) CurrentFacing = FacingDirection.UpLeft;
        else if (down && right) CurrentFacing = FacingDirection.DownRight;
        else if (down && left) CurrentFacing = FacingDirection.DownLeft;
        else if (up) CurrentFacing = FacingDirection.Up;
        else if (down) CurrentFacing = FacingDirection.Down;
        else if (left) CurrentFacing = FacingDirection.Left;
        else if (right) CurrentFacing = FacingDirection.Right;

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