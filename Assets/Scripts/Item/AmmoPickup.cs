using System.Collections;
using UnityEngine;

//จุดเก็บกระสุนข้าวสารตามแมพ ผู้เล่นเดินชนแล้วได้กระสุนเพิ่มทันที
[RequireComponent(typeof(Collider2D))]
public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [SerializeField] private int ammoAmount = 5;//จำนวนกระสุนที่จะได้ต่อการเก็บ 1 ครั้ง

    [Header("Respawn Settings")]
    [SerializeField] private float respawnSeconds = 15f;

    [Header("Visual (Optional)")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Collider2D col;
    private bool isCollected = false;//กันเก็บซ้ำ

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true; //ตั้งเป็น Trigger เพื่อเช็คผู้เล่นเดินมาชน
    }

    //เช็คว่าผู้เล่นเดินมาชน -> เพิ่มกระสุน
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerController>(out var player))
        {
            player.AddAmmo(ammoAmount);

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


    private void SetVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        col.enabled = visible;
    }
}
