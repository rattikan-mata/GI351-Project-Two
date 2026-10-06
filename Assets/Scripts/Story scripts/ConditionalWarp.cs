using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class ConditionalWarp : MonoBehaviour
{
    public enum WarpType { Normal, RequireTaniDefeated }

    [Header("Warp Settings")]
    public string targetSceneName;
    public WarpType requiredCondition = WarpType.Normal;
    
    [Header("Error Message")]
    [Tooltip("ข้อความที่จะขึ้นถ้าเงื่อนไขไม่ผ่าน เช่น 'เข้าคอกวัวไม่ได้รากไม้บังอยู่'")]
    public string errorMessage = "เข้าไม่ได้!";

    private bool playerInRange = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            TryWarp();
        }
    }

    private void TryWarp()
    {
        // เช็คเงื่อนไข
        if (requiredCondition == WarpType.RequireTaniDefeated && !StoryManager.Instance.isTaneeDefeated)
        {
            // เงื่อนไขไม่ผ่าน: โชว์ข้อความเตือน (ตรงนี้คุณอาจจะทำ UI Text ลอยขึ้นมา หรือใช้ Debug.Log ไปก่อน)
            Debug.Log(errorMessage);
            // ถ้าอยากใช้ Combat Text ลอยๆ สีแดงแก้ขัดไปก่อน ให้ใช้บรรทัดล่างนี้แทนได้ครับ
            // UIManager.Instance?.ShowCombatText(transform.position + Vector3.up, 0, false); 
            return;
        }

        // เงื่อนไขผ่าน: วาร์ป!
        SceneManager.LoadScene(targetSceneName);
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