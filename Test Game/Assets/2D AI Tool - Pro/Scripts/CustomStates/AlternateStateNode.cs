using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Cooldown Alternate Node")]
    [NodeTint("#364f78")]
    public class AlternateStateNode : EntityState
    {
        [Header("If NextState1 Not in Cooldown")]
        [BeginGroup]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState1;

        [EndGroup]
        [Header("If NextState1 in Cooldown")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState2;

        public override void Enter()
        {
            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            if (NextState1 != null && !NextState1.IsInCooldown)
                StateMachine.ChangeState(NextState1);

            else if (NextState2 != null)
                StateMachine.ChangeState(NextState2);

            else
                Debug.LogError("No alternative found on " + name);
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return port.Connection?.node;
        }
    }
}