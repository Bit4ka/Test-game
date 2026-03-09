using AI2DTool;
using System.Collections;
using UnityEngine;

namespace MaykerStudio
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        protected DamageDetails damageDetails;

        [BeginGroup]
        [SerializeField]
        protected float gravity = 8f;

        [SerializeField]
        [Tooltip("The delay before getting the projectile to the pool or destroying it.")]
        protected float OnHitGroundDelay = 0f;

        [SerializeField]
        protected bool enableGravityOnMaxTravel;

        [SerializeField]
        [Tooltip("If checked, the projectile will be destroyed instead of going back to the pool.")]
        protected bool destroyProjectile;

        [SerializeField]
        protected bool UseTags;

        [SerializeField]
        [TagSelector]
        [ShowIf(nameof(UseTags), true)]
        protected string TargetTag;

        public LayerMask whatIsGround, whatIsTarget;

        [EndGroup]
        [SerializeField]
        protected GameObject SpawnOnDisable;

        protected Rigidbody2D rb;

        protected float speed;

        protected float startTime;

        protected float maxTravelDistance, delayCounter;

        protected Vector2 StartPos;

        protected TrailRenderer trailRenderer;

        protected bool isGravityOn;
        protected bool hasHitGround;

        protected Vector2 moveDir;

        protected GameObject CachedTarget;

        private LayerMask CombinedLayers;

        public AI2DTool.EntityObjectPool ObjectPool { get; set; }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            TryGetComponent(out trailRenderer);

            if (trailRenderer == null)
                trailRenderer = GetComponentInChildren<TrailRenderer>();
        }

        private void Start()
        {
            rb.gravityScale = 0.0f;

            StartPos = transform.position;

            isGravityOn = false;
            hasHitGround = false;

            delayCounter = 0;

            CombinedLayers = whatIsGround | whatIsTarget;
        }

        public virtual void Update()
        {
            if (!hasHitGround)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        public virtual void FixedUpdate()
        {
            if (gameObject.activeSelf)
            {
                //Doing this because the projectile can be too fast for the collision to detect the right position of the projectile.
                damageDetails.position.Set(transform.position.x - moveDir.x, transform.position.y - moveDir.y);

#if UNITY_EDITOR
                ExtDebug.DrawEllipse(damageDetails.position, .1f, .1f, 32, Color.magenta);
#endif

                if (Mathf.Abs(Vector3.Distance(StartPos, transform.position)) >= maxTravelDistance && !enableGravityOnMaxTravel)
                {
                    ReturnToPool();
                }
                else if (Mathf.Abs(Vector3.Distance(StartPos, transform.position)) >= maxTravelDistance && !isGravityOn && enableGravityOnMaxTravel)
                {
                    isGravityOn = true;
                    rb.gravityScale = gravity;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (gameObject.activeSelf && collision.gameObject.activeSelf)
                HandleCollision(collision);
        }

        protected virtual void HandleCollision(Collider2D collision)
        {
            if (CachedTarget)
            {
                if (collision.gameObject != CachedTarget) CachedTarget = collision.gameObject;
            }
            else
            {
                CachedTarget = collision.gameObject;
            }

            if (whatIsTarget == (whatIsTarget | (1 << CachedTarget.layer)))
            {
                if (UseTags)
                {
                    if (CachedTarget.CompareTag(TargetTag))
                    {
                        if (CachedTarget.TryGetComponent(out IDamageable d))
                        {
                            ReturnToPool(d);
                        }
                        else
                            ReturnToPool(0f);
                    }
                }
                else
                {
                    if (CachedTarget.TryGetComponent(out IDamageable d))
                    {
                        ReturnToPool(d);
                    }
                    else
                        ReturnToPool(0f);
                }


            }
            else if (whatIsGround == (whatIsGround | (1 << CachedTarget.layer)))
            {
                hasHitGround = true;
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                ReturnToPool(OnHitGroundDelay);
            }
        }

        public void FireProjectile(float speed, Vector2 dir, float maxTravelDistance, float damage, float stunAmount)
        {
            Start();
            this.speed = speed;
            this.maxTravelDistance = maxTravelDistance;
            damageDetails.damageAmount = damage;
            damageDetails.stunDamageAmount = stunAmount;
            damageDetails.sender = gameObject;

            startTime = Time.time;

            SetMoveDirection(dir);
        }

        public virtual void SetMoveDirection(Vector2 dir)
        {
            rb.linearVelocity = dir * speed;

            moveDir = dir;
        }


        private void ReturnToPool()
        {
            if (SpawnOnDisable != null)
            {
                Instantiate(SpawnOnDisable, gameObject.transform.position, Quaternion.identity);
            }

            gameObject.SetActive(false);

#if UNITY_EDITOR
            if (!destroyProjectile && ObjectPool == null)
                Debug.LogWarning(gameObject + " object pool is null. If you want to use it than set the object pool before firing the projectile.");
#endif

            if (ObjectPool && !destroyProjectile)
                ObjectPool.AddToPool(gameObject);
            else
                Destroy(gameObject);
        }

        protected virtual void ReturnToPool(IDamageable targetToDamage)
        {
            targetToDamage.Damage(damageDetails);

            ReturnToPool(0f);
        }

        protected void ReturnToPool(float sec)
        {
            delayCounter += Time.deltaTime;
            if (delayCounter >= sec)
            {
                ReturnToPool();
            }
        }

        public virtual void OnDrawGizmos()
        {
        }
    }
}