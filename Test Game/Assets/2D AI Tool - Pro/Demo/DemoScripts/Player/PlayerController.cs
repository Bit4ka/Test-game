using AI2DTool;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MaykerStudio.Demo
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public GameType gameType;

        [HideIf(nameof(gameType), GameType.TopDown2D)]
        public Transform groundCheck;

        [HideIf(nameof(gameType), GameType.TopDown2D)]
        public float groundCheckRadius = 2;

        public LayerMask whatIsGround;

        public LayerMask whatIsEnemies;
        public Transform attackCheck;

        public float moveSpeed = 5;

        public int amountOfJumps = 2;

        [HideIf(nameof(gameType), GameType.TopDown2D)]
        public float JumpForce = 10;

        [HideIf(nameof(gameType), GameType.TopDown2D)]
        public float jumpDuration = .1f;

        public int fireRate = 10;

        public float attackDamage = 10f, stunDamageAmount = 1;

        public LayersObject layersOBJ;

        public GameObject projectilePrefab;

        public Transform firePositionTransform;

        public Transform projectilesHolder;

        public float projectileSpeed = 20f;

        #region Private Vars

        private bool isGrounded, isJumping, moveLeft, moveRight, isAttacking, move, hopDamage;

        private int facingDirection, currentJump;

        private float originalGravity, jumpStartTime, hopDuration, fireStartTime;

        private Rigidbody2D RB;

        private Animator anim;

        private DamageDetails attackDetails;

        private EntityObjectPool objectPool;

        private Vector2 movementDirection;
        private Vector2 prevMovementDirection;
        private Vector2 hopDamageSpeed;

        #endregion

        #region UNITY_CALLBACKS

        private void OnEnable()
        {
            fireStartTime = Time.time + fireRate / 10;
        }

        void Start()
        {
            currentJump = amountOfJumps;
            facingDirection = 1;
            RB = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            originalGravity = RB.gravityScale;

            gameObject.layer = layersOBJ.PlayerLayer;

            whatIsEnemies |= 1 << layersOBJ.EntityLayer;
            whatIsGround |= 1 << layersOBJ.WhatIsGround;

            objectPool = gameObject.AddComponent<EntityObjectPool>();

            attackDetails.stunDamageAmount = stunDamageAmount;
            attackDetails.damageAmount = attackDamage;
            attackDetails.sender = gameObject;
        }

        void Update()
        {
            attackDetails.position = transform.position;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current.rightButton.isPressed)
#else
            if (Input.GetMouseButton(1))
#endif
            {
                if (Time.unscaledTime >= fireStartTime + fireRate / 10)
                {
                    Shoot();

                    fireStartTime = Time.unscaledTime;
                }
            }

            if (gameType == GameType.Platformer2D && !hopDamage)
            {

#if ENABLE_INPUT_SYSTEM
                moveLeft = Keyboard.current.aKey.isPressed;
                moveRight = Keyboard.current.dKey.isPressed;
#else
                moveLeft = Input.GetKey(KeyCode.A);
                moveRight = Input.GetKey(KeyCode.D);
#endif

#if ENABLE_INPUT_SYSTEM
                if (Mouse.current.leftButton.wasPressedThisFrame)
#else
                if (Input.GetMouseButton(0))
#endif
                {
                    if (isGrounded)
                        Attack();
                }
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
#else
                if (Input.GetKeyDown(KeyCode.Space))
#endif
                {
                    currentJump--;
                    Jump();
                }

#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current.spaceKey.wasReleasedThisFrame)
#else
                if (Input.GetKeyUp(KeyCode.Space))
#endif
                {
                    isJumping = false;

                    if (currentJump > 0)
                        RB.linearVelocity = new Vector2(RB.linearVelocity.x, 1f);
                }

#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current.spaceKey.isPressed)
#else
                if (Input.GetKey(KeyCode.Space))
#endif
                {
                    if (isJumping)
                        RB.linearVelocity = Vector2.up * JumpForce;
                }

                if (RB.linearVelocity.y < 0 && !isGrounded)
                {
                    RB.gravityScale = 5f;
                }
                else
                {
                    RB.gravityScale = originalGravity;
                }


                if (isJumping && Time.time >= jumpStartTime + jumpDuration)
                {
                    isJumping = false;
                    RB.linearVelocity = new Vector2(RB.linearVelocity.x, 0f);
                }

#if ENABLE_INPUT_SYSTEM
                Flip(Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0);
#else
                Flip(Input.GetAxis("Horizontal"));
#endif
            }
            else if(!hopDamage)
            {
#if ENABLE_INPUT_SYSTEM
                move = Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed;
#else
                move = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
#endif
            }

            Animations();

            if (hopDamage)
            {
                hopDuration += Time.deltaTime;
                if(hopDuration >= 0.1f)
                {
                    hopDamage = false;
                    hopDuration = 0f;

                    RB.linearVelocity = Vector2.zero;
                }
            }
        }

        void FixedUpdate()
        {
            if (gameType == GameType.Platformer2D)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

                currentJump = isGrounded ? amountOfJumps : currentJump;

                if (isAttacking)
                {
                    RB.linearVelocity = Vector2.zero;
                }
                else if (moveLeft)
                {
                    RB.linearVelocity = new Vector2(-moveSpeed, RB.linearVelocity.y);
                }
                else if (moveRight)
                {
                    RB.linearVelocity = new Vector2(moveSpeed, RB.linearVelocity.y);
                }
                else
                {
                    RB.linearVelocity = new Vector2(0.0f, RB.linearVelocity.y);
                }
            }
            else
            {
                if (move)
                {
                    move = false;

#if ENABLE_INPUT_SYSTEM
                    RB.linearVelocity = new Vector2(Keyboard.current.aKey.isPressed ? -1f : Keyboard.current.dKey.isPressed ? 1f : 0f * moveSpeed,
                        Keyboard.current.sKey.isPressed ? -1f : Keyboard.current.wKey.isPressed ? 1f : 0f * moveSpeed);
#else
                    RB.velocity = new Vector2(Input.GetAxisRaw("Horizontal") * moveSpeed, Input.GetAxisRaw("Vertical") * moveSpeed);
#endif
                }
            }

            if (hopDamage)
            {
                RB.linearVelocity = hopDamageSpeed;
            }
        }

#endregion

#region Attack entity
        public void TriggerAttack()
        {
            //If you want your attack to damage all entities in the radius use OverlapCircleAll, otherwise only call OverlapCircle
            Collider2D[] cols = Physics2D.OverlapCircleAll(attackCheck.position, 1, whatIsEnemies);

            foreach (Collider2D c in cols)
            {
                if (c)
                    SendDamage(attackDetails, c.gameObject);
            }
        }

#endregion

#region Damage Functions

        public void SendDamage(DamageDetails details, GameObject target)
        {
            if(target.TryGetComponent(out IDamageable d))
            {
                d.Damage(details);
                d.KnockBack(4, 0.2f, (target.transform.position - transform.position).normalized);
            }
        }

        public void KnockBack(float knockBackLevel, float knockBackDuration, Vector2 knockBackDirection)
        {
            
        }

        public void Damage(DamageDetails details)
        {
            //Reduce player health

            hopDamage = true;

            if(gameType == GameType.Platformer2D)
            {
                if (details.position.x > transform.position.x)
                    hopDamageSpeed = new Vector2(-10f, RB.linearVelocity.y);
                else
                    hopDamageSpeed = new Vector2(10f, RB.linearVelocity.y);
            }
            else
            {
                hopDamageSpeed = - (details.position - (Vector2)transform.position) * 10f;
            }
        }

#endregion

        public void AnimationTrigger1() { }

        public void AnimationFinish() { }

        public void FinishAttack()
        {
            isAttacking = false;
        }

#region Animations Handling
        void Animations()
        {
            if (!isAttacking)
            {
                if (gameType == GameType.Platformer2D)
                {
                    if (isGrounded)
                    {
                        if (moveRight || moveLeft)
                            anim.Play("Run");
                        else
                            anim.Play("Idle");
                    }
                    else
                    {
                        anim.Play("Jump");
                    }
                }
                else
                {
                    movementDirection = RB.linearVelocity.normalized;

                    if (movementDirection != Vector2.zero)
                        prevMovementDirection = movementDirection;

                    //Debug.Log(movementDirection);

                    anim.SetFloat("xVelocity", movementDirection.x);
                    anim.SetFloat("yVelocity", movementDirection.y);

                    anim.SetFloat("prevDirectionX", prevMovementDirection.x);
                    anim.SetFloat("prevDirectionY", prevMovementDirection.y);

                    if (RB.linearVelocity.magnitude > 0)
                        anim.Play("Run");
                    else
                        anim.Play("Idle");
                }
            }

            else
            {
                anim.Play("Attack");
            }
        }

#endregion

        void Jump()
        {
            if (!isJumping && currentJump > 0)
            {
                jumpStartTime = Time.time;

                isJumping = true;

                RB.linearVelocity = Vector2.up * JumpForce;
            }
        }

        void Attack()
        {
            if (!isAttacking)
                isAttacking = true;
        }

        void Shoot()
        {
            GameObject projectile = objectPool.Get(projectilePrefab, firePositionTransform.position, projectilesHolder);
            Projectile projScript = projectile.GetComponent<Projectile>();

            projScript.ObjectPool = objectPool;

            projScript.whatIsGround |= 1 << layersOBJ.WhatIsGround;
            projScript.whatIsTarget |= 1 << layersOBJ.EntityLayer;

#if ENABLE_INPUT_SYSTEM
            Vector2 dir = (Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
#else
            Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
#endif

            projScript.FireProjectile(projectileSpeed, dir.normalized, 15, attackDamage, 1f);
        }

        private void OnDrawGizmos()
        {
            if (groundCheck != null && gameType == GameType.Platformer2D)
            {
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

            }
        }

        void Flip(float xVelocity)
        {
            if (xVelocity > 0 && facingDirection != 1)
            {
                Vector3 s = transform.localScale;

                s.x *= facingDirection;

                transform.localScale = s;

                facingDirection = 1;
            }
            else if (xVelocity < 0 && facingDirection != -1)
            {
                facingDirection = -1;

                Vector3 s = transform.localScale;

                s.x *= facingDirection;

                transform.localScale = s;
            }
        }
    }

    public enum GameType
    {
        Platformer2D,
        TopDown2D
    }
}