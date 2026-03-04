using UnityEngine;

public class HealthIncreaseHeart : MonoBehaviour, ICollectible
{
    [SerializeField] private float _healthAmount;
    [SerializeField] private bool _restoreHealth;

    public void Collect(Collector collector)
    {
        if (collector.TryGetComponent<Health>(out Health health))
        {
            health.MaxHealth += _healthAmount;
            if(_restoreHealth)
                health.RestoreFullHealth();
        }

        Destroy(gameObject);
    }
}
