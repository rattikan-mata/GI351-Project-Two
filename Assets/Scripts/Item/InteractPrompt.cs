using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InteractPrompt : MonoBehaviour
{
    [Tooltip("ลาก GameObject ภาพปุ่ม F (ที่เป็นลูกของวัตถุนี้) มาใส่")]
    [SerializeField] private GameObject promptIcon;

    private void Awake()
    {
        // ซ่อนปุ่ม F ไว้เป็นค่าเริ่มต้นตอนเริ่มเกม
        if (promptIcon != null) 
        {
            promptIcon.SetActive(false);
        }
        
        // บังคับให้ Collider เป็น Trigger (เพื่อให้เดินผ่านทะลุได้และใช้เช็คระยะ)
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ถ้าคนที่เดินมาชนมี Tag ว่า "Player" ให้โชว์ปุ่ม F
        if (other.CompareTag("Player") && promptIcon != null)
        {
            promptIcon.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // ถ้า Player เดินออกจากระยะ ให้ซ่อนปุ่ม F
        if (other.CompareTag("Player") && promptIcon != null)
        {
            promptIcon.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // ป้องกันบัคภาพค้าง กรณีไอเทมถูกทำลายจังหวะที่ปุ่มยังโชว์อยู่
        if (promptIcon != null) 
        {
            promptIcon.SetActive(false);
        }
    }
}