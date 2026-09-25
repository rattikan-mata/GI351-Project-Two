using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Modular Sub-Controllers")]
    [SerializeField] private UIPlayerHealthView healthView;
    [SerializeField] private UIInventoryView inventoryView;
    [SerializeField] private UIPauseMenuView pauseMenuView;
    [SerializeField] private UICameraEffect cameraEffect;
    [SerializeField] private UICombatTextSpawner combatTextSpawner;

    [Header("Scene Config")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "MAP TEST";

    private bool isPaused = false;

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

        if (pauseMenuView != null)
        {
            pauseMenuView.Initialize(
                onResume: ResumeGame,
                onRestart: RestartGame,
                onExit: ExitToMainMenu
            );
        }
    }

    private void Update()
    {
        HandlePauseInput();
        UpdatePlayerHealth();
        UpdateInventory();
    }

    private void UpdatePlayerHealth()
    {
        if (healthView == null || PlayerController.Instance == null) return;
        healthView.UpdateHealthView(PlayerController.Instance.CurrentHP, PlayerController.Instance.MaxHP);
    }

    private void UpdateInventory()
    {
        if (inventoryView == null || PlayerController.Instance == null) return;

        var slots = PlayerController.Instance.InventorySlots;
        int activeIdx = PlayerController.Instance.ActiveSlotIndex;

        for (int i = 0; i < 4; i++)
        {
            if (slots != null && i < slots.Length && slots[i]?.item != null)
            {
                var item = slots[i].item;
                Sprite icon = item.icon != null ? item.icon : PlaceholderIconFactory.GetPlaceholder(item.itemType);
                string text = item.maxDurability > 0 ? slots[i].currentDurability.ToString() : (item.amount > 0 ? item.amount.ToString() : "1");
                inventoryView.UpdateSlotDisplay(i, icon, text, i == activeIdx);
            }
            else
            {
                inventoryView.UpdateSlotDisplay(i, null, "", i == activeIdx);
            }
        }
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuView != null) pauseMenuView.SetVisible(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuView != null) pauseMenuView.SetVisible(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowCombatText(Vector3 position, int amount, bool isHeal)
    {
        if (combatTextSpawner != null) combatTextSpawner.Spawn(position, amount, isHeal);
    }

    public void TriggerScreenShake(float duration = 3f, float magnitude = 0.25f)
    {
        if (cameraEffect != null) cameraEffect.Shake(duration, magnitude);
    }
}