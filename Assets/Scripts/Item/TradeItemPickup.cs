using System.Collections;
using UnityEngine;

//ไอเทมเก็บบนแมพ (ของสำหรับเอาไปแลกที่ต้นไม้) เดินชนแล้วเก็บเข้ากระเป๋าทันที
[RequireComponent(typeof(Collider2D))]
public class TradeItemPickup : MonoBehaviour
{
    #region Pickup Settings
    [Header("Pickup Settings")]
    [SerializeField] private int itemAmount = 1; //จำนวนของที่จะได้ต่อการเก็บ

    [SerializeField] private float respawnSeconds = 0f; //เวลาที่ไอเทมจะเกิดใหม่

    [SerializeField] private SpriteRenderer spriteRenderer; //ใช้ซ่อนรูปตอนเกิดใหม่
    #endregion

    #region State
    private Collider2D col;
    private bool isCollected = false; //กันเก็บซ้ำ
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true; //ตั้งเป็น Trigger เพื่อเช็คผู้เล่นเดินมาชน
    }
    #endregion

    #region Pickup Logic
    //เช็คว่าผู้เล่นเดินมาชนไอเทม -> เพิ่มของเข้ากระเป๋า
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerController>(out var player))
        {
            player.AddTradeItem(itemAmount);

            if (respawnSeconds > 0f)
            {
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private IEnumerator RespawnRoutine()
    {
        isCollected = true;
        SetVisible(false);

        yield return new WaitForSeconds(respawnSeconds);

        isCollected = false;
        SetVisible(true);
    }
    #endregion

    #region Helpers
    //สั่งโชว์/ซ่อนทั้งรูปและ collider
    private void SetVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        col.enabled = visible;
    }
    #endregion
}