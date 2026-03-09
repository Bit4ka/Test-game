using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Check Health Amount")]
    public class E_CheckHealthAmount : EntityState
    {
        [BeginGroup("TypeCheck")]
        [SerializeField]
        private OnEntityHealthNode.HealthType HealthTypeCheck;

        [ShowIf(nameof(HealthTypeCheck), OnEntityHealthNode.HealthType.Value)]
        [FormerlySerializedAs("Health Value")]
        [Min(0)]
        public int healthValue;

        [ShowIf(nameof(HealthTypeCheck), OnEntityHealthNode.HealthType.Percentage)]
        [FormerlySerializedAs("Health Percentage")]
        [Min(0)]
        [EndGroup]
        public int healthPercentage;

        [BeginGroup("Transitions")]
        [Header("If true")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState1;

        [EndGroup]
        [Header("If false")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState2;

        public override void Enter()
        {
            base.Enter();

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            if (HealthTypeCheck == OnEntityHealthNode.HealthType.Percentage)
            {
                if (Mathf.RoundToInt((EntityAI.CurrentHealth / EntityAI.MaxHealth) * 100f) <= healthPercentage)
                {
                    StateMachine.ChangeState(NextState1);
                }
                else
                {
                    StateMachine.ChangeState(NextState2);
                }
            }
            else
            {
                if (EntityAI.CurrentHealth <= healthValue)
                {
                    StateMachine.ChangeState(NextState1);
                }
                else
                {
                    StateMachine.ChangeState(NextState2);
                }
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return port.Connection?.node;
        }
    }
}