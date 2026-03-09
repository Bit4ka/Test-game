using MaykerStudio;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/Dash Attack State")]
    public class E_DashAttack : EntityState
    {
        #region Variables
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation dashAnimation;

        [SerializeField]
        [Tooltip("This animation will play when the state starts.")]
        private EntityAnimation preparingAnimation;

        [Min(0.0f)]
        [SerializeField]
        private float preparingDuraton = 0.5f;

        [Min(1f)]
        [SerializeField]
        private float dashSpeed = 30f;

        [Min(0.1f)]
        [SerializeField]
        [Tooltip("The max distance from the initial position the entity can go.")]
        private float dashMaxDistance = 10f;

        [Min(0.1f)]
        [SerializeField]
        [Tooltip("The max duration of the dash force.")]
        private float maxDashDuration = .4f;

        [Min(0.0f)]
        [SerializeField]
        private float attackDamage = 10f;

        [Min(0)]
        [SerializeField]
        private int stunAmount = 10;

        [Min(0)]
        [SerializeField]
        private int knockBackLevel = 4;

        [Range(0f, 1f)]
        [SerializeField]
        private float knockBackDuration = 0.2f;

        [Min(0.01f)]
        [SerializeField]
        [Tooltip("Radius damage detection. See the red circle when the state starts.")]
        private float attackRadius = 1f;

        [Min(0f)]
        [SerializeField]
        [ShowIf(nameof(BounceBackOnWalls), true)]
        private float bounceBackDuration = 1f;

        [Min(0.1f)]
        [SerializeField]
        [ShowIf(nameof(BounceBackOnWalls), true)]
        private float bounceSpeed = 1f;

        [SerializeField]
        [ShowIf(nameof(BounceBackOnWalls), true)]
        [Tooltip("The force angle that will be applied. Please, use values and between 0 and 1 on both axis.")]
        private Vector2 BounceBackAngle = new Vector2(1f, 0.5f);

        [SerializeField]
        [Tooltip("Check if the state should stop if detects a ledge. ONLY WORK WITH PLATFORMER.")]
        private bool StopOnLedge;

        [SerializeField]
        private bool BounceBackOnWalls = true;

        [EndGroup]
        [SerializeField]
        private bool entityFlying;
        #endregion

        #region Transitions
        [BeginGroup("Transitions")]
        [Header("If maxDistance achieve Or duration is over")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [EndGroup]
        [Header("If target hitted")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState2;
        #endregion

        private bool isDashing, hasHittedTarget, hasHittedWall, IsDetectingLedge;

        private float dashCounter, bounceBackCounter;

        private Vector3 startPos;

        private Vector3 targetDirection;

        private Collider2D[] targets = new Collider2D[10];

        private Transform target;

        public override void AnimationFinish()
        {
            base.AnimationFinish();
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();
        }

        public override void AnimationTrigger2()
        {
            base.AnimationTrigger2();
        }

        public override void DoChecks()
        {
            base.DoChecks();

            if (StopOnLedge)
                IsDetectingLedge = EntityAI.CheckLedge();

            if (isDashing && !hasHittedTarget)
            {
                int count = Physics2D.OverlapCircleNonAlloc(EntityAI.attackCheck.position, attackRadius, targets, EntityAI.entityData.whatIsTarget);

                for (int i = 0; i < count; i++)
                {
                    if (targets[i] == null)
                        break;
                    else
                    {
                        hasHittedTarget = true;

                        if (attackDamage > 0)
                        {
                            EntityAI.SendDamage(damageDetails, targets[i].gameObject, knockBackLevel, knockBackDuration, Vector2.right * EntityAI.FacingDirection);
                        }
                    }
                }
            }
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.SeeThroughWalls = true;
            target = EntityAI.GetFirstTargetTransform();
            EntityAI.SeeThroughWalls = CanSeeThroughWalls;

            if(target)
                EntityAI.FlipToTarget(target.position, true);

            EntityAI.IgnoreFallClamp = true;
            
            dashCounter = 0f;

            EntityAI.Flying = entityFlying;

            damageDetails.damageAmount = attackDamage;
            damageDetails.stunDamageAmount = stunAmount;

            startPos = EntityAI.transform.position;

            EntityAI.SetVelocityX(0f);

            EntityAI.PlayAnim(preparingAnimation);
            EntityAI.PlayAudio(preparingAnimation.SoundAsset);

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
        }

        public override void Exit()
        {
            base.Exit();

            hasHittedWall = false;
            EntityAI.IgnoreFallClamp = false;
            EntityAI.Flying = false;
            EntityAI.StopKnockBack = false;
            isDashing = false;
            hasHittedTarget = false;

            bounceBackCounter = 0f;

            EntityAI.EntityDelegates.OnDashAttackEnd?.Invoke(EntityAI);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            //If the user set the 'PlayDamagedAnim' it will reset state start time for the preparation.

            if (hasHittedWall)
            {
                bounceBackCounter += Time.deltaTime;

                if(bounceBackCounter >= bounceBackDuration)
                {
                    EntityAI.SetVelocity(Vector2.zero);
                }
            }

            if (EntityAI.IsDamaged && PlayDamagedAnim)
            {
                startTime = Time.time;
            }

            if (isDashing)
            {
                dashCounter += Time.deltaTime;

                if (hasHittedTarget)
                {
                    if (NextState2 != null && !NextState2.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState2);
                    }
                }
            }

            if (!isDashing)
            {
                EntityAI.PlayAnim(preparingAnimation);
                EntityAI.PlayAudio(preparingAnimation.SoundAsset);

                if (Time.time >= startTime + preparingDuraton)
                {
                    if (target)
                    {
                        targetDirection = (target.position - EntityAI.transform.position).normalized;
                        EntityAI.FlipToTarget(target.position, true);

                        isDashing = true;
                        EntityAI.PlayAnim(dashAnimation);
                        EntityAI.PlayAudio(dashAnimation.SoundAsset);

                        startTime = Time.time;

                        EntityAI.StopKnockBack = true;

                        EntityAI.EntityDelegates.OnDashAttackStart?.Invoke(EntityAI);
                    }
                    else
                    {
                        dashCounter = maxDashDuration + 1;
                    }
                }
            }

            if (Vector3.Distance(startPos, EntityAI.transform.position) > dashMaxDistance && isDashing)
            {
                if (Time.time >= startTime + 0.1f || (StopOnLedge && !IsDetectingLedge))
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                        StateMachine.ChangeState(NextState1);
                }
            }
            else
            {
                if (dashCounter >= maxDashDuration || (StopOnLedge && !IsDetectingLedge))
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                        StateMachine.ChangeState(NextState1);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (!hasHittedWall)
            {
                if (!EntityAI.Flying && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                {
                    if (isDashing && Vector3.Distance(startPos, EntityAI.transform.position) < dashMaxDistance && (StopOnLedge && IsDetectingLedge || !StopOnLedge))
                    {
                        if (BounceBackOnWalls && EntityAI.CheckWall())
                        {
                            EntityAI.SetVelocity(bounceSpeed, BounceBackAngle, -EntityAI.FacingDirection);
                            hasHittedWall = true;
                            dashCounter = maxDashDuration - bounceBackDuration;
                        }
                        else
                        {
                            EntityAI.SetVelocityX(dashSpeed * EntityAI.FacingDirection);
                        }
                    }
                    else
                        EntityAI.SetVelocityX(0f);
                }

                else if (EntityAI.Flying || EntityAI.entityData.gameType == D_Entity.GameType.Topdown2D)
                {
                    if (isDashing && Vector3.Distance(startPos, EntityAI.transform.position) < dashMaxDistance)
                    {
                        if (BounceBackOnWalls && EntityAI.CheckWall())
                        {
                            EntityAI.SetVelocity(-targetDirection * bounceSpeed);
                            hasHittedWall = true;
                            dashCounter = maxDashDuration - bounceBackDuration;
                        }
                        else
                        {
                            EntityAI.SetVelocity(targetDirection * dashSpeed);
                        }
                        
                    }
                    else
                        EntityAI.SetVelocity(Vector2.zero);
                }
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return base.GetValue(port);
        }


#if UNITY_EDITOR
        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();

            ExtDebug.DrawEllipse(EntityAI.attackCheck.position, attackRadius, attackRadius, 32, Color.red);
        }

#endif
    }
}