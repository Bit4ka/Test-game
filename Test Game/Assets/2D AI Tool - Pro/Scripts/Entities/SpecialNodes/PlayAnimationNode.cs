using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Play Animation")]
    [NodeTint("#696f78")]
    [NodeWidth(210)]
    public class PlayAnimationNode : EntityState
    {
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation Animation;

        [SerializeField]
        [Tooltip("Use the AnimationTrigger1 event to move the character with the animation")]
        private Movements Movement;

        [SerializeField]
        private bool entityFlying;

        [SerializeField]
        [Tooltip("If checked, the character will move to the target. If not, it'll move in the direction it's facing.")]
        private bool MoveToClosestTarget;

        [SerializeField]
        [Tooltip("Check this if you want to ignore the cooldown of the NexState")]
        private bool IgnoreStateCooldown;

        [EndGroup]
        [SerializeField]
        [Header("On Animation Finish")]
        [Tooltip("The transition happens when the AnimationFinish event is called on animation.")]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        protected EntityState NextState;

        private Vector2 moveDirection;

        private float movementTimer = 0f;

        private bool move;

        public override void AnimationFinish()
        {
            base.AnimationFinish();
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            if (Movement.movementSpeed > 0 && Movement.movementDuration > 0)
            {
                movementTimer = 0f;

                if (MoveToClosestTarget)
                {
                    Transform target = EntityAI.GetFirstTargetTransform();

                    if (target)
                    {
                        moveDirection = (target.position - EntityAI.transform.position).normalized;
                        EntityAI.FlipToTarget(target.position, true);
                    }
                }

                move = true;
            }
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.PlayAnim(Animation);
            EntityAI.PlayAudio(Animation.SoundAsset);

            NextState = CheckState(NextState);

            EntityAI.Flying = entityFlying;
            EntityAI.Rb.gravityScale = entityFlying ? 0 : EntityAI.OriginalGravityScale;
        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.Flying = false;
            EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
        }

        public override object GetValue(NodePort port)
        {
            base.GetValue(port);

            NextState = GetFromPort("NextState", port, NextState);

            return NextState;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            EntityAI.PlayAnim(Animation);

            if (isAnimationFinished)
            {
                if (NextState != null)
                {
                    if(IgnoreStateCooldown)
                        StateMachine.ChangeState(NextState);

                    else if (!NextState.IsInCooldown)
                        StateMachine.ChangeState(NextState);
                }
                    
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (move)
            {
                movementTimer += Time.deltaTime;

                if (movementTimer < Movement.movementDuration)
                {
                    if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                    {
                        EntityAI.SetVelocityX(Movement.movementSpeed * EntityAI.FacingDirection);
                    }
                    else if (EntityAI.Flying || EntityAI.entityData.gameType == D_Entity.GameType.Topdown2D)
                    {
                        EntityAI.SetVelocity(moveDirection * Movement.movementSpeed);
                    }
                }
                else
                {
                    move = false;
                    EntityAI.SetVelocity(Vector2.zero);
                }
            }
        }

        [System.Serializable]
        public class Movements
        {
            [Min(0f)]
            public float movementSpeed;
            [Min(0f)]
            public float movementDuration;
        }
    }
}