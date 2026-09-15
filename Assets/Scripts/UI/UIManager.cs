using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    #region [Scene]
    [Header("[Scene]")]
    [SerializeField] private int mainMenuScene = 0;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    [SerializeField] private int gamePlayScene = 1;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button pauseExitButton;
    #endregion

    #region [Player]
    [Header("[Player]")]
    [SerializeField] private Scrollbar playerHpScrollbar;
    [SerializeField] private Image[] inventory = new Image[4];
    [SerializeField] private Color selectedSlotColor = Color.yellow;
    [SerializeField] private Color normalSlotColor = Color.white;
    #endregion

    #region [Enemy]
    [Header("[Enemy]")]
    [SerializeField] private Scrollbar enemyHpScrollbar;
    #endregion

    private int currentSelectedSlot = 0;
    private bool isPaused = false;

    public event Action<int> OnUseItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        SetupButtons();

        SelectInventorySlot(0);

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        HandlePauseInput();

        if (!isPaused)
        {
            HandleInventoryInput();
        }
    }

    #region PLAYER & INVENTORY
    public void UpdatePlayerHP(float currentHP, float maxHP)
    {
        if (playerHpScrollbar != null)
        {
            float normalized = (maxHP > 0f) ? Mathf.Clamp01(currentHP / maxHP) : 0f;
            playerHpScrollbar.size = normalized;
            playerHpScrollbar.value = normalized;
        }
    }

    private void HandleInventoryInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectInventorySlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectInventorySlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectInventorySlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectInventorySlot(3);

        if (Input.GetMouseButtonDown(0))
        {
            UseCurrentSelectedItem();
        }
    }

    public void SelectInventorySlot(int index)
    {
        if (inventory == null || inventory.Length == 0) return;
        if (index < 0 || index >= inventory.Length) return;

        currentSelectedSlot = index;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] != null)
            {
                inventory[i].color = (i == currentSelectedSlot) ? selectedSlotColor : normalSlotColor;
            }
        }
    }

    private void UseCurrentSelectedItem()
    {
        OnUseItem?.Invoke(currentSelectedSlot);
    }
    #endregion

    #region ENEMY HP
    public void UpdateEnemyHP(float currentHP, float maxHP)
    {
        if (enemyHpScrollbar != null)
        {
            float normalized = (maxHP > 0f) ? Mathf.Clamp01(currentHP / maxHP) : 0f;
            enemyHpScrollbar.size = normalized;
            enemyHpScrollbar.value = normalized;
        }
    }
    #endregion

    #region SCENE MANAGEMENT (เชื่อมด้วย Build Index ไม่ใช้ String)
    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gamePlayScene);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;

        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (pausePanel == null) return;

        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void SetupButtons()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (exitButton != null) exitButton.onClick.AddListener(QuitGame);

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (pauseExitButton != null) pauseExitButton.onClick.AddListener(ExitToMainMenu);
    }
    #endregion
}