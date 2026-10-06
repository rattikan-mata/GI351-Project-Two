using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class EndingDoor : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ชื่อซีนฉากจบ ต้องตรงกับใน Build Settings")]
    [SerializeField] private string endingSceneName = "Endding";

    [Header("Error Message")]
    [Tooltip("ข้อความเตือนตอนไม่มีกุญแจ")]
    [SerializeField] private string errorMessage = "ประตูล็อคอยู่!";
    [SerializeField] private float messageDuration = 2.0f;

    [Header("Success Message")]
    [Tooltip("ข้อความตอนมีกุญแจและไขประตูได้")]
    [SerializeField] private string successMessage = "ไขกุญแจสำเร็จ! กำลังไปฉากจบ...";
    [SerializeField] private float successDuration = 1.5f;

    private bool playerInRange = false;

    private void Awake()
    {
        
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        // เมื่อผู้เล่นยืนอยู่ในระยะแล้วกดปุ่ม F
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            TryOpenDoor();
        }
    }

    private void TryOpenDoor()
    {
        // เช็กว่า StoryManager มีกุญแจบ้าน (hasHouseKey) แล้วหรือยัง
        if (StoryManager.Instance != null && StoryManager.Instance.hasHouseKey)
        {
            // กรณีมีกุญแจ: โชว์ข้อความสำเร็จ และเปลี่ยนซีน
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWarpMessage(successMessage, successDuration);
            }

            AudioManager.Instance?.PlaySFX("door_open");
            Invoke(nameof(LoadEndingScene), successDuration);
        }
        else
        {
            // กรณีไม่มีกุญแจ: แสดงข้อความแจ้งเตือนบน UI Text
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWarpMessage(errorMessage, messageDuration);
            }
            else
            {
                Debug.Log(errorMessage);
            }

            AudioManager.Instance?.PlaySFX("door_locked");
        }
    }

    private void LoadEndingScene()
    {
        SceneManager.LoadScene(endingSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}