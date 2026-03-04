using UnityEngine;

public class Heart : MonoBehaviour, ICollectible
{
    [SerializeField] private float _healthAmount;

    public void Collect(Collector collector)
    {
        if (collector.TryGetComponent<Health>(out Health health))
        {
            health.AddHealth(_healthAmount);
        }

        Destroy(gameObject);
    }
}
