using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("ใส่ชื่อซีนตอนเล่นเกม (เช่น MAP TEST 1)")]
    public string gameplaySceneName = "MAP TEST 1"; 

    [Tooltip("ใส่ชื่อซีนหน้าเมนูหลัก (เช่น MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    // ฟังก์ชันสำหรับปุ่ม "เริ่มเกมใหม่"
    public void RestartGame()
    {
        ResetStoryState();
        Time.timeScale = 1f; // เผื่อมีการ Pause เกมไว้ ให้เวลาเดินปกติ
        SceneManager.LoadScene(gameplaySceneName);
    }

    // ฟังก์ชันสำหรับปุ่ม "กลับเมนเมนู"
    public void GoToMainMenu()
    {
        ResetStoryState();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ฟังก์ชันรีเซ็ตเนื้อเรื่อง
    private void ResetStoryState()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.isTaneeDefeated = false;
            StoryManager.Instance.hasHouseKey = false;
        }
    }
}