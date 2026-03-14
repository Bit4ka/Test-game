using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; // jeśli używasz nowego Input System
using UnityEngine.SceneManagement;

public class PauseMenuMinimal : MonoBehaviour
{
    [SerializeField] private InputSystem_Actions inputActions; // przeciągnij asset tutaj (jeśli masz nowy input)

    private VisualElement pausePanel;      // główny kontener pauzy
    private VisualElement optionsPanel;    // panel ustawień
    private bool isPaused = false;

    private void Awake()
    {
        if (inputActions == null)
            inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        // Input ESC (jeśli masz nowy system)
        if (inputActions != null)
        {
            inputActions.UI.Pause.performed += _ => TogglePause();
            inputActions.UI.Enable();
        }

        // Pobierz elementy UI (bezpiecznie)
        var root = GetComponent<UIDocument>()?.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("Brak UIDocument lub root!");
            return;
        }

        pausePanel = root.Q<VisualElement>("pause-panel"); // lub "pause-bg"
        optionsPanel = root.Q<VisualElement>("options-panel");

        // Ukryj wszystko na wszelki wypadek (czasami runtime nadpisuje UXML)
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.None;
        if (optionsPanel != null) optionsPanel.style.display = DisplayStyle.None;

        // Przyciski – podpinamy tylko jeśli istnieją
        var continueBtn = root.Q<Button>("continue-btn");
        if (continueBtn != null) continueBtn.clicked += () => Resume();

        var optionsBtn = root.Q<Button>("options-btn");
        if (optionsBtn != null) optionsBtn.clicked += () => ShowOptions();

        var exitBtn = root.Q<Button>("exit-btn");
        if (exitBtn != null) exitBtn.clicked += () => ExitGame();

        // Opcjonalny przycisk w rogu
        var cornerBtn = root.Q<Button>("pause-toggle-btn");
        if (cornerBtn != null) cornerBtn.clicked += () => TogglePause();

        // Debug – sprawdź czy ukryte
        Debug.Log("Pauza ukryta na starcie: " + (pausePanel?.style.display.value == DisplayStyle.None));
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.UI.Pause.performed -= _ => TogglePause();
            inputActions.UI.Disable();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel != null)
            pausePanel.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;

        Time.timeScale = isPaused ? 0f : 1f;
        AudioListener.pause = isPaused;

        UnityEngine.Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = isPaused;
    }

    private void Resume()
    {
        if (optionsPanel != null)
            optionsPanel.style.display = DisplayStyle.None;

        if (isPaused)
            TogglePause();
    }

    private void ShowOptions()
    {
        if (pausePanel != null)
            pausePanel.style.display = DisplayStyle.None;

        if (optionsPanel != null)
            optionsPanel.style.display = DisplayStyle.Flex;
    }

    private void ExitGame()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        // Application.Quit();              // w buildzie
        SceneManager.LoadScene("MainMenu"); // lub co chcesz
    }
}