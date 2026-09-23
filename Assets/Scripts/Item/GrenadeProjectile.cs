using UnityEngine;

/// <summary>
/// ลูกระเบิดของหนังสติ๊ก (SniperShot) : บินตรงไปข้างหน้า ชนมอน/กำแพง = ระเบิด
/// ระเบิดแล้วทำดาเมจรอบจุดระเบิด (AoE) + ปล่อยกระสุนกระจายรอบทิศ (ใช้ Projectile.cs เดิมเป็นเศษกระสุน)
/// ค่าทั้งหมด (ดาเมจ/รัศมี/จำนวนเศษ/ความเร็ว/ระยะ ฯลฯ) ปรับได้ที่ ItemData asset ของหนังสติ๊ก
///
/// วิธีทำ Prefab: ลูกระเบิด = Sprite + Rigidbody2D + Collider2D + GrenadeProjectile.cs
///                 แล้วลากใส่ช่อง Sniper Projectile Prefab ใน ItemData
///                 เศษกระสุน = Prefab เดิมที่มี Projectile.cs ลากใส่ช่อง Fragment Prefab
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class GrenadeProjectile : MonoBehaviour
{
    [Header("Layers")]
    [Tooltip("Layer ของมอนสเตอร์ (ใช้ทั้งตอนลูกระเบิดชน และตอนหาเป้าในรัศมีระเบิด)")]
    [SerializeField] private LayerMask targetLayer;

    [Tooltip("(ไม่บังคับ) Layer ของกำแพง/สิ่งกีดขวางที่ทำให้ลูกระเบิดระเบิดทันทีเมื่อชน")]
    [SerializeField] private LayerMask obstacleLayer;

    private ItemData data;
    private Vector2 direction;
    private Vector3 startPos;
    private Rigidbody2D rb;
    private Collider2D directHitCollider; // มอนที่โดนลูกระเบิดชนตรงๆ (ใช้กันเศษกระสุนโดนซ้ำ)
    private bool exploded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        GetComponent<Collider2D>().isTrigger = true; // ให้ OnTriggerEnter2D ทำงาน
    }

    /// <summary>เรียกจาก PlayerController ตอนยิง</summary>
    public void Init(Vector2 fireDirection, ItemData itemData)
    {
        data = itemData;
        direction = fireDirection.normalized;
        startPos = transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        if (exploded || data == null) return;

        rb.MovePosition(rb.position + direction * data.sniperSpeed * Time.fixedDeltaTime);

        if (Vector3.Distance(startPos, transform.position) >= data.sniperRange)
        {
            if (data.explodeAtMaxRange) Explode();
            else Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded || data == null) return;

        int otherLayerMask = 1 << other.gameObject.layer;

        if ((targetLayer.value & otherLayerMask) != 0)
        {
            directHitCollider = other;
            Explode();
        }
        else if ((obstacleLayer.value & otherLayerMask) != 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector2 center = transform.position;

        // 1) ดาเมจระเบิดรอบจุดระเบิด (มอนแต่ละตัวโดนครั้งเดียว แม้มีหลาย Collider)
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, data.explosionRadius, targetLayer);
        var alreadyHit = new System.Collections.Generic.HashSet<GameObject>();

        foreach (var hit in hits)
        {
            if (!alreadyHit.Add(hit.gameObject)) continue;

            if (hit.TryGetComponent<Monster>(out var monster))
            {
                monster.TakeDamage(data.damage);

                Vector2 dir = ((Vector2)monster.transform.position - center).normalized;
                monster.ApplyKnockback(dir, data.knockbackForce);

                if (data.stunDuration > 0f) monster.ApplyStun(data.stunDuration);
            }
            else if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(data.damage);
            }
        }

        // 2) เอฟเฟกต์ระเบิด (ถ้ามี)
        if (data.explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(data.explosionEffectPrefab, center, Quaternion.identity);
            Destroy(fx, data.explosionEffectLifetime);
        }

        // 3) กระสุนกระจายรอบทิศ
        SpawnFragments(center);

        Destroy(gameObject);
    }

    private void SpawnFragments(Vector2 center)
    {
        if (data.fragmentPrefab == null || data.fragmentCount <= 0) return;

        int count = data.fragmentCount;
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = data.fragmentAngleOffset + step * i;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 spawnPos = center + dir * data.fragmentSpawnOffset;

            GameObject fragObj = Instantiate(data.fragmentPrefab, spawnPos, Quaternion.identity);

            if (fragObj.TryGetComponent<Projectile>(out var fragment))
            {
                fragment.Init(dir, data.fragmentSpeed, data.fragmentDamage, data.fragmentRange);
            }

            // กันเศษกระสุนทุกเม็ดโดนมอนตัวที่ลูกระเบิดชนซ้ำ (ไม่งั้นตัวนั้นโดนดาเมจซ้อนตั้งแต่เฟรมแรก)
            if (data.fragmentsIgnoreDirectTarget && directHitCollider != null
                && fragObj.TryGetComponent<Collider2D>(out var fragCol))
            {
                Physics2D.IgnoreCollision(fragCol, directHitCollider);
            }
        }
    }
}
