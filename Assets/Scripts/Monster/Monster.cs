using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Monster : MonoBehaviour, IDamageable
{
    #region Health
    [Header("Health")]
    [SerializeField] protected int maxHP = 30;
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

    #region Item Drop (Summon พระ)
    [Header("Item Drop")]
    [SerializeField, Range(0f, 1f)] protected float dropChance = 0.3f; //โอกาสดรอปไอเทม (0 = ไม่ดรอป, 1 = ดรอปทุกครั้ง)
    [SerializeField] protected GameObject dropItemPrefab;
    #endregion

    #region Attack (Contact Damage)
    [Header("Attack")]
    [SerializeField] protected int contactDamage = 10;   //ดาเมจตอนติดตัวผู้เล่น
    [SerializeField] protected float attackCooldown = 1f; //cooldown กันโดนดาเมจรัวตอนติดตัวผู้เล่นอยู่

    private float lastAttackTime = -999f;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
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
        if (isFlashing && Time.time >= flashUntil)
        {
            isFlashing = false;
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || playerTransform == null) return;
        ChasePlayerIfInRange();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
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
        spriteRenderer.color = Color.red;
        flashUntil = Time.time + hitFlashDuration;
        isFlashing = true;
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
    //ตัวเช็คว่าชนผู้เล่นไหม ถ้าใช่ก็เรียก TakeDamage ของ PlayerController
    protected bool DamagePlayerOnContact(Collider2D collision, int damage)
    {
        if (collision.CompareTag("Player") && collision.TryGetComponent<PlayerController>(out var player))
        {
            player.TakeDamage(damage);
            return true;
        }
        return false;
    }
    #endregion
}