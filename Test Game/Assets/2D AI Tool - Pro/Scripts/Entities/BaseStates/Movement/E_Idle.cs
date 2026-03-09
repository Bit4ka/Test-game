using MaykerStudio;
using MaykerStudio.Types;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Idle State")]
    public class E_Idle : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation IdleAnimation;

        [SerializeField]
        [NodeEnum]
        private DetectionType detectionType;

        [Min(0.0f)]
        [SerializeField]
        private float targetsAgroDistance = 10f;

        [Min(0.0f)]
        [SerializeField]
        private float delayBeforeExitState = 0.5f;

        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        [Range(5f, 270f)]
        [SerializeField]
        [EndGroup]
        private float FOV = 15f;

        #endregion

        #region Transition

        [BeginGroup("Transitions")]
        [Header("If target in agro range")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [Header("If NextState1 in cooldown")]
        [SerializeField]
        [Disable]
        [EndGroup]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState2;

        #endregion

        private bool targetInAgro, IsGrounded;

        private float delayCounter;

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

            IsGrounded = EntityAI.CheckGround();

            if (NextState1 == null && NextState2 == null)
                return;

            switch (detectionType)
            {
                case DetectionType.Circle:
                    targetInAgro = EntityAI.CheckTargetsInRadius(targetsAgroDistance);
                    break;
                case DetectionType.Ray:
                    targetInAgro = EntityAI.CheckTargetsInRange(targetsAgroDistance);
                    break;
                case DetectionType.FOV:
                    targetInAgro = EntityAI.CheckTargetsInFieldOfView(FOV, targetsAgroDistance);
                    break;
                case DetectionType.Box:
                    targetInAgro = EntityAI.CheckBox();
                    break;
                default:
                    break;
            }
        }

        public override void Enter()
        {
            base.Enter();

            delayCounter = 0f;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
        }

        public override void Exit()
        {
            base.Exit();

            targetInAgro = false;

            EntityAI.FlipToTarget(targetsAgroDistance);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Transition

            if (targetInAgro)
            {
                delayCounter += Time.deltaTime;

                if (delayCounter >= delayBeforeExitState)
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState1);
                        return;
                    }
                    else if (NextState2 != null && !NextState2.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState2);
                        return;
                    }
                }
            }
            else
            {
                delayCounter = 0f;
            }

            #endregion

            EntityAI.PlayAnim(IdleAnimation);
            EntityAI.PlayAudio(IdleAnimation.SoundAsset);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (IsGrounded || EntityAI.Flying || EntityAI.entityData.gameType == D_Entity.GameType.Topdown2D)
            {
                if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                    EntityAI.SetVelocityX(0f);
                else
                    EntityAI.SetVelocity(Vector2.zero);
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

            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, targetsAgroDistance);
        }
#endif

    }
}