using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    public bool isTaneeDefeated = false;
    public bool hasHouseKey = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // เพิ่มฟังก์ชันนี้เพื่อเคลียร์สถานะทั้งหมดให้กลับเป็นค่าเริ่มต้น
    public void ResetStory()
    {
        isTaneeDefeated = false;
        hasHouseKey = false;
    }
}