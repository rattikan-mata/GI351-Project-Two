using System;
using System.Collections;
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
    #endregion

    #region [EFFECTS & FEEDBACK]
    [Header("[EFFECTS & FEEDBACK]")]
    [Tooltip("Prefab ตัวเลขลอย (มี TextMeshPro หรือ TextMeshProUGUI)")]
    [SerializeField] private GameObject damageTextPrefab;

    [Header("Low HP Screen Vignette")]
    [Tooltip("Image สีแดงครอบทั้งหน้าจอใน Canvas")]
    [SerializeField] private Image lowHealthVignette;
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float pulseSpeed = 4f;

    [Header("Screen Shake Settings")]
    [Tooltip("ลาก Camera.main หรือปล่อยว่างไว้ให้ดึงอัตโนมัติ")]
    [SerializeField] private Camera targetCamera;
    private Coroutine shakeCoroutine;
    private Vector3 originalCameraLocalPos;
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
    [SerializeField] private string gameplaySceneName = "MAP TEST";
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

        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            originalCameraLocalPos = targetCamera.transform.localPosition;
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

        if (lowHealthVignette != null)
        {
            lowHealthVignette.enabled = false;
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
        UpdateLowHealthVignette();
    }

    #region 1. HP BAR & LOW HP EFFECT
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
    }

    private void UpdateLowHealthVignette()
    {
        if (lowHealthVignette == null || PlayerController.Instance == null) return;

        float maxHP = PlayerController.Instance.MaxHP;
        float currentHP = PlayerController.Instance.CurrentHP;

        if (maxHP <= 0f) return;

        float ratio = currentHP / maxHP;

        if (ratio <= lowHealthThreshold && ratio > 0f)
        {
            float intensity = 1f - (ratio / lowHealthThreshold);
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.2f, 0.7f, intensity * pulse);

            Color color = lowHealthVignette.color;
            color.a = alpha;
            lowHealthVignette.color = color;
            lowHealthVignette.enabled = true;
        }
        else
        {
            lowHealthVignette.enabled = false;
        }
    }
    #endregion

    #region 2. DAMAGE/HEAL TEXT & SCREEN SHAKE (PUBLIC API)
    public void ShowCombatText(Vector3 worldPos, int amount, bool isHeal)
    {
        if (damageTextPrefab == null) return;

        GameObject textObj = Instantiate(damageTextPrefab, worldPos + Vector3.up * 1f, Quaternion.identity);
        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = isHeal ? $"+{amount}" : $"-{amount}";
            tmp.color = isHeal ? Color.green : Color.red;
        }

        StartCoroutine(CombatTextFadeRoutine(textObj));
    }

    private IEnumerator CombatTextFadeRoutine(GameObject textObj)
    {
        float duration = 0.8f;
        float speed = 1.2f;
        float elapsed = 0f;

        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>();
        Color startColor = tmp != null ? tmp.color : Color.white;

        while (elapsed < duration)
        {
            if (textObj == null) yield break;

            textObj.transform.position += Vector3.up * (speed * Time.deltaTime);

            if (tmp != null)
            {
                float alpha = Mathf.Clamp01(1f - (elapsed / duration));
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(textObj);
    }

    public void TriggerScreenShake(float duration = 3f, float magnitude = 0.25f)
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main;
            originalCameraLocalPos = targetCamera.transform.localPosition;
        }

        if (targetCamera == null) return;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ScreenShakeRoutine(duration, magnitude));
    }

    private IEnumerator ScreenShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            targetCamera.transform.localPosition = originalCameraLocalPos + new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        targetCamera.transform.localPosition = originalCameraLocalPos;
        shakeCoroutine = null;
    }
    #endregion

    #region 3. INVENTORY & ITEM MANAGEMENT
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

    #region 4. SCENE MANAGEMENT & PAUSE
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