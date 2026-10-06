using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndingManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Text แสดงเนื้อเรื่องฉากจบ")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    
    [Tooltip("ปุ่ม Next สำหรับกดอ่านบรรทัดถัดไป")]
    [SerializeField] private Button nextButton;
    
    [Tooltip("ปุ่ม Exit สำหรับกดออกจากฉากจบ")]
    [SerializeField] private Button exitButton;

    [Header("Ending Text Content")]
    [TextArea(3, 6)]
    [Tooltip("ใส่ข้อความฉากจบแต่ละบรรทัดที่นี่ (กด + เพิ่มข้อความได้เรื่อยๆ)")]
    [SerializeField] private List<string> dialogueLines = new List<string>();

    [Header("Scene Transition Settings")]
    [Tooltip("ชื่อซีนเมนูหลักที่จะโหลดกลับไปเมื่อกดปุ่ม Exit")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Typewriter Effect Settings")]
    [Tooltip("เปิด/ปิดระบบตัวหนังสือค่อยๆ พิมพ์ทีละตัว")]
    [SerializeField] private bool useTypewriterEffect = true;
    [Tooltip("ความเร็วในการพิมพ์ตัวอักษรแต่ละตัว (ค่าน้อยยิ่งเร็ว)")]
    [SerializeField] private float typingSpeed = 0.05f;

    private int currentIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private void Start()
    {
        // เมื่อเริ่มฉาก: แสดงปุ่ม Next และซ่อนปุ่ม Exit ไว้ก่อน
        if (nextButton != null) nextButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(false);

        // เชื่อมฟังก์ชันกดปุ่มอัตโนมัติ
        if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);

        // แสดงข้อความบรรทัดแรก
        if (dialogueLines.Count > 0)
        {
            ShowCurrentLine();
        }
        else
        {
            ShowExitButton();
        }
    }

    /// 
    /// ฟังก์ชันทำงานเมื่อผู้เล่นกดปุ่ม Next
    /// 
    public void OnNextButtonClicked()
    {
        // 1. ถ้าตัวหนังสือกำลังค่อยๆ พิมพ์อยู่ -> กด Next แล้วให้แสดงข้อความเต็มทันที
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = dialogueLines[currentIndex];
            isTyping = false;
            return;
        }

        // 2. ขยับไปข้อความบรรทัดถัดไป
        currentIndex++;

        // 3. เช็กว่ายังมีข้อความเหลืออยู่ไหม
        if (currentIndex < dialogueLines.Count)
        {
            ShowCurrentLine();
        }
        else
        {
            // ถ้าอ่านจนหมดแล้ว ให้เปลี่ยนไปโชว์ปุ่ม Exit
            ShowExitButton();
        }
    }

    private void ShowCurrentLine()
    {
        if (useTypewriterEffect)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeWriterRoutine(dialogueLines[currentIndex]));
        }
        else
        {
            dialogueText.text = dialogueLines[currentIndex];
        }
    }

    private IEnumerator TypeWriterRoutine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void ShowExitButton()
    {
        // ซ่อนปุ่ม Next และเปิดปุ่ม Exit ขึ้นมาแทน
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
    }

    /// 
    /// ฟังก์ชันทำงานเมื่อผู้เล่นกดปุ่ม Exit
    /// 
    public void OnExitButtonClicked()
    {
        AudioManager.Instance?.PlaySFX("button_click");
        
        // โหลดกลับไปยังหน้า Main Menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
}