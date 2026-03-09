using MaykerStudio;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/2D Platformer Only/Drop Attack State")]
    public class E_DropAttack : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation dropAttackAnimation;

        [Min(0.1f)]
        [SerializeField]
        private float dropSpeed = 5f;

        [Min(0.0f)]
        [SerializeField]
        private float attackDamage = 10f;

        [Min(0)]
        [SerializeField]
        private int stunAmount = 10;

        [Min(0)]
        [SerializeField]
        private int knockBackLevel = 4;

        [SerializeField]
        private Vector2 knockBackDirection = Vector2.right;

        [Range(0f, 1f)]
        [SerializeField]
        private float knockBackDuration = 0.2f;

        [Min(0.1f)]
        [SerializeField]
        private float attackRadius = 2f;

        [Min(0.0f)]
        [SerializeField]
        [EndGroup]
        private float delayBeforeExitState = 0.5f;
        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If entity is grounded")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [EndGroup]
        [Header("If entity got damaged")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState2;

        #endregion

        private float delayCounter;

        private bool isGrounded;

        private Vector3 rotation;

        private bool actionInvoked;

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

            Collider2D[] cols = Physics2D.OverlapCircleAll(EntityAI.transform.position, attackRadius, EntityAI.entityData.whatIsTarget);

            foreach (Collider2D col in cols)
            {
                if (col.gameObject != EntityAI.gameObject)
                {
                    EntityAI.SendDamage(damageDetails, col.gameObject, knockBackLevel, knockBackDuration, knockBackDirection.normalized);
                }
            }
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.IgnoreFallClamp = true;

            delayCounter = 0f;

            rotation = EntityAI.transform.rotation.eulerAngles;

            rotation.z = 0f;

            EntityAI.transform.rotation = Quaternion.Euler(rotation);

            damageDetails.damageAmount = attackDamage;
            damageDetails.stunDamageAmount = stunAmount;

            EntityAI.FlipToTarget();

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            EntityAI.PlayAnim(dropAttackAnimation);
            EntityAI.PlayAudio(dropAttackAnimation.SoundAsset);

            actionInvoked = false;
        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.IgnoreFallClamp = false;
            EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (EntityAI.IsDamaged)
            {
                if (NextState2 != null && !NextState2.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState2);
                    return;
                }
            }

            if (isGrounded && Time.time > startTime + 0.1f)
            {
                if (!actionInvoked)
                {
                    EntityAI.EntityDelegates.OnDropAttackEnd?.Invoke(EntityAI);
                    actionInvoked = true;
                }
                delayCounter += Time.deltaTime;
                if (delayCounter >= delayBeforeExitState)
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                        StateMachine.ChangeState(NextState1);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            EntityAI.SetVelocityY(-dropSpeed);
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

            ExtDebug.DrawEllipse(EntityAI.transform.position, attackRadius, attackRadius, 32, Color.red);
        }

#endif
    }
}