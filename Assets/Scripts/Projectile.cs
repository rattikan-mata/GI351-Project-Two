using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Target Layer")]
    [SerializeField] private LayerMask targetLayer;// Layer ของมอนสเตอร์ที่กระสุนจะไปโดน

    private float maxTravelDistance;//ระยะสูงสุดก่อนกระสุนหาย (รับค่ามาจาก PlayerController)

    private Vector2 direction;
    private float speed;
    private int damage;
    private Vector3 startPos;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;


        Collider2D col = GetComponent<Collider2D>(); //บังคับให้ Collider เป็น Trigger เพื่อให้ OnTriggerEnter2D ทำงาน
        col.isTrigger = true;
    }
    public void Init(Vector2 fireDirection, float fireSpeed, int fireDamage, float travelDistance)// <summary>เรียกจาก PlayerController
    {
        direction = fireDirection.normalized;
        speed = fireSpeed;
        damage = fireDamage;
        maxTravelDistance = travelDistance;
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
        // เช็คว่าโดน Layer มอนสเตอร์หรือไม่
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}

public interface IDamageable
{
    void TakeDamage(int amount);
}
