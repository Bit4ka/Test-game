using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaykerStudio
{
    public class LevelSelector : MonoBehaviour
    {
        [SceneName]
        public string platformer;

        [SceneName]
        public string topdown;

        [SceneName]
        public string shooter;

        public GameObject Buttons;
        public Slider LoadingBar;

        private bool isLoading;

        public void PlayPlatformer()
        {
            if (!isLoading)
                StartCoroutine(LoadAsync(platformer));
        }

        public void PlayTopdown()
        {
            if (!isLoading)
                StartCoroutine(LoadAsync(topdown));
        }

        public void PlayShooter()
        {

        }

        public IEnumerator LoadAsync(string scene)
        {
            isLoading = true;
            Buttons.SetActive(false);
            LoadingBar.gameObject.SetActive(true);

            AsyncOperation op = SceneManager.LoadSceneAsync(scene);

            while (!op.isDone)
            {
                float progress = Mathf.Clamp01(op.progress / 0.9f);

                LoadingBar.value = progress;

                yield return null;
            }
        }

    }
}