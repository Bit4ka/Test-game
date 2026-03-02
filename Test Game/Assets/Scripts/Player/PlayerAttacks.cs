using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerAttacks : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _bulletSpeed;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _fireCooldown;
    private bool _canShoot = true;
    private Vector2 _shootDirection;

    private void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;

        _shootDirection = (mousePos - transform.position).normalized;
        _firePoint.localPosition = _shootDirection;
    }

    #region Player Attacks
    public void ShootBullet(InputAction.CallbackContext context)
    {
        if (context.performed && _canShoot)
        {
            _canShoot = false;
            Invoke(nameof(AllowShooting), _fireCooldown);

            GameObject bullet = Instantiate(_bulletPrefab, _firePoint.position, Quaternion.identity);

            if (bullet.TryGetComponent<IProjectile>(out IProjectile projectile))
            {
                projectile.Fire(_shootDirection * _bulletSpeed);
            }
        }
    }

    private void AllowShooting() => _canShoot = true;
    #endregion
}
