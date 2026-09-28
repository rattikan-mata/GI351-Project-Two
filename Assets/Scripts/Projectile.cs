using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Target Layer")]
    [SerializeField] private LayerMask targetLayer; // Layer ของมอนสเตอร์ที่กระสุนจะไปโดน (ใช้ตอน Can Hit Player ปิดอยู่)

    [Header("Hit Player (สำหรับกระสุนของบอส/มอน)")]
    [Tooltip("ติ๊กไว้ = กระสุนนี้ทำดาเมจผู้เล่นได้ด้วย เช็คจาก Tag \"Player\" โดยตรง (ไม่ใช้ Layer/IDamageable) ใช้กับกระสุนของบอส/มอน ส่วน Prefab กระสุนของผู้เล่นเอง (ยิงใส่มอน) ปล่อยไม่ติ๊กไว้")]
    [SerializeField] private bool canHitPlayer = false;

    [Header("Piercing (ทะลุเป้าหมาย)")]
    [Tooltip("ติ๊กไว้ = กระสุนจะไม่หายตัวหลังโดนเป้าหมายแรก จะพุ่งทะลุต่อไปโดนตัวอื่นได้อีก จนกว่าจะหมดระยะ Max Travel Distance")]
    [SerializeField] private bool pierceThroughTargets = false;

    [Tooltip("จำนวนเป้าหมายสูงสุดที่ทะลุได้ (-1 = ทะลุได้ไม่จำกัด) มีผลเฉพาะตอนติ๊ก Pierce Through Targets ไว้เท่านั้น")]
    [SerializeField] private int maxPierceCount = -1;

    private int pierceCount = 0;
    private readonly HashSet<GameObject> alreadyHitTargets = new HashSet<GameObject>(); // กันโดนเป้าหมายเดิมซ้ำ (เผื่อมี Collider ย่อยหลายชิ้น)

    private float maxTravelDistance; // ระยะสูงสุดก่อนกระสุนหาย (รับค่ามาจาก Init)

    private Vector2 direction;
    private float speed;
    private int damage;
    private float knockbackForce; // แรงผลักกระเด็นตอนโดนเป้าหมาย (0 = ไม่ผลัก) รับค่ามาจาก Init()
    private Vector3 startPos;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        Collider2D col = GetComponent<Collider2D>(); // บังคับให้ Collider เป็น Trigger เพื่อให้ OnTriggerEnter2D ทำงาน
        col.isTrigger = true;
    }

    /// <summary>เรียกจาก PlayerController / BossController / Monster ตอนยิงกระสุนออกไป</summary>
    public void Init(Vector2 fireDirection, float fireSpeed, int fireDamage, float travelDistance, float impactKnockbackForce = 0f)
    {
        direction = fireDirection.normalized;
        speed = fireSpeed;
        damage = fireDamage;
        maxTravelDistance = travelDistance;
        knockbackForce = impactKnockbackForce;
        startPos = transform.position;

        // หมุนสไปรต์กระสุนตามทิศทางการยิง
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        if (Vector3.Distance(startPos, transform.position) >= maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // กระสุนที่ทำดาเมจผู้เล่นได้ (เช่น ของบอส/มอน) -> เช็คจาก Tag "Player" ตรงๆ ไม่ผ่าน Layer/IDamageable
        if (canHitPlayer && other.CompareTag("Player"))
        {
            if (!alreadyHitTargets.Add(other.gameObject)) return; // กันโดนตัวเดิมซ้ำ

            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamage(damage);

                if (knockbackForce > 0f)
                {
                    Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                    player.ApplyKnockback(dir, knockbackForce);
                }
            }

            HandleHitResolution();
            return;
        }

        // เช็คว่าโดน Layer มอนสเตอร์หรือไม่ (สำหรับกระสุนที่ยิงใส่มอนตามปกติ)
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (!alreadyHitTargets.Add(other.gameObject)) return; // กันโดนเป้าหมายเดิมซ้ำ (เผื่อมอนมี Collider ย่อยหลายชิ้นซ้อนกัน)

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }

        // เผื่อ targetLayer ครอบคลุมผู้เล่นด้วยในบางเคส (ไม่ได้ใช้ canHitPlayer) ให้ผลักกระเด็นได้เหมือนกัน
        if (knockbackForce > 0f && other.TryGetComponent<PlayerController>(out var pushedPlayer))
        {
            pushedPlayer.ApplyKnockback(direction, knockbackForce);
        }

        HandleHitResolution();
    }

    // ตัดสินใจว่ากระสุนจะทะลุต่อไปหรือหายตัว ใช้ร่วมกันทั้งเคสโดนผู้เล่นและโดนมอน
    private void HandleHitResolution()
    {
        if (!pierceThroughTargets)
        {
            Destroy(gameObject);
            return;
        }

        // ทะลุเป้าหมายนี้ไปต่อ นับจำนวนที่ทะลุแล้ว ถ้าครบตามที่กำหนด (ไม่ใช่ -1) ค่อยหายตัว
        pierceCount++;
        if (maxPierceCount >= 0 && pierceCount >= maxPierceCount)
        {
            Destroy(gameObject);
        }
    }
}

public interface IDamageable
{
    void TakeDamage(int amount);
}