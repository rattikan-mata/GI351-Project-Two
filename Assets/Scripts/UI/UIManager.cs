using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    #region [MAIN MENU]
    [Header("[MAIN MENU]")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    #endregion

    #region [GAME PLAY]
    [Header("[GAME PLAY]")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button gameplayExitButton;
    #endregion

    #region [HP BAR]
    [Header("[HP BAR]")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Scrollbar playerScrollbar;

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Scrollbar enemyScrollbar;
    #endregion

    #region [INVENTORY]
    [System.Serializable]
    public class InventorySlotUI
    {
        [Tooltip("Item - Prefabs")]
        public GameObject itemPrefab;

        [Tooltip("Slot - sprites")]
        public Image slotSprite;

        [Tooltip("Number - texts")]
        public TextMeshProUGUI numberText;
    }

    [Header("[INVENTORY]")]
    [SerializeField] private InventorySlotUI[] inventorySlots = new InventorySlotUI[4];

    [Header("Slot Colors")]
    [SerializeField] private Color normalSlotColor = Color.white;
    [SerializeField] private Color selectedSlotColor = Color.yellow;
    #endregion

    #region [SCENE SETTINGS]
    [Header("[SCENE SETTINGS]")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private int mainMenuBuildIndex = 0;
    [SerializeField] private int gameplayBuildIndex = 1;
    #endregion

    private int currentSelectedSlot = 0;
    private bool isPaused = false;
    private Sprite[] defaultSlotSprites;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        defaultSlotSprites = new Sprite[inventorySlots.Length];
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].slotSprite != null)
            {
                defaultSlotSprites[i] = inventorySlots[i].slotSprite.sprite;
            }
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        SetupButtons();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        InitializeStartingItems();
        UpdateSlotSelectionVisual(0);
    }

    private void Update()
    {
        HandlePauseInput();

        if (!isPaused)
        {
            HandleInventorySelectionInput();
            UpdateInventoryUI();
        }

        UpdateHealthBarsRealtime();
    }

    #region 1. HP BAR (ลดจากขวามาซ้าย)
    private void UpdateHealthBarsRealtime()
    {
        if (playerScrollbar != null)
        {
            float playerHP = 0f;
            float playerMaxHP = 20f;

            if (PlayerController.Instance != null)
            {
                playerHP = PlayerController.Instance.CurrentHP;
                playerMaxHP = PlayerController.Instance.MaxHP;
            }
            else if (playerPrefab != null && playerPrefab.TryGetComponent<PlayerController>(out var pc))
            {
                playerHP = pc.CurrentHP;
                playerMaxHP = pc.MaxHP;
            }

            float fill = playerMaxHP > 0f ? Mathf.Clamp01(playerHP / playerMaxHP) : 0f;
            playerScrollbar.direction = Scrollbar.Direction.LeftToRight;
            playerScrollbar.value = 0f;
            playerScrollbar.size = fill;
        }

        if (enemyScrollbar != null)
        {
            float enemyHP = 0f;
            float enemyMaxHP = 20f;
            Monster enemyTarget = null;

            if (enemyPrefab != null && enemyPrefab.scene.IsValid())
            {
                enemyTarget = enemyPrefab.GetComponent<Monster>();
            }

            if (enemyTarget == null)
            {
#if UNITY_2023_1_OR_NEWER
                enemyTarget = FindFirstObjectByType<Monster>();
#else
                enemyTarget = FindObjectOfType<Monster>();
#endif
            }

            if (enemyTarget != null)
            {
                enemyHP = enemyTarget.CurrentHP;
                enemyMaxHP = enemyTarget.MaxHP;
            }

            float fill = enemyMaxHP > 0f ? Mathf.Clamp01(enemyHP / enemyMaxHP) : 0f;
            enemyScrollbar.direction = Scrollbar.Direction.LeftToRight;
            enemyScrollbar.value = 0f;
            enemyScrollbar.size = fill;
        }
    }
    #endregion

    #region 2. INVENTORY & ITEM MANAGEMENT
    private void InitializeStartingItems()
    {
        if (PlayerController.Instance == null) return;
        var playerSlots = PlayerController.Instance.InventorySlots;
        if (playerSlots == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i >= playerSlots.Length) break;

            if (inventorySlots[i] != null && inventorySlots[i].itemPrefab != null)
            {
                WorldItem worldItem = inventorySlots[i].itemPrefab.GetComponent<WorldItem>();
                if (worldItem != null && worldItem.Data != null)
                {
                    playerSlots[i].item = worldItem.Data;
                    playerSlots[i].currentDurability = (worldItem.CurrentDurability > 0)
                        ? worldItem.CurrentDurability
                        : worldItem.Data.maxDurability;
                    continue;
                }
            }

            playerSlots[i].item = null;
            playerSlots[i].currentDurability = 0;
        }
    }

    private void UpdateInventoryUI()
    {
        if (PlayerController.Instance == null) return;
        var playerSlots = PlayerController.Instance.InventorySlots;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            Image img = inventorySlots[i].slotSprite;
            TextMeshProUGUI txt = inventorySlots[i].numberText;

            if (i < playerSlots.Length && playerSlots[i] != null && playerSlots[i].item != null)
            {
                var slotData = playerSlots[i];
                ItemData item = slotData.item;

                if (img != null)
                {
                    img.enabled = true;
                    img.sprite = (item.icon != null) ? item.icon : PlaceholderIconFactory.GetPlaceholder(item.itemType);
                }

                if (txt != null)
                {
                    if (item.maxDurability > 0)
                    {
                        txt.text = slotData.currentDurability.ToString();
                    }
                    else if (item.amount > 0)
                    {
                        txt.text = item.amount.ToString();
                    }
                    else
                    {
                        txt.text = "1";
                    }
                    txt.gameObject.SetActive(true);
                }
            }
            else
            {
                if (img != null)
                {
                    img.enabled = true;
                    img.sprite = (defaultSlotSprites != null && i < defaultSlotSprites.Length) ? defaultSlotSprites[i] : null;
                }

                if (txt != null)
                {
                    txt.text = "";
                    txt.gameObject.SetActive(false);
                }
            }
        }
    }

    private void HandleInventorySelectionInput()
    {
        if (PlayerController.Instance != null)
        {
            currentSelectedSlot = PlayerController.Instance.ActiveSlotIndex;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) currentSelectedSlot = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) currentSelectedSlot = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) currentSelectedSlot = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) currentSelectedSlot = 3;
        }

        UpdateSlotSelectionVisual(currentSelectedSlot);
    }

    private void UpdateSlotSelectionVisual(int activeIndex)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].slotSprite != null)
            {
                inventorySlots[i].slotSprite.color = (i == activeIndex) ? selectedSlotColor : normalSlotColor;
            }
        }
    }
    #endregion

    #region 3. SCENE MANAGEMENT & PAUSE
    private void SetupButtons()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (exitButton != null) exitButton.onClick.AddListener(QuitGame);

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (gameplayExitButton != null) gameplayExitButton.onClick.AddListener(ExitToMainMenu);
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

    public void StartGame()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            SceneManager.LoadScene(gameplaySceneName);
        else
            SceneManager.LoadScene(gameplayBuildIndex);
    }

    public void ResumeGame()
    {
        if (pausePanel == null) return;
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuBuildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion
}