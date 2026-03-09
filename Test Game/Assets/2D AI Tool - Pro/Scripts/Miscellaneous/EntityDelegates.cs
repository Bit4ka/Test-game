using MaykerStudio;
using UnityEngine;

namespace AI2DTool
{
    /// <summary>
    /// MonoBehaviour component that holds delegates actions that others classes can subscribe to those events.
    /// </summary>
    public class EntityDelegates : MonoBehaviour
    {
        public delegate void DamageReceiveAction(Entity entity, DamageDetails details);
        public delegate void StunnedAction(Entity entity);
        public delegate void RecoveredStunnenAction(Entity entity);
        public delegate void DeadAction(Entity entity, int score);
        public delegate void DropAttackEndAction(Entity entity);
        public delegate void TargetDetectedAction(Entity entity);
        public delegate void TargetNotDetectedAction(Entity entity);
        public delegate void DamageSendAction(Entity entity, GameObject target, DamageDetails details);
        public delegate void DashAttackStartAction(Entity entity);
        public delegate void DashAttackEndAction(Entity entity);
        public delegate void AttacksStartAction(Entity entity);
        public delegate void AttacksEndAction(Entity entity);

        /// <summary>
        /// This delegate is only invoked by <see cref="Entity"/> class. Please don't invoke outside of it.
        /// </summary>
        public DamageReceiveAction OnDamageReceive;

        /// <summary>
        /// This delegate is only invoked by <see cref="Entity"/> class. Please don't invoke outside of it.
        /// </summary>
        public StunnedAction OnEntityStunned;

        /// <summary>
        /// This delegate is only invoked by <see cref="Entity"/> class. Please don't invoke outside of it.
        /// </summary>
        public RecoveredStunnenAction OnEntityRecoveredStunned;

        /// <summary>
        /// This delegate is only invoked by <see cref="E_DeadNormal"/> class. Please don't invoke outside of it.
        /// </summary>
        public DeadAction OnEntityDead;

        /// <summary>
        /// This delegate is only invoked by <see cref="E_DropAttack"/> state class. Please don't invoke outside of it.
        /// </summary>
        public DropAttackEndAction OnDropAttackEnd;

        /// <summary>
        /// This delegate is only invoked by <see cref="Entity"/> class. Please don't invoke outside of it.
        /// </summary>
        public TargetDetectedAction OnTargetDetected;

        /// <summary>
        /// This delegate is only invoked by <see cref="Entity"/> class. Please don't invoke outside of it.
        /// </summary>
        public TargetDetectedAction OnTargetNotDetected;

        /// <summary>
        /// This delegate is only invoked by <see cref="Entity"/> class. Please don't invoke outside of it.
        /// </summary>
        public DamageSendAction OnDamageSend;

        /// <summary>
        /// This delegate is only invoked by <see cref="E_DashAttack"/> class. Please don't invoke outside of it.
        /// </summary>
        public DashAttackStartAction OnDashAttackStart;

        /// <summary>
        /// This delegate is only invoked by <see cref="E_DashAttack"/> class. Please don't invoke outside of it.
        /// </summary>
        public DashAttackEndAction OnDashAttackEnd;

        /// <summary>
        /// This delegate is only invoked by <see cref="E_Attacks"/> class. Please don't invoke outside of it.
        /// </summary>
        public AttacksStartAction OnAttacksStart;

        /// <summary>
        /// This delegate is only invoked by <see cref="E_Attacks"/> class. Please don't invoke outside of it.
        /// </summary>
        public AttacksEndAction OnAttacksEnd;
    }
}