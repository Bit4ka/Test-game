using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerChargedAttack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private BoxCollider2D _playerCollider;
    [SerializeField] private Image _loadCircle;
    [SerializeField] private ParticleSystem _burstFX;

    [Header("Attack Settings")]
    [SerializeField] private float _damage;
    [SerializeField] private float _chargeDuration;
    [SerializeField] private float _attackCooldown;
    [SerializeField] private float _attackRange;
    [SerializeField] private LayerMask _enemyLayer;
    
    private bool _canAttack = true;
    private float _chargeTimer;
    private bool _isHolding;

    private void Update()
    {
        if (_isHolding && _canAttack)
        {
            _chargeTimer += Time.deltaTime;
            _loadCircle.fillAmount = _chargeTimer / _chargeDuration;

            if( _chargeTimer >= _chargeDuration)
            {
                _canAttack = false;
                ResetHolding();
                Attack();
                Invoke(nameof(AllowAttacking), _attackCooldown);
            }
        }
    }

    public void OnHold(InputAction.CallbackContext context)
    {
        if (context.started) _isHolding = true;
        else if (context.canceled) ResetHolding();
    }

    private void ResetHolding()
    {
        _isHolding = false;
        _chargeTimer = 0;
        _loadCircle.fillAmount = 0;
    }

    private void AllowAttacking() => _canAttack = true;

    private void Attack()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(_playerCollider.bounds.center, _attackRange, _enemyLayer);

        _burstFX.Play();

        foreach(Collider2D enemy in enemies)
        {
            if(enemy.gameObject.TryGetComponent<Health>(out Health enemyHealth))
            {
                enemyHealth.TakeDamage(_damage);
                Debug.Log($"Atak wykonany, zosta³o HP {enemyHealth.CurrentHealth}");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_playerCollider.bounds.center, _attackRange);
    }
}
