using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LocalConditionalWarp : MonoBehaviour
{
    public enum WarpType { Normal, RequireTaneeDefeated, RequireHouseKey }

    [Header("Warp Destination")]
    [Tooltip("ลาก Empty GameObject ที่เป็นจุดปลายทางมาใส่ตรงนี้")]
    public Transform destination;

    [Header("Warp Settings")]
    public WarpType requiredCondition = WarpType.Normal;

    [Header("Messages")]
    [Tooltip("ข้อความที่จะขึ้นถ้าเงื่อนไขไม่ผ่าน")]
    public string errorMessage = "เข้าไม่ได้!";

    [Tooltip("ข้อความที่จะขึ้นตอนผ่านเงื่อนไข/ไขกุญแจสำเร็จ")]
    public string successMessage = "ไขกุญแจบ้านสำเร็จ!";

    private bool playerInRange = false;
    private GameObject playerRef; // เอาไว้จำตัวผู้เล่นตอนเดินมาเหยียบ

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        // ถ้าผู้เล่นอยู่ในระยะ และกด F ให้ทำการวาร์ป
        if (playerInRange && Input.GetKeyDown(KeyCode.F) && playerRef != null)
        {
            TryWarp();
        }
    }

    private void TryWarp()
    {
        if (destination == null)
        {
            Debug.LogWarning("ลืมใส่จุดหมายปลายทาง (Destination) ที่ประตูวาร์ป!");
            return;
        }

        // เช็คเงื่อนไขที่ 1: ต้องตีตานีก่อน
        if (requiredCondition == WarpType.RequireTaneeDefeated && !StoryManager.Instance.isTaneeDefeated)
        {
            UIManager.Instance?.ShowWarpMessage(errorMessage);
            return;
        }

        // เช็คเงื่อนไขที่ 2: ต้องมีกุญแจบ้าน
        if (requiredCondition == WarpType.RequireHouseKey && !StoryManager.Instance.hasHouseKey)
        {
            UIManager.Instance?.ShowWarpMessage(errorMessage);
            return;
        }

        // เงื่อนไขผ่าน (รวมถึงกรณีใช้กุญแจบ้านสำเร็จ): โชว์ข้อความสำเร็จก่อนวาร์ป
        if (!string.IsNullOrEmpty(successMessage))
        {
            UIManager.Instance?.ShowWarpMessage(successMessage);
        }

        // ย้ายตำแหน่งผู้เล่นไปที่จุดหมาย (วาร์ปในฉากเดียวกัน)
        playerRef.transform.position = destination.position;

        // เล่นเสียงวาร์ป (เปิดใช้คอมเมนต์ด้านล่างนี้ได้ถ้ามีระบบเสียง)
        // AudioManager.Instance?.PlaySFX("warp_sound"); 
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerRef = other.gameObject; // จำตัวผู้เล่นไว้
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerRef = null;
        }
    }
}