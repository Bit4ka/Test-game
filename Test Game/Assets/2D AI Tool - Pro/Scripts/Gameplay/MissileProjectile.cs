using AI2DTool;
using UnityEngine;

namespace MaykerStudio
{
    public class MissileProjectile : Projectile, IDamageable
    {
        [BeginGroup("Missile options")]
        [SerializeField]
        private float DetectionRadius = 5f;

        [SerializeField]
        private float MaxTurnAngle = 45f;

        [Min(1f)]
        [SerializeField]
        [Tooltip("How much the speed should be multiplied when a target is detected.")]
        private float SpeedMultiplierOnTarget = 2f;

        [SerializeField]
        [Min(0f)]
        [ShowIf(nameof(CanBeDamaged), true)]
        private float MaxHealth = 100;

        [SerializeField]
        private bool StopTrackingAtMaxSpeed;

        [SerializeField]
        private bool CanBeDamaged;

        [EndGroup]
        [SerializeField]
        private bool CanSeeThroughWalls;

        [SerializeField]
        [ProgressBar("Health %", 0f, 100f, HexColor = "#ff0000")]
        [ShowIf(nameof(CanBeDamaged), true)]
        private float inspectorHealth;

        private bool TargetAquired, TargetSpotted;

        private float CurrentHealth;

        private float currentAngle, originalAngle;

        private float lerpTime;

        private Vector2 InitialDirection;

        private Collider2D Target;

        public override void OnEnable()
        {
            CurrentHealth = MaxHealth;
            lerpTime = 0;
            currentAngle = 0;
            originalAngle = 0;
            InitialDirection = default;
        }

        public override void Update()
        {
            base.Update();

            if (Target)
            {
                Vector2 targerDir = ((Vector2)Target.transform.position - rb.position).normalized;

                currentAngle = Mathf.Atan2(Mathf.Abs(targerDir.y), targerDir.x) * Mathf.Rad2Deg;

                if (CanSeeThroughWalls)
                {
                    TargetAquired = true;
                    SetMoveDirection(targerDir);
                }
                else
                {
                    if (TargetSpotted)
                    {
                        TargetAquired = true;
                        SetMoveDirection(targerDir);
                    }
                }
            }
            else
            {
                TargetAquired = false;
            }


            if (CurrentHealth <= 0 && CanBeDamaged)
            {
                ReturnToPool(0f);
                return;
            }

#if UNITY_EDITOR
            inspectorHealth = CurrentHealth / MaxHealth * 100f;
#endif

        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (Target == null)
                Target = Physics2D.OverlapCircle(transform.position, DetectionRadius, whatIsTarget);

            if (Target != null && !CanSeeThroughWalls && !TargetAquired)
            {
                if (!Physics2D.Linecast(transform.position, Target.transform.position, whatIsGround))
                {
                    TargetSpotted = true;

#if UNITY_EDITOR
                    Debug.DrawLine(transform.position, Target.transform.position);
#endif
                }
            }
        }

        public override void SetMoveDirection(Vector2 dir)
        {
            if (!TargetAquired)
            {
                base.SetMoveDirection(dir);

                if(InitialDirection == default)
                {
                    InitialDirection = dir;
                    originalAngle = Mathf.Atan2(Mathf.Abs(InitialDirection.y), InitialDirection.x) * Mathf.Rad2Deg;
                }
            }
            else if(Mathf.Abs(currentAngle - originalAngle) <= MaxTurnAngle &&
                (StopTrackingAtMaxSpeed && lerpTime < 1 || !StopTrackingAtMaxSpeed))
            {
                lerpTime += Time.deltaTime;
                rb.linearVelocity = Vector2.Lerp(InitialDirection * speed, dir * Mathf.Lerp(speed, speed * SpeedMultiplierOnTarget, lerpTime), lerpTime);
            }
        }

        public override void OnDisable()
        {
            TargetAquired = false;
            TargetSpotted = false;
            Target = null;

            base.OnDisable();
        }

        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Gizmos.DrawWireSphere(transform.position, DetectionRadius);
        }

        public void Damage(DamageDetails details)
        {
            CurrentHealth -= details.damageAmount;
        }

        public void KnockBack(float knockBackLevel, float knockBackDuration, Vector2 knockBackDirection)
        {
            
        }
    }
}