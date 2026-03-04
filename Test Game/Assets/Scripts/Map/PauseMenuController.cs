using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;

public class PauseMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root, pauseBg, optionsPanel;
    private Button continueBtn, optionsBtn, exitBtn, optionsBackBtn, pauseToggleBtn;
    private Slider volumeSlider;
    private bool isPaused = false;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        var rootVE = uiDocument.rootVisualElement;  // Root VisualElement
        pauseBg = rootVE.Q<VisualElement>("pause-bg");
        optionsPanel = rootVE.Q<VisualElement>("options-panel");
        pauseToggleBtn = rootVE.Q<Button>("pause-toggle-btn");
        continueBtn = rootVE.Q<Button>("continue-btn");
        optionsBtn = rootVE.Q<Button>("options-btn");
        exitBtn = rootVE.Q<Button>("exit-btn");
        optionsBackBtn = rootVE.Q<Button>("options-back-btn");
        volumeSlider = rootVE.Q<Slider>("volume-slider");

        // Events
        continueBtn.clicked += OnResume;
        optionsBtn.clicked += OnToggleOptions;
        exitBtn.clicked += OnExitToMenu;
        optionsBackBtn.clicked += OnToggleOptions;
        pauseToggleBtn.clicked += OnTogglePause;

        // UKRYJ MENU NA START (display: none = całkowicie usuń z renderu!)
        SetPauseMenuVisible(false);
        SetOptionsVisible(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            OnTogglePause();
    }

    private void OnTogglePause()
    {
        isPaused = !isPaused;
        SetPauseMenuVisible(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        AudioListener.pause = isPaused;
        //Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        //Cursor.visible = isPaused;
    }

    private void SetPauseMenuVisible(bool visible)
    {
        pauseBg.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;  // Flex bo column w USS
        pauseToggleBtn.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;  // Ukryj przycisk w rogu PODCZAS pauzy
    }

    private void OnResume()
    {
        // Zamknij options jeśli otwarte
        SetOptionsVisible(false);
        OnTogglePause();
    }

    private void OnToggleOptions()
    {
        bool isVisible = optionsPanel.resolvedStyle.display == DisplayStyle.Flex;
        SetOptionsVisible(!isVisible);
    }

    private void SetOptionsVisible(bool visible)
    {
        optionsPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnExitToMenu()
    {
        Time.timeScale = 1f;
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        SceneManager.LoadScene("MainMenu");  // Zmień na swoją scenę
    }
}