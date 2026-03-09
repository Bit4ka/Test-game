using MaykerStudio;
using MaykerStudio.Types;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Follow Target NavMesh State")]
    public class E_FollowTarget_NavMesh : E_FollowTarget_NavMesh_Base
    {
        #region Transition

        [BeginGroup("Transitions")]
        [Header("If target in Min agroRange")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        private EntityState NextState1;

        [Header("If target in Max agroRadius")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        private EntityState NextState2;

        [Header("If target outSide Max agroRadius")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        [EndGroup]
        private EntityState NextState3;

        #endregion

        private bool InMinAgroRange, InMaxAgroDistance;

        public override void DoChecks()
        {
            base.DoChecks();

            if (NextState1 == null && NextState2 == null && NextState3 == null)
                return;

            if (Target && !isJumping)
            {
                InMinAgroRange = Vector2.Distance(EntityAI.transform.position, Target.position) <= minAgroDistance;

                switch (detectionType)
                {
                    case DetectionType.Circle:
                        InMaxAgroDistance = EntityAI.CheckTargetsInRadius(maxAgroDistance);
                        break;
                    case DetectionType.Ray:
                        InMaxAgroDistance = EntityAI.CheckTargetsInRange(maxAgroDistance);
                        break;
                    case DetectionType.Box:
                        InMaxAgroDistance = EntityAI.CheckBox();
                        break;
                    case DetectionType.FOV:
                        InMaxAgroDistance = EntityAI.CheckTargetsInFieldOfView(FOV, maxAgroDistance);
                        break;
                    default:
                        break;
                }
            }
            else if (!isJumping)
            {
                InMinAgroRange = false;

                InMaxAgroDistance = EntityAI.HandleAggroAndTarget(null);
            }
        }

        public override void Enter()
        {
            base.Enter();

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
            NextState3 = CheckState(NextState3);

            EntityAI.SeeThroughWalls = true;
            Target = EntityAI.GetFirstTargetTransform();
            EntityAI.SeeThroughWalls = CanSeeThroughWalls;

            if (!Target)
                Debug.LogWarning("Could not find a target to follow.");
        }

        public override void Exit()
        {
            base.Exit();

            InMinAgroRange = false;
            InMaxAgroDistance = false;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Transitions

            if (InMinAgroRange && !IsLanding)
            {
                delayTimer += Time.deltaTime;
                if (delayTimer >= delayToExitState)
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                    {
                        if (!EntityAI.Flying && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                        {
                            if (IsGrounded)
                            {
                                StateMachine.ChangeState(NextState1);
                                return;
                            }
                        }
                        else
                        {
                            StateMachine.ChangeState(NextState1);
                            return;
                        }
                    }
                }
            }

            if (InMaxAgroDistance && !IsLanding)
            {
                delayTimer += Time.deltaTime;
                if (delayTimer >= delayToExitState)
                {
                    if (NextState2 != null && !NextState2.IsInCooldown)
                    {
                        if (!EntityAI.Flying && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                        {
                            if (IsGrounded)
                            {
                                StateMachine.ChangeState(NextState2);
                                return;
                            }
                        }
                        else
                        {
                            StateMachine.ChangeState(NextState2);
                            return;
                        }
                    }
                }
            }
            else if (!IsLanding)
            {
                delayTimer += Time.deltaTime;
                if (delayTimer >= delayToExitState)
                {
                    if (NextState3 != null && !NextState3.IsInCooldown)
                    {
                        if (!EntityAI.Flying && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                        {
                            if (IsGrounded)
                            {
                                StateMachine.ChangeState(NextState3);
                                return;
                            }
                        }
                        else
                        {
                            StateMachine.ChangeState(NextState2);
                            return;
                        }
                    }
                }
            }

            #endregion
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);
            NextState3 = GetFromPort("NextState3", port, NextState3);

            return base.GetValue(port);
        }
    }
}