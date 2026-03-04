using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health _playerHealth;
    [SerializeField] private Image _totalHealthBar;
    [SerializeField] private Image _currentHealthbar;

    void Update()
    {
        _totalHealthBar.fillAmount = _playerHealth.MaxHealth / 10;
        _currentHealthbar.fillAmount = _playerHealth.CurrentHealth / 10;
    }
}
