using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Components")]
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Rigidbody2D _rb;
    private PlayerController _playerController;

    [Header("Health")]
    public float MaxHealth;
    public float CurrentHealth { get; private set; }

    [Header("IFrames")]
    [SerializeField] private int _numberOfFlashes;
    [SerializeField] private float _iFramesTime;

    private void Start()
    {
        CurrentHealth = MaxHealth;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _playerController = GetComponent<PlayerController>();
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth);

        if(CurrentHealth > 0)
        {
            StartCoroutine(Invulnerability());
        }
        else
        {
            if(_rb != null)
                _rb.linearVelocity = Vector2.zero;

            if(_playerController != null)
                _playerController.enabled = false;

            if (_animator != null)
                _animator.SetTrigger("Die");

            Invoke(nameof(Deactivate), 2);
        }
    }

    public void AddHealth(float health)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + health, 0, MaxHealth);
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = MaxHealth;
    }

    private void Deactivate() => gameObject.SetActive(false);

    // Gracz przez okreslony czas nie dostaje obra¿eñ po uderzeniu
    private IEnumerator Invulnerability()
    {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        Color defaultColor = _spriteRenderer.color;

        for(int i =0; i < _numberOfFlashes; i++)
        {
            _spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(_iFramesTime / (_numberOfFlashes * 2));
            _spriteRenderer.color = defaultColor;
            yield return new WaitForSeconds(_iFramesTime / (_numberOfFlashes * 2));
        }

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
    }
}
