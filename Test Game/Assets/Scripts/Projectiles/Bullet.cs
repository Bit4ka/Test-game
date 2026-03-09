using AI2DTool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IProjectile
{
    [Header("Components")]
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;
    private SpriteRenderer _spriteRenderer;

    private bool _isActive;
    private Vector2 _shootDirection;

    [Header("Settings")]
    [SerializeField] private float _damage;
    [SerializeField] private float _timeToDestroy;
    [SerializeField] private ParticleSystem _destroyFX; 

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (!_isActive) return;

        _rb.linearVelocity = _shootDirection;
    }

    public void Fire(Vector3 direction)
    {
        _isActive = true;
        _shootDirection = direction;

        Invoke(nameof(LifeTimeEnd), _timeToDestroy);
    }

    private void LifeTimeEnd() => StartCoroutine(DestroyBullet());

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Warstwy, o które pocisk powinien móc siê rozbiæ. Do przerobienia
        if (!Layers.ProjectileHitLayers.Contains(collision.gameObject.layer) || collision.CompareTag("IgnoreProjectiles"))
            return;


        if (collision.TryGetComponent<Health>(out Health targetHealth))
            targetHealth.TakeDamage(_damage);

        if (collision.TryGetComponent<IDamageable>(out IDamageable health))
        {
            DamageDetails details = new DamageDetails() { damageAmount = _damage, stunDamageAmount = 2, sender = gameObject, position = -_shootDirection };
            health.Damage(details);
        }

        StartCoroutine(DestroyBullet());
    }

    private IEnumerator DestroyBullet()
    {
        _spriteRenderer.enabled = false;
        _collider.enabled = false;
        _isActive = false;
        _rb.linearVelocity = Vector2.zero;

        _destroyFX.Play();
        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }
}
