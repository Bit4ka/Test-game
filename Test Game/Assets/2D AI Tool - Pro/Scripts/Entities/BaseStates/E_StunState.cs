using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Others/Stunned State")]
    public class E_StunState : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation StunnedAnimation;

        [SerializeField]
        private float StunnedDuration = 2f;

        [EndGroup]
        [SerializeField]
        [Tooltip("If checked, a damage will end this state.")]
        private bool WakeOnDamage;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [SerializeField]
        [Header("If duration is over or damaged")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        [EndGroup]
        private EntityState NextState1;

        #endregion

        public override void DoChecks()
        {
            base.DoChecks();
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.IsDamaged = false;

            NextState1 = CheckState(NextState1);
            

            EntityAI.EntityDelegates.OnEntityStunned?.Invoke(EntityAI);

            EntityAI.SetVelocity(Vector2.zero);
        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.IsStunned = false;
            EntityAI.EntityDelegates.OnEntityRecoveredStunned?.Invoke(EntityAI);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            EntityAI.PlayAnim(StunnedAnimation);
            EntityAI.PlayAudio(StunnedAnimation.SoundAsset);

            if (Time.time >= startTime + StunnedDuration || EntityAI.IsDamaged && WakeOnDamage)
            {
                if (NextState1 != null)
                    StateMachine.ChangeState(NextState1);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            

            return base.GetValue(port);
        }

        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();
        }
    }
}