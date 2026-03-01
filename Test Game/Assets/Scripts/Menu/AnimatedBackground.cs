using UnityEngine;
using UnityEngine.UIElements;

public class AnimatedBackground : MonoBehaviour
{
    public Sprite[] frames;
    public float frameRate = 12f;

    private VisualElement background;
    private int currentFrame;
    private float timer;
    private bool initialized = false;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        background = root.Q<VisualElement>("Background");

        if (background != null)
            initialized = true;
    }

    void Update()
    {
        if (!initialized || background == null || frames.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            background.style.backgroundImage =
                new StyleBackground(frames[currentFrame]);
        }
    }

    void OnDisable()
    {
        initialized = false;
    }
}