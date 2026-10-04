using UnityEngine;
using UnityEngine.SceneManagement; // จำเป็นสำหรับการเปลี่ยนซีน

[RequireComponent(typeof(Collider2D))]
public class EndingDoor : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("พิมพ์ชื่อซีนฉากจบให้ตรงกับไฟล์ซีนเป๊ะๆ (เช่น Endding)")]
    public string endingSceneName = "Endding";

    [Header("Error Message")]
    [Tooltip("ข้อความเตือนตอนไม่มีกุญแจ")]
    public string errorMessage = "ประตูล็อคอยู่! ต้องไปปราบปอบเพื่อเอากุญแจมาไข";

    private bool playerInRange = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            TryOpenDoor();
        }
    }

    private void TryOpenDoor()
    {
        // เช็คว่ามีกุญแจบ้านจาก StoryManager หรือยัง
        if (StoryManager.Instance.hasHouseKey)
        {
            Debug.Log("ไขกุญแจสำเร็จ! กำลังตัดไปฉากจบ...");
            AudioManager.Instance?.PlaySFX("door_open"); // เล่นเสียงเปิดประตู (ถ้ามี)
            
            // โหลดซีนฉากจบ
            SceneManager.LoadScene(endingSceneName);
        }
        else
        {
            // ถ้ายังไม่มีกุญแจ
            Debug.Log(errorMessage);
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