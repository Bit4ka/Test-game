using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;

    private bool _isActive;
    private Vector2 _shootDirection;

    [SerializeField] private float _timeToActivate;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _collider.enabled = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_isActive) return;

        _rb.linearVelocity = _shootDirection;
    }

    public void FireProjectile(Vector3 direction)
    {
        _isActive = true;
        _shootDirection = direction;
        Invoke(nameof(EnableCollider), _timeToActivate);
    }

    // To not destroy on player
    private void EnableCollider()
    {
        _collider.enabled = true;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Shoud take damage from npc
        Destroy(gameObject);
    }
}
