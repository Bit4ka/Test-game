using MaykerStudio.Types;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Check Targets detected")]
    public class E_CheckTargetsDetected : EntityState
    {
        [BeginGroup("Variables")]
        [SerializeField]
        private DetectionType detectionType = DetectionType.Circle;

        [SerializeField]
        [Min(0f)]
        [HideIf(nameof(detectionType), DetectionType.Box)]
        private float MaxAgroDistance = 7f;

        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        [SerializeField]
        [Range(5f, 270f)]
        [EndGroup]
        private float FOV = 15f;

        [BeginGroup("Transitions")]
        [Header("If target detected")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [Header("If target NOT detected")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [EndGroup]
        private EntityState NextState2;

        private bool targetDetected;

        public override void DoChecks()
        {
            base.DoChecks();

            MaykerStudio.ExtDebug.DrawEllipse(EntityAI.targetCheck.position, MaxAgroDistance, MaxAgroDistance, 32, Color.magenta, 0.5f);

            switch (detectionType)
            {
                case DetectionType.Circle:
                    targetDetected = EntityAI.CheckTargetsInRadius(MaxAgroDistance);
                    break;
                case DetectionType.Ray:
                    targetDetected = EntityAI.CheckTargetsInRange(MaxAgroDistance);
                    break;
                case DetectionType.FOV:
                    targetDetected = EntityAI.CheckTargetsInFieldOfView(FOV, MaxAgroDistance);
                    break;
                case DetectionType.Box:
                    targetDetected = EntityAI.CheckBox();
                    break;
            }
        }

        public override void Enter()
        {
            base.Enter();

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            if (targetDetected)
            {
                if (NextState1 != null && !NextState1.IsInCooldown)
                    StateMachine.ChangeState(NextState1);
            }
            else
            {
                if (NextState2 != null && !NextState2.IsInCooldown)
                    StateMachine.ChangeState(NextState2);
            }

        }

        public override void Exit()
        {
            base.Exit();

            targetDetected = false;
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return port.Connection?.node;
        }
    }
}