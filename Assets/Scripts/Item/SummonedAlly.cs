using UnityEngine;

//ตัวพระที่ถูกเรียกออกมา เดินตามผู้เล่นเเละยิงมอนที่อยู่ในระยะอัตโนมัติ หมดเวลาเเล้วหายไป
[RequireComponent(typeof(Rigidbody2D))]
public class SummonedAlly : MonoBehaviour
{
    #region Follow Settings
    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 3.5f;   //ความเร็วตอนเดินตามผู้เล่น
    [SerializeField] private float followOffsetX = 1f;    //ระยะห่างด้านข้างผู้เล่นตอนยืนนิ่ง
    #endregion

    #region Attack Settings
    [Header("Attack Settings")]
    [SerializeField] private GameObject projectilePrefab; //กระสุนที่พระยิง (ใช้ prefab เดียวกับผู้เล่นได้)
    [SerializeField] private float projectileSpeed = 8f;   //ความเร็วกระสุนของพระ
    [SerializeField] private int projectileDamage = 10;    //ดาเมจกระสุนของพระ
    [SerializeField] private float projectileRange = 8f;   //ระยะสูงสุดที่กระสุนของพระจะพุ่งไป
    [SerializeField] private float attackCooldown = 0.8f;  //cooldown ของการยิงแต่ละนัด
    [SerializeField] private float detectRange = 7f;       //ระยะมองเห็นมอน ถ้ามอนอยู่ในระยะนี้จะยิง
    [SerializeField] private LayerMask monsterLayer;       //Layer ของมอนที่พระจะเล็งยิง
    #endregion

    #region Lifetime Settings
    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 30f; //เวลาที่พระอยู่ก่อนหายไป (วินาที) - เวลาจำกัดตามที่ตั้งค่า
    #endregion

    #region State
    private Transform playerTransform;
    private Rigidbody2D rb;
    private float lastAttackTime = -999f;
    private float dieAtTime;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        //เก็บ reference ผู้เล่นไว้ล่วงหน้า จะได้เดินตามถูกตัว
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }

        //ตั้งเวลาหมดอายุของตัวพระ (limited lifetime)
        dieAtTime = Time.time + lifeTime;
    }

    private void Update()
    {
        //หมดเวลาเเล้วหายไป
        if (Time.time >= dieAtTime)
        {
            Destroy(gameObject);
            return;
        }

        TryShootNearestMonster();
    }

    private void FixedUpdate()
    {
        FollowPlayer();
    }
    #endregion

    #region Follow Logic
    //เดินตามผู้เล่น ถ้าห่างเกินระยะที่กำหนดค่อยวิ่งเข้ามา
    private void FollowPlayer()
    {
        if (playerTransform == null) return;

        Vector2 targetPos = (Vector2)playerTransform.position + Vector2.left * followOffsetX;
        float distance = Vector2.Distance(rb.position, targetPos);

        if (distance > 0.1f)
        {
            Vector2 direction = (targetPos - rb.position).normalized;
            rb.MovePosition(rb.position + direction * followSpeed * Time.fixedDeltaTime);
        }
    }
    #endregion

    #region Attack Logic
    //หามอนที่ใกล้ที่สุดในระยะ เเล้วยิงใส่ถ้า cooldown ครบเเล้ว
    private void TryShootNearestMonster()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (projectilePrefab == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRange, monsterLayer);
        if (hits.Length == 0) return; //ไม่มีมอนในระยะ ไม่ยิง

        //หาตัวที่ใกล้ที่สุด
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = hit.transform;
            }
        }

        if (nearest == null) return;

        lastAttackTime = Time.time;

        //ยิงกระสุนไปทางมอนตัวที่ใกล้ที่สุด
        Vector2 direction = ((Vector2)nearest.position - (Vector2)transform.position).normalized;
        GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        if (projObj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Init(direction, projectileSpeed, projectileDamage, projectileRange);
        }
    }
    #endregion
}