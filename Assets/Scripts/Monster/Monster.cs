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
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float stoppingDistance = 0.6f;

    protected Rigidbody2D rb;
    protected Transform playerTransform;
    #endregion

    #region Hit Feedback (Optional Flash)
    [Header("Hit Feedback (Optional)")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected float hitFlashDuration = 0.1f;

    private Color originalColor;
    private float flashUntil = 0f;
    private bool isFlashing = false;
    #endregion

    #region Stun
    [Header("Stun Settings")]
    [SerializeField] protected Color stunColor = Color.yellow;
    [SerializeField] protected float stunDuration = 1.5f;

    protected bool isStunned = false;
    protected float stunEndTime = 0f;
    public bool IsStunned => isStunned;
    #endregion

    #region Knockback
    [Header("Knockback")]
    [SerializeField] protected float knockbackDuration = 0.15f;

    protected bool isKnockedBack = false;
    #endregion

    #region Item Drop (Summon พระ)
    [Header("Item Drop")]
    [SerializeField, Range(0f, 1f)] protected float dropChance = 0.3f;
    [SerializeField] protected GameObject dropItemPrefab;
    #endregion

    #region Attack (Contact Damage)
    [Header("Attack")]
    [SerializeField] protected int contactDamage = 10;
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected float attackKnockbackForce = 5f;

    private float lastAttackTime = -999f;
    #endregion

    #region Dash Attack Settings
    [Header("Dash Attack")]
    [SerializeField] protected float dashAttackRange = 3f;
    [SerializeField] protected float dashSpeed = 12f;
    [SerializeField] protected float dashDuration = 0.25f;
    [SerializeField] protected float dashRecoveryTime = 1.5f;

    [Header("Dash Warning (Telegraph)")]
    [SerializeField] protected Color warningColor = Color.cyan;
    [SerializeField] protected float warningDuration = 0.6f;
    [SerializeField] protected float blinkInterval = 0.1f;

    protected bool isAttacking = false;
    protected bool isDashing = false;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // สำคัญ: ถ้า Rigidbody2D ของมอน/ผู้เล่นเป็น Kinematic ทั้งคู่ (ปกติของเกมที่คุมการเดินเองด้วย MovePosition)
        // Unity จะไม่ยิง OnCollisionEnter/Stay2D ให้เลยถ้าไม่เปิดตัวนี้ไว้ -> เป็นสาเหตุหลักที่ดาเมจ/HP ไม่ลด
        rb.useFullKinematicContacts = true;

        // ป้องกันมอนวิ่งพุ่ง (dash) เร็วจนทะลุผ่านผู้เล่นในเฟรมเดียวโดยไม่เกิดการชน (tunneling)
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    protected virtual void Start()
    {
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }
    }

    protected virtual void Update()
    {
        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;
            if (spriteRenderer != null && !isFlashing)
            {
                spriteRenderer.color = originalColor;
            }
        }

        if (isFlashing && Time.time >= flashUntil)
        {
            isFlashing = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isStunned ? stunColor : originalColor;
            }
        }

        if (spriteRenderer != null && !isDead && !isStunned)
        {
            if (rb.linearVelocity.x < -0.1f) // เดินไปทางซ้าย
            {
                spriteRenderer.flipX = true;
            }
            else if (rb.linearVelocity.x > 0.1f) // เดินไปทางขวา
            {
                spriteRenderer.flipX = false;
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || playerTransform == null) return;

        // Knockback and the actual dash movement drive rb.linearVelocity themselves
        // from their coroutines. If we zero the velocity here too, we run BEFORE the
        // physics step every fixed frame and stomp whatever the coroutine just set,
        // so the monster never actually moves (it only visually flashes/telegraphs).
        if (isKnockedBack || isDashing)
        {
            return;
        }

        // Stunned (and not being knocked back) or mid-telegraph/recovery: hold still.
        if (isStunned || isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= dashAttackRange)
        {
            StartCoroutine(DashAttackRoutine());
        }
        else
        {
            ChasePlayerIfInRange();
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        HandleContactDamage(other);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        HandleContactDamage(other);
    }

    /// <summary>
    /// ใช้ร่วมกันทั้ง Enter และ Stay เพราะการพุ่งชน (dash) เร็วมาก
    /// บางทีสัมผัสกันแค่เฟรมเดียว (เกิด Enter แต่ไม่มี Stay ตามมา) ถ้าดักแค่ Stay อย่างเดียวอาจพลาดได้
    /// </summary>
    private void HandleContactDamage(Collision2D other)
    {
        // Debug ชั่วคราว: ถ้า log นี้ไม่ขึ้นเลยตอนมอนพุ่งชนผู้เล่น แปลว่าปัญหาอยู่ที่ Physics setup
        // (Collider หาย/เป็น Trigger/Layer Collision Matrix ปิดไว้) ไม่ใช่ปัญหาที่ logic ข้างล่าง
        Debug.Log($"[Monster] ชนกับ: {other.gameObject.name}, isDashing={isDashing}");

        if (!isDashing || isDead || isStunned) return;

        if (Time.time - lastAttackTime < attackCooldown) return;

        if (DamagePlayerOnContact(other, contactDamage))
        {
            lastAttackTime = Time.time;
        }
    }

    private void OnDrawGizmosSelected()
    {
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
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    #endregion

    #region Hit Feedback Logic
    protected void PlayHitFlash()
    {
        if (spriteRenderer == null) return;

        if (!isStunned)
        {
            spriteRenderer.color = Color.red;
        }

        flashUntil = Time.time + hitFlashDuration;
        isFlashing = true;
    }
    #endregion

    #region Stun Logic
    public void ApplyStun(float duration)
    {
        if (isDead) return;

        isStunned = true;
        stunEndTime = Time.time + duration;

        if (spriteRenderer != null) spriteRenderer.color = stunColor;
    }

    public float GetStunDuration() => stunDuration;
    #endregion

    #region Knockback Logic
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
            float t = 1f - (elapsed / knockbackDuration);
            rb.linearVelocity = knockbackVelocity * t;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
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

        Debug.Log($"[Monster] โดนดาเมจ {amount} -> เลือดเหลือ {Mathf.Max(currentHP, 0)}/{maxHP}");

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
    protected bool DamagePlayerOnContact(Collision2D collision, int damage)
    {
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent<PlayerController>(out var player))
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
        isAttacking = true;

        float elapsed = 0f;
        bool toggleColor = false;

        while (elapsed < warningDuration)
        {
            if (!isStunned && !isFlashing && spriteRenderer != null)
            {
                spriteRenderer.color = toggleColor ? warningColor : originalColor;
            }
            toggleColor = !toggleColor;

            float waitTime = Mathf.Min(blinkInterval, warningDuration - elapsed);
            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;
        }

        if (!isStunned && !isFlashing && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (playerTransform != null && !isDead && !isStunned)
        {
            Vector2 dashDirection = ((Vector2)playerTransform.position - rb.position).normalized;
            float dashTime = 0f;

            isDashing = true;
            while (dashTime < dashDuration)
            {
                if (isDead || isStunned) break;

                rb.linearVelocity = dashDirection * dashSpeed;
                dashTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            isDashing = false;
        }

        if (!isDead)
        {
            yield return new WaitForSeconds(dashRecoveryTime);
        }

        isAttacking = false;
    }
    #endregion
}