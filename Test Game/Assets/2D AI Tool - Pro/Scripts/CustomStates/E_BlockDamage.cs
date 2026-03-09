using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/Block Damage")]
    public class E_BlockDamage : EntityState
    {
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation BlockAnimation;

        [SerializeField]
        private BlockDirection blockDirection;

        [SerializeField]
        private bool MoveInState;

        [ShowIf(nameof(blockDirection), BlockDirection.Front)]
        [SerializeField]
        [EndGroup]
        private bool ExitOnTargetBehind = true;

        [BeginGroup("Transitions")]
        [Header("If target behind && ExitOnTargetBehind")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        [EndGroup]
        private EntityState NextState1;

        private bool faceDamageDirection;

        private Transform Target;

        public override void Enter()
        {
            base.Enter();

            Target = EntityAI.GetFirstTargetTransform();

            faceDamageDirection = EntityAI.entityData.faceDamageDirection;

            EntityAI.entityData.faceDamageDirection = false; ;

            NextState1 = CheckState(NextState1);

            EntityAI.SetVelocity(Vector2.zero);

            EntityAI.StopKnockBack = true;

            EntityAI.PlayAnim(BlockAnimation);

            EntityAI.PlayAudio(BlockAnimation.SoundAsset);

            EntityAI.EntityDelegates.OnDamageReceive += OnDamagedReceive;
        }


        public override void Exit()
        {
            base.Exit();

            EntityAI.StopKnockBack = false;

            EntityAI.CanBeDamaged = true;

            EntityAI.entityData.faceDamageDirection = faceDamageDirection ;

            EntityAI.EntityDelegates.OnDamageReceive -= OnDamagedReceive;
        }

        public override void DoChecks()
        {
            base.DoChecks();
        }

        private enum BlockDirection
        {
            Front,
            Back,
            Both
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            EntityAI.PlayAnim(BlockAnimation);

            if (ExitOnTargetBehind)
            {
                if (EntityAI.FacingDirection == 1 && Target.position.x - 0.5f < EntityAI.transform.position.x ||
                    EntityAI.FacingDirection == -1 && Target.position.x + 0.5f > EntityAI.transform.position.x)
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState1);
                    }
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (!MoveInState)
            {
                if(EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                {
                    EntityAI.SetVelocityX(0f);
                }
                else
                {
                    EntityAI.SetVelocity(Vector2.zero);
                }
            }
        }

        public void OnDamagedReceive(Entity entity, DamageDetails details)
        {
            switch (blockDirection)
            {
                case BlockDirection.Front:
                    if (EntityAI.IsFacingPosition(details.position))
                        EntityAI.CanBeDamaged = false;
                    else
                        EntityAI.CanBeDamaged = true;
                    break;

                case BlockDirection.Back:
                    if (EntityAI.IsFacingPosition(details.position))
                        EntityAI.CanBeDamaged = true;
                    else
                        EntityAI.CanBeDamaged = false;
                    break;

                case BlockDirection.Both:
                    EntityAI.CanBeDamaged = false;
                    break;
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);

            return port.Connection?.node;
        }
    }
}