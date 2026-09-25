using UnityEngine;
using UnityEngine.UI;

public class UIPauseMenuView : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    public void Initialize(System.Action onResume, System.Action onRestart, System.Action onExit)
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(() => onResume?.Invoke());
        if (restartButton != null) restartButton.onClick.AddListener(() => onRestart?.Invoke());
        if (exitButton != null) exitButton.onClick.AddListener(() => onExit?.Invoke());
        SetVisible(false);
    }

    public void SetVisible(bool isVisible)
    {
        if (menuRoot != null) menuRoot.SetActive(isVisible);
    }
}