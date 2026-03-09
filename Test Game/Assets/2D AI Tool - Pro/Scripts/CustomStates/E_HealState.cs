using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/Heal State")]
    public class E_HealState : EntityState
    {
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation HealAnimation;

        [SerializeField]
        private bool WaitForTrigger1;

        [SerializeField]
        private ParticleSystem ParticleToSpawn;

        [SerializeField]
        private GameObject ObjectToSpawn;

        [EndGroup]
        [SerializeField]
        private float healAmount = 10;

        [BeginGroup("Transitions")]
        [Header("If Animation finished")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState;

        public override void AnimationFinish()
        {
            base.AnimationFinish();
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            if (WaitForTrigger1)
            {
                Heal();
            }
        }

        public override void Enter()
        {
            base.Enter();

            NextState = CheckState(NextState);

            EntityAI.PlayAnim(HealAnimation);
            EntityAI.PlayAudio(HealAnimation.SoundAsset);

            if (!WaitForTrigger1)
            {
                Heal();
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (isAnimationFinished)
            {
                if (NextState != null && !NextState.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            EntityAI.SetVelocity(Vector2.down);
        }

        public override object GetValue(NodePort port)
        {
            NextState = GetFromPort("NextState", port, NextState);

            return port.Connection?.node;
        }


        private void Heal()
        {
            if (ParticleToSpawn)
            {
                ParticleSystem p = Instantiate(ParticleToSpawn, EntityAI.transform);

                var shape = p.shape;
                shape.scale = EntityAI.Collider.bounds.size;
            }

            if (ObjectToSpawn)
            {
                Instantiate(ObjectToSpawn, EntityAI.transform.position, Quaternion.identity);
            }

            EntityAI.AddHealing(healAmount);
        }
    }
}