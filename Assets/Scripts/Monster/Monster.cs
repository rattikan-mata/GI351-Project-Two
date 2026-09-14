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
        if (isStunned || isKnockedBack) return; //ติดสตันหรือกำลังกระเดน -> ไม่เดินไล่
        ChasePlayerIfInRange();
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
}