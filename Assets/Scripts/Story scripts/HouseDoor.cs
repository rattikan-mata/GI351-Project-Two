using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HouseDoor : MonoBehaviour
{
    private bool playerInRange = false;

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            if (StoryManager.Instance.hasHouseKey)
            {
                Debug.Log("ไขกุญแจสำเร็จ! เข้าบ้านได้ (ใส่โค้ดเปิดประตูหรือเปลี่ยนฉากจบที่นี่)");
                AudioManager.Instance?.PlaySFX("door_open"); // ถ้ามีเสียง
            }
            else
            {
                Debug.Log("เข้าบ้านไม่ได้ไม่มีกุญแจ");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}