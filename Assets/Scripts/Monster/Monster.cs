using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Monster : MonoBehaviour, IDamageable
{
    #region Health
    [Header("Health")]
    [SerializeField] protected int maxHP = 10;
    protected int currentHP;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    protected bool isDead = false;
    #endregion

    #region Movement / Chase AI
    [Header("Movement / Chase AI")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 5f;   // ระยะมองเห็นผู้เล่น ถ้าอยู่ในระยะนี้จะเดินเข้าหา
    [SerializeField] protected float stoppingDistance = 0.6f; // ระยะห่างที่มอนหยุดกันทะลุตัว player

    protected Rigidbody2D rb;
    protected Transform playerTransform;
    #endregion

    #region Hit Feedback (Optional Flash)
    [Header("Hit Feedback (Optional)")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected float hitFlashDuration = 0.1f; //มอนตัวเเฟลชเวลาโดนตีเผื่อไว้เป็น juice

    private Color originalColor;
    private float flashUntil = 0f;
    private bool isFlashing = false;
    #endregion

    #region Stun
    [Header("Stun")]
    [SerializeField] protected Color stunColor = Color.yellow; //สีตอนติดสตัน

    protected bool isStunned = false;
    protected float stunEndTime = 0f;
    public bool IsStunned => isStunned;
    #endregion

    #region Knockback
    [Header("Knockback")]
    [SerializeField] protected float knockbackDuration = 0.15f; //เวลาที่ใช้ในการกระเดน

    protected bool isKnockedBack = false;
    #endregion

    #region Item Drop (Summon พระ)
    [Header("Item Drop")]
    [SerializeField, Range(0f, 1f)] protected float dropChance = 0.3f; //โอกาสดรอปไอเทม (0 = ไม่ดรอป, 1 = ดรอปทุกครั้ง)
    [SerializeField] protected GameObject dropItemPrefab;
    #endregion

    #region Attack (Contact Damage)
    [Header("Attack")]
    [SerializeField] protected int contactDamage = 10;   //ดาเมจตอนติดตัวผู้เล่น
    [SerializeField] protected float attackCooldown = 1f; //cooldown กันโดนดาเมจรัวตอนติดตัวผู้เล่นอยู่
    [SerializeField] protected float attackKnockbackForce = 5f; //แรงผลักผู้เล่นตอนโดนมอนตี

    private float lastAttackTime = -999f;
    #endregion

    #region Dash Attack Settings
    [Header("Dash Attack")]
    [SerializeField] protected float dashAttackRange = 3f; // ระยะห่างที่จะเริ่มหยุดนิ่งเพื่อเตรียมพุ่งชน
    [SerializeField] protected float dashSpeed = 12f; // ความเร็วตอนพุ่งตัว
    [SerializeField] protected float dashDuration = 0.25f; // เวลาที่ใช้ในการพุ่ง (ยิ่งน้อยยิ่งพุ่งสั้น)
    [SerializeField] protected float dashRecoveryTime = 1.5f; // เวลาที่มอนสเตอร์จะยืนชะงักหลังพุ่งเสร็จก่อนเดินต่อ

    [Header("Dash Warning (Telegraph)")]
    [SerializeField] protected Color warningColor = Color.cyan; // สีที่ต้องการให้กระพริบเตือน
    [SerializeField] protected float warningDuration = 0.6f; // ระยะเวลากระพริบเตือนก่อนพุ่ง
    [SerializeField] protected float blinkInterval = 0.1f; // ความเร็วในการสลับสีกระพริบ

    protected bool isAttacking = false; // เช็คว่ามอนสเตอร์กำลังอยู่ในลูปการโจมตีหรือไม่
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        GetComponent<Collider2D>().isTrigger = true;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    protected virtual void Start()
    {
        // เก็บ reference ผู้เล่นไว้ล่วงหน้า กันเรียก PlayerController.Instance ทุกเฟรมโดยไม่จำเป็น
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }
    }

    protected virtual void Update()
    {
        //หมดเวลาสตัน -> คืนสีเดิม (แต่ถ้ายังติดแฟลชสีแดงอยู่ให้รอแฟลชจบก่อน)
        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;
            if (spriteRenderer != null && !isFlashing)
            {
                spriteRenderer.color = originalColor;
            }
        }

        //ไม่ทับสีตอนที่ยังติดสตันอยู่ (ถ้ายังติดสตันอยู่ให้เปลี่ยนเป็นสีเหลืองแทนสีเดิม)
        if (isFlashing && Time.time >= flashUntil)
        {
            isFlashing = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isStunned ? stunColor : originalColor;
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || playerTransform == null) return;

        // เพิ่มเงื่อนไข isAttacking เพื่อไม่ให้มอนสเตอร์เดินไล่ปกติเวลากำลังเตรียมพุ่งหรือพุ่งอยู่
        if (isStunned || isKnockedBack || isAttacking) return;

        // ตรวจสอบระยะห่างระหว่างมอนสเตอร์และผู้เล่น
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= dashAttackRange)
        {
            // ถ้าเข้ามาในระยะ ให้เริ่มกระบวนการชาร์จโจมตี
            StartCoroutine(DashAttackRoutine());
        }
        else
        {
            ChasePlayerIfInRange();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead || isStunned) return; //ติดสตัน -> ตีผู้เล่นไม่ได้
        if (Time.time - lastAttackTime < attackCooldown) return;

        if (DamagePlayerOnContact(other, contactDamage))
        {
            lastAttackTime = Time.time;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วาดวงกลมระยะมองเห็นใน Scene View เวลาเลือกมอนตัวนี้ ช่วยจูนค่าง่ายขึ้น
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
    #endregion

    #region Movement Logic
    protected void ChasePlayerIfInRange()
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= detectionRange && distance > stoppingDistance)
        {
            Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
        // อยู่นอกระยะมองเห็น หรือใกล้ผู้เล่นจนถึง stoppingDistance แล้ว -> ไม่ขยับ (ยืนเฉย)
    }
    #endregion

    #region Hit Feedback Logic
    protected void PlayHitFlash()
    {
        if (spriteRenderer == null) return;

        // ถ้าไม่ติดสตันค่อยเปลี่ยนเป็นสีแดง (กันทับสีเหลืองตอนสตัน)
        if (!isStunned)
        {
            spriteRenderer.color = Color.red;
        }

        flashUntil = Time.time + hitFlashDuration;
        isFlashing = true;
    }
    #endregion

    #region Stun Logic
    //ทำให้มอนติดสตัน (เดิน/ตีไม่ได้) เเละเปลี่ยนสีเป็นสีเหลือง เรียกจากการตีธรรมดาของผู้เล่น
    public void ApplyStun(float duration)
    {
        if (isDead) return;

        isStunned = true;
        stunEndTime = Time.time + duration;

        if (spriteRenderer != null) spriteRenderer.color = stunColor;
    }
    #endregion

    #region Knockback Logic
    //ผลักมอนกระเดนออกไปตามทิศทางที่กำหนด เรียกจากการตีธรรมดาของผู้เล่น
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

    #region Item Drop Logic
    protected virtual void TryDropItem()
    {
        if (dropItemPrefab == null) return;
        if (Random.value <= dropChance)
        {
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
        }
    }
    #endregion

    #region Damage & Death
    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP -= amount;
        PlayHitFlash();

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        TryDropItem();

        Destroy(gameObject);
    }
    #endregion

    #region Player Contact Helper
    //ตัวเช็คว่าชนผู้เล่นไหม ถ้าใช่ก็เรียก TakeDamage เเละผลักผู้เล่นกระเดน
    protected bool DamagePlayerOnContact(Collider2D collision, int damage)
    {
        if (collision.CompareTag("Player") && collision.TryGetComponent<PlayerController>(out var player))
        {
            player.TakeDamage(damage);

            Vector2 knockDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            player.ApplyKnockback(knockDir, attackKnockbackForce);

            return true;
        }
        return false;
    }
    #endregion

    #region Dash Attack Logic
    protected virtual IEnumerator DashAttackRoutine()
    {
        isAttacking = true; // ล็อคสถานะไว้ไม่ให้ FixedUpdate() สั่งเดินปกติ

        // --- เฟสที่ 1: หยุดยืนและกระพริบเตือน (Telegraph) ---
        float elapsed = 0f;
        bool toggleColor = false;

        while (elapsed < warningDuration)
        {
            // เช็คว่าไม่ได้ติดสถานะโดนผู้เล่นตี (ตีธรรมดาสตันหรือกระพริบแดง) ค่อยแสดงสีเตือน
            if (!isStunned && !isFlashing && spriteRenderer != null)
            {
                spriteRenderer.color = toggleColor ? warningColor : originalColor;
            }
            toggleColor = !toggleColor;

            float waitTime = Mathf.Min(blinkInterval, warningDuration - elapsed);
            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;
        }

        // คืนสีเดิมก่อนเริ่มพุ่ง
        if (!isStunned && !isFlashing && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // --- เฟสที่ 2: ล็อคเป้าแล้วพุ่งชน (Dash) ---
        // เช็คอีกครั้งเผื่อตายหรือโดนสตันระหว่างกระพริบเตือน
        if (playerTransform != null && !isDead && !isStunned)
        {
            // ล็อคทิศทางไปยังตำแหน่งปัจจุบันของผู้เล่น
            Vector2 dashDirection = ((Vector2)playerTransform.position - rb.position).normalized;
            float dashTime = 0f;

            while (dashTime < dashDuration)
            {
                // ถ้าโดนสตันหรือตายตอนกำลังพุ่ง ให้หยุดพุ่งทันที
                if (isDead || isStunned) break;

                // เคลื่อนที่ไปข้างหน้าอย่างรวดเร็ว
                rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
                dashTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        // --- เฟสที่ 3: ชะงักเพื่อพักหลังพุ่ง (Recovery) ---
        if (!isDead)
        {
            yield return new WaitForSeconds(dashRecoveryTime);
        }

        // ปลดล็อคสถานะ มอนสเตอร์จะกลับไปเช็คการเดินต่อใน FixedUpdate()
        isAttacking = false;
    }
    #endregion
}