using AI2DTool;
using UnityEngine;

namespace MaykerStudio
{
    public class TargetDetectedSprite : MonoBehaviour
    {
        [SerializeField]
        private Entity entity;

        [SerializeField]
        private GameObject detectedSprite;

        [SerializeField]
        private GameObject normalSprite;

        private float timer;

        EntityDelegates entityDelegates;

        private void Start()
        {
            if (entityDelegates == null)
                TryGetComponent(out entityDelegates);

            entityDelegates.OnTargetDetected += OnTargetDetected;
            entityDelegates.OnTargetNotDetected += OnTargetNotDetected;
        }

        private void OnDisable()
        {
            if (entityDelegates == null)
                TryGetComponent(out entityDelegates);

            entityDelegates.OnTargetDetected -= OnTargetDetected;
            entityDelegates.OnTargetNotDetected -= OnTargetNotDetected;
        }

        private void OnTargetNotDetected(Entity entity)
        {
            if (this.entity == entity)
            {
                timer += Time.deltaTime;
                if (timer >= 0.5f)
                {
                    detectedSprite.SetActive(false);
                    normalSprite.SetActive(true);
                }
            }
        }

        private void OnTargetDetected(Entity entity)
        {
            if (this.entity == entity)
            {
                timer = 0f;
                detectedSprite.SetActive(true);
                normalSprite.SetActive(false);
            }
        }
    }
}