using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacks : MonoBehaviour
{
    [Header("Player Attacks")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _bulletSpeed;

    #region Player Attacks
    public void ShootBullet(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            Vector3 shootDirection = (mousePos - transform.position).normalized;

            GameObject bullet = Instantiate(_bulletPrefab.gameObject, transform.position, Quaternion.identity);
            
            // Zamieniæ na interfejs
            if(bullet.TryGetComponent<Projectile>(out Projectile projectile)){
                projectile.FireProjectile(shootDirection *  _bulletSpeed);
            }
        }
    }
    #endregion
}
