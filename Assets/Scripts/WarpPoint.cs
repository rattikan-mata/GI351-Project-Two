using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WarpPoint : MonoBehaviour
{
    [Header("Warp Destination")]
    [Tooltip("ลากจุดหมายปลายทาง (Transform) มาใส่ช่องนี้")]
    [SerializeField] private Transform destination;

    [Header("Settings")]
    [Tooltip("เวลาดีเลย์ก่อนวาร์ป (ใส่ 0 คือวาร์ปทันที)")]
    [SerializeField] private float warpDelay = 0f;

    private bool isWarping = false; // ป้องกันการชนซ้ำรัวๆ

    private void Awake()
    {
        // บังคับให้ Collider เป็น Trigger เสมอ
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isWarping) return;

        // เช็คว่าคนที่มาชนคือผู้เล่น (ใช้ Tag "Player" เหมือนที่ตั้งไว้ในสคริปต์อื่นๆ)
        if (other.CompareTag("Player") && destination != null)
        {
            StartCoroutine(WarpRoutine(other.transform));
        }
    }

    private IEnumerator WarpRoutine(Transform playerTransform)
    {
        isWarping = true;

        yield return new WaitForSeconds(warpDelay);

        // ย้ายตำแหน่งผู้เล่นไปยังจุดหมาย
        playerTransform.position = destination.position;

        // บังคับย้าย Rigidbody2D ด้วยเพื่อป้องกันฟิสิกส์ดึงตัวละครกลับ
        if (playerTransform.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.position = destination.position;
        }

        // รอสักพักก่อนเปิดให้วาร์ปใหม่ได้ ป้องกันบัควาร์ปวนลูป
        yield return new WaitForSeconds(0.5f);
        isWarping = false;
    }
}