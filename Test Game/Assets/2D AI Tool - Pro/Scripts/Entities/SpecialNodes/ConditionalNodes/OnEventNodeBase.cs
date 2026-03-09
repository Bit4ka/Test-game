using UnityEngine;
using UnityEngine.Serialization;
using XNode;

namespace AI2DTool
{
    /// <summary>
    /// Base class to event-based nodes, it contains basic methods and functions, just need to inherit from this class, and also, add the subscription to the event <see cref="EntityAI.CheckOnEventNodes"/>
    /// </summary>
    [CreateNodeMenu("")]
    [NodeWidth(210)]
    public class OnEventNodeBase : Node
    {
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [BeginGroup("Base Options")]
        [Disable]
        public EntityState NextState;

        [Tooltip("Check this if you want to ignore the cooldown of the NextState")]
        [SerializeField]
        private bool IgnoreCooldown;

        [Tooltip("Check this option if you want the transition to happen only once.")]
        [SerializeField]
        [EndGroup]
        private bool CallOnlyOnce = true;

        [HideInInspector]
        public EntityAI entityAI;

        [HideInInspector]
        public bool HasTransitionOnce;

        private EntityState CachedState;

        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            base.OnCreateConnection(from, to);

            GetValue(from);
        }

        public override void OnRemoveConnection(NodePort port)
        {
            base.OnRemoveConnection(port);

            GetValue(port);
        }

        public override object GetValue(NodePort port)
        {
            CachedState = null;

            NextState = port.Connection?.node as EntityState;

            return NextState;
        }

        public virtual void TryTransitionToNextState(params object[] args) { }

        public virtual void TryTransitionToNextState(Entity entity, DamageDetails details) { }

        public void StartTransition()
        {
            if (NextState != null)
            {
                if (CachedState == null)
                    CachedState = entityAI.GetState(NextState);

                if (IgnoreCooldown)
                {
                    Transition();
                }
                else if (!CachedState.IsInCooldown)
                {
                    Transition();
                }
#if UNITY_EDITOR
                else if (!HasTransitionOnce)
                {
                    Debug.LogWarning(entityAI.name + " -> " + name + ": "+CachedState.name+" is in cooldown.");
                }
#endif
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning(entityAI.name + " -> " + name + ": NextState is null");
            }
#endif
        }

        private void Transition()
        {
            if (CallOnlyOnce && !HasTransitionOnce)
            {
                entityAI.StateMachine.ChangeState(CachedState);
                HasTransitionOnce = true;
            }
            else if (!CallOnlyOnce)
                entityAI.StateMachine.ChangeState(CachedState);
        }
    }
}