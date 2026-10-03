using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    // ตัวแปรจำสถานะต่างๆ
    public bool isTaniDefeated = false;
    public bool hasHouseKey = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // ทำให้ตัวจำสถานะนี้อยู่ตลอดไป ไม่หายตอนเปลี่ยนฉาก
    }
}