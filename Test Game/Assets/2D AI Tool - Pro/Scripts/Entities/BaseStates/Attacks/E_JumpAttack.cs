using MaykerStudio;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/2D Platformer Only/Jump Attack State")]
    public class E_JumpAttack : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation JumpAnimation;

        [SerializeField]
        private EntityAnimation FallingAnimation;

        [SerializeField]
        private EntityAnimation LandAnimation;

        [SerializeField]
        [Tooltip("This animation will play when the state starts")]
        private EntityAnimation PreparingAnimation;

        [SerializeField]
        [Tooltip("The angle of the jump, you can put normalized values between 0 and 1.")]
        private Vector2 jumpAngle;

        [Min(0.0f)]
        [SerializeField]
        private float jumpSpeed = 5f;

        [Min(0.0f)]
        [SerializeField]
        private float jumpDuration = .5f;

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("The delay before changing the preparing animation to jump animation and apply the force.")]
        private float delayBeforeJump = 0.5f;

        [Min(0.0f)]
        [SerializeField]
        private float damageAmount = 10f;

        [Min(0)]
        [SerializeField]
        private int stunAmount = 10;

        [Min(0)]
        [SerializeField]
        private int knockBackLevel = 4;

        [Range(0f, 1f)]
        [SerializeField]
        private float knockBackDuration = 0.2f;

        [SerializeField]
        private Vector2 knockBackDirection = Vector2.right;

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("This radius is shown as a red circle with the gizmos is enabled.")]
        private float damageRadius = 1f;

        [Min(0.0f)]
        [SerializeField]
        private float targetBellowCheckDistance = 3f;

        [EndGroup]
        [SerializeField]
        private bool stopVelocityOnEnd = true;
        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If entity is grounded")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState1;

        [Header("If target Hitted")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState2;

        [EndGroup]
        [Header("If target bellow")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState3;

        #endregion

        private float delayCounter, jumpCounter;

        private bool isGrounded, IsJumping, hasJumpOnce, targetsHitted, targetBellow;

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

            isGrounded = EntityAI.CheckGround();

            targetBellow = Physics2D.Raycast(EntityAI.transform.position, -Vector2.down, targetBellowCheckDistance, EntityAI.entityData.whatIsTarget);

            if (IsJumping && !targetsHitted)
            {
                Collider2D[] cols = Physics2D.OverlapCircleAll(EntityAI.transform.position, damageRadius, EntityAI.entityData.whatIsTarget);

                foreach (Collider2D col in cols)
                {
                    if (col.gameObject != EntityAI.gameObject)
                    {
                        targetsHitted = true;
                        EntityAI.SendDamage(damageDetails, col.gameObject, knockBackLevel, knockBackDuration, knockBackDirection.normalized);
                    }
                }
            }
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.IgnoreFallClamp = true;

            EntityAI.PlayAnim(PreparingAnimation);
            EntityAI.PlayAudio(PreparingAnimation.SoundAsset);

            damageDetails.damageAmount = damageAmount;
            damageDetails.stunDamageAmount = stunAmount;

            EntityAI.FlipToTarget();

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
            NextState3 = CheckState(NextState3);
        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.IgnoreFallClamp = false;
            targetsHitted = false;
            IsJumping = false;
            hasJumpOnce = false;
            delayCounter = 0f;

            if (stopVelocityOnEnd)
                EntityAI.SetVelocityX(0f);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (targetsHitted)
            {
                if (NextState2 != null && !NextState2.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState2);
                    return;
                }

            }

            if (!IsJumping && !hasJumpOnce)
                delayCounter += Time.deltaTime;

            if (delayCounter >= delayBeforeJump)
            {
                if (!IsJumping)
                {
                    EntityAI.PlayAnim(JumpAnimation);
                    EntityAI.PlayAudio(JumpAnimation.SoundAsset);

                    IsJumping = true;

                    startTime = Time.time;

                    EntityAI.FlipToTarget();
                }

                if (EntityAI.Rb.linearVelocity.y < -0.1f && !isGrounded)
                {
                    EntityAI.PlayAnim(FallingAnimation);
                    EntityAI.PlayAudio(FallingAnimation.SoundAsset);
                }
                else if (isGrounded && Time.time >= startTime + 0.05f)
                {
                    EntityAI.PlayAnim(LandAnimation);
                    EntityAI.PlayAudio(LandAnimation.SoundAsset);
                }

                #region Transitions
                if (isGrounded && Time.time >= startTime + 0.1f && IsJumping)
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState1);
                        return;
                    }
                }

                if (targetBellow)
                {
                    if (NextState3 != null && !NextState3.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState3);
                        return;
                    }
                }
                #endregion
            }

            if (IsJumping && !hasJumpOnce)
            {
                jumpCounter += Time.deltaTime;
                if (jumpCounter >= jumpDuration)
                {
                    jumpCounter = 0f;
                    hasJumpOnce = true;

                    if (stopVelocityOnEnd)
                        EntityAI.SetVelocityY(0f);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (IsJumping && !hasJumpOnce)
            {
                EntityAI.SetVelocity(jumpSpeed, jumpAngle, EntityAI.FacingDirection);
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);
            NextState3 = GetFromPort("NextState3", port, NextState3);

            return base.GetValue(port);
        }

#if UNITY_EDITOR
        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();

            ExtDebug.DrawEllipse(EntityAI.transform.position, damageRadius, damageRadius, 32, Color.red);

            Debug.DrawRay(EntityAI.transform.position, -Vector2.up * targetBellowCheckDistance, Color.blue);
        }

#endif
    }
}