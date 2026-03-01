using System.Collections;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Shoud take damage from npc
        StartCoroutine(DestroyBullet());
    }

    private IEnumerator DestroyBullet()
    {
        _destroyFX.Play();
        _spriteRenderer.enabled = false;
        _collider.enabled = false;
        _isActive = false;
        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }


}
