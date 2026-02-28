using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Private Fields
    [Header("Components")]
    private Rigidbody2D _rb;
    private Animator _animator;

    [Header("Player Movement")]
    [SerializeField] private float _moveSpeed;
    private Vector2 _movement;

    #endregion

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _movement * _moveSpeed;
    }

    #region Player Movement

    public void Move(InputAction.CallbackContext context)
    {
        _animator.SetBool("IsWalking", true);

        if (context.canceled)
        {
            _animator.SetBool("IsWalking", false);
            _animator.SetFloat("LastInputX", _movement.x);
            _animator.SetFloat("LastInputY", _movement.y);
        }

        _movement = context.ReadValue<Vector2>();

        _animator.SetFloat("InputX", _movement.x);
        _animator.SetFloat("InputY", _movement.y);
    }
    #endregion
}
