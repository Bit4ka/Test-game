using MaykerStudio;
using MaykerStudio.Types;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Follow Partner NavMesh State")]
    public class E_FollowPartner_NavMesh : E_FollowTarget_NavMesh_Base
    {
        #region Variables

        [BeginGroup]
        [EndGroup]
        [SerializeField]
        [Tooltip("The name of the target that the state will follow.")]
        private string PartnerName;

        #endregion

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

        [Header("If Partner outSide Max agroRadius")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        private EntityState NextState3;

        [Header("If Partner in Max agroRadius")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        [EndGroup]
        private EntityState NextState4;

        #endregion

        private float delayPartnerTimer, JumpStartTime;

        private bool TargetInMinAgroRange, TargetInMaxAgroDistance;

        public override void AnimationFinish()
        {
            base.AnimationFinish();
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();
        }

        public override void AnimationTrigger2()
        {
            base.AnimationTrigger2();
        }

        public override void DoChecks()
        {
            base.DoChecks();

            switch (detectionType)
            {
                case DetectionType.Circle:
                    TargetInMinAgroRange = EntityAI.CheckTargetsInRadius(minAgroDistance);
                    TargetInMaxAgroDistance = EntityAI.CheckTargetsInRadius(maxAgroDistance);
                    break;
                case DetectionType.Ray:
                    TargetInMinAgroRange = EntityAI.CheckTargetsInRange(minAgroDistance);
                    TargetInMaxAgroDistance = EntityAI.CheckTargetsInRange(maxAgroDistance);
                    break;
                case DetectionType.Box:
                    TargetInMinAgroRange = EntityAI.CheckBox();
                    TargetInMaxAgroDistance = EntityAI.CheckBox();
                    break;
                case DetectionType.FOV:
                    TargetInMinAgroRange = EntityAI.CheckTargetsInFieldOfView(FOV, minAgroDistance);
                    TargetInMaxAgroDistance = EntityAI.CheckTargetsInFieldOfView(FOV, maxAgroDistance);
                    break;
                default:
                    break;
            }
        }

        public override void Enter()
        {
            base.Enter();

            delayTimer = 0;
            delayPartnerTimer = 0;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
            NextState3 = CheckState(NextState3);
            NextState4 = CheckState(NextState4);

            if(!Target)
                Target = GameObject.Find(PartnerName).transform;

            if (!Target)
                Debug.Log(EntityAI.gameObject.name + " could not find " + PartnerName);

            EntityAI.Flying = entityFlying;

            if (entityFlying)
                EntityAI.Rb.gravityScale = 0f;

            originalMat = EntityAI.Rb.sharedMaterial;
        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.Rb.sharedMaterial = originalMat;

            TargetInMinAgroRange = false;
            TargetInMaxAgroDistance = false;
            isJumping = false;

            if (entityFlying)
            {
                EntityAI.Flying = false;
                EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Transitions
            //If TARGET is inside the min agroDistance
            if (TargetInMinAgroRange)
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
            //If TARGET is inside the max agroDistance
            if (TargetInMaxAgroDistance)
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
            //If partner is outside the max agroDistance
            else if (Vector2.Distance(EntityAI.transform.position, Target.position) > maxAgroDistance)
            {
                delayTimer += Time.deltaTime;
                if (delayTimer >= delayToExitState)
                {
                    if (NextState3 != null && !NextState3.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState3);
                        return;
                    }
                }
            }
            //If partner is inside the max agroDistance
            if (Vector2.Distance(EntityAI.transform.position, Target.position) <= maxAgroDistance)
            {
                delayPartnerTimer += Time.deltaTime;
                if (delayPartnerTimer >= delayToExitState)
                {
                    if (NextState4 != null && !NextState4.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState4);
                        return;
                    }
                }
            }
            else
            {
                delayPartnerTimer = 0f;
            }
            #endregion
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);
            NextState3 = GetFromPort("NextState3", port, NextState3);
            NextState4 = GetFromPort("NextState4", port, NextState4);

            return base.GetValue(port);
        }

#if UNITY_EDITOR

        public override void DrawGizmosDebug()
        {
            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, maxAgroDistance);

            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, minAgroDistance, Color.magenta);

            ExtDebug.DrawPathFinding(Path.corners);
        }
#endif

    }
}