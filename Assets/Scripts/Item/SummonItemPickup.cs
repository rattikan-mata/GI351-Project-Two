using UnityEngine;

//ไอเทม Summon ที่ดรอปจากมอน ผู้เล่นเดินชนแล้วเก็บเข้ากระเป๋าทันที เอาไปกดใช้เรียกพระได้
[RequireComponent(typeof(Collider2D))]
public class SummonItemPickup : MonoBehaviour
{
    #region Settings
    [Header("Summon Item Settings")]
    [SerializeField] private int itemAmount = 1; //จำนวนไอเทม summon ที่จะได้ต่อการเก็บ 1 ครั้ง
    [SerializeField] private GameObject allyPrefab; //พระที่จะถูกเรียกออกมาเมื่อใช้ไอเทมนี้
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true; //ตั้งเป็น Trigger เพื่อเช็คผู้เล่นเดินมาชน
    }
    #endregion

    #region Pickup Logic
    //เช็คว่าผู้เล่นเดินมาชน -> เพิ่มไอเทม summon เข้ากระเป๋าแล้วทำลายตัวเอง
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerController>(out var player))
        {
            player.AddSummonItem(itemAmount, allyPrefab, transform.position);


            Destroy(gameObject); //เก็บครั้งเดียวจบ ไม่งอกใหม่
        }
    }
    #endregion
}