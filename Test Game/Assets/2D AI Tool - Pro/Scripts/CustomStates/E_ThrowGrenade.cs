using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/Throw Grenade")]
    public class E_ThrowGrenade : EntityState
    {
        [BeginGroup]
        [SerializeField]
        private EntityAnimation ThrowAnimation;

        [SerializeField]
        [PrefabObjectOnly]
        private Grenade grenadePrefab;

        [SerializeField]
        private float throwForce = 10;

        [SerializeField]
        private float yOffset = 0f;

        [EndGroup]
        [SerializeField]
        private float maxDuration = 3f;

        [BeginGroup("Transition")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Header("If Animation finish")]
        private EntityState NextState;

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            EntityAI.SeeThroughWalls = true;
            Vector2 targetDir = (EntityAI.GetFirstTargetTransform().position - EntityAI.Collider.bounds.center).normalized;
            EntityAI.SeeThroughWalls = CanSeeThroughWalls;

            targetDir.y += yOffset / 10;

            EntityAI.FlipToTarget(targetDir, false);

            Grenade g = Instantiate(grenadePrefab, EntityAI.attackCheck.position, grenadePrefab.transform.rotation);
            g.Throw(targetDir, throwForce, maxDuration);
        }

        public override void Enter()
        {
            base.Enter();

            NextState = CheckState(NextState);

            EntityAI.PlayAnim(ThrowAnimation);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (isAnimationFinished)
            {
                if (NextState != null && !NextState.IsInCooldown)
                    StateMachine.ChangeState(NextState);
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState = GetFromPort("NextState", port, NextState);

            return base.GetValue(port);
        }
    }
}