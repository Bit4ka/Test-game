using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument nie jest podpięty!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("rootVisualElement jest null!");
            return;
        }

        Debug.Log("Szukam przycisków...");

        var startButton = root.Q<Button>("StartButton");
        if (startButton != null)
        {
            Debug.Log("Znaleziono StartButton – podpinam kliknięcie");
            startButton.clicked += OnStartClicked;
        }
        else
        {
            Debug.LogError("Nie znaleziono przycisku 'StartButton'");
        }

        var exitButton = root.Q<Button>("ExitButton");
        if (exitButton != null)
        {
            Debug.Log("Znaleziono ExitButton – podpinam kliknięcie");
            exitButton.clicked += OnExitClicked;
        }
        else
        {
            Debug.LogError("Nie znaleziono przycisku 'ExitButton'");
        }
        

        var allButtons = root.Query<Button>().ToList();
        Debug.Log("Znaleziono przycisków: " + allButtons.Count);
        foreach (var btn in allButtons)
        {
            Debug.Log("Przycisk: " + btn.name);
        }
    }

    private void OnStartClicked()
    {
        Debug.Log("=== KLIKNIĘTO START ===");
        Debug.Log("Aktualna scena: " + SceneManager.GetActiveScene().name);
        Debug.Log("Scena SampleScene jest dostępna – ładuję...");

        SceneManager.LoadScene("SampleScene");
    }

    private void OnExitClicked()
    {
        Debug.Log("=== KLIKNIĘTO EXIT ===");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        
        #else
        Application.Quit();
        #endif
    }
}