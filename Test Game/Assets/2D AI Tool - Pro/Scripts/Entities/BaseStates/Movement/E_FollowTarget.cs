using MaykerStudio;
using MaykerStudio.Types;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Follow Target State")]
    public class E_FollowTarget : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        protected EntityAnimation FollowAnimation;

        [SerializeField]
        protected EntityAnimation IdleAnimation;

        [SerializeField]
        [NodeEnum]
        protected DetectionType detectionType = DetectionType.Circle;

        [SerializeField]
        [Tooltip("Movement speed.")]
        protected float speed = 10f;

        [Min(0.0f)]
        [SerializeField]
        protected float delayToExitState = .5f;

        [Min(0.0f)]
        [SerializeField]
        protected float minAgroDistance = 1f;

        [Min(0.0f)]
        [SerializeField]
        protected float maxAgroDistance = 3f;

        [SerializeField]
        protected bool stopIfNoLedge = true;

        [SerializeField]
        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        [Range(5f, 270f)]
        [EndGroup]
        protected float FOV = 15;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If target in Min agroRange")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        protected EntityState NextState1;

        [Header("If target outside agroRadius")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        protected EntityState NextState2;

        [Header("If NextState1 in coolDown && target minAgroDist")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        protected EntityState NextState3;

        [EndGroup]
        [Header("If NextState2 in coolDown && target out max agroRange")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        protected EntityState NextState4;

        #endregion

        protected float delayTimer;

        protected bool InMinAgroRange, InMaxAgroRadius, LedgeCheck, IsGrounded;

        protected Transform target;

        public bool IsTypeFov()
        {
            return detectionType == DetectionType.FOV;
        }

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
            InMinAgroRange = EntityAI.CheckTargetsInRange(minAgroDistance);

            switch (detectionType)
            {
                case DetectionType.Circle:
                    InMaxAgroRadius = EntityAI.CheckTargetsInRadius(maxAgroDistance);
                    break;
                case DetectionType.Ray:
                    InMaxAgroRadius = EntityAI.CheckTargetsInRange(maxAgroDistance);
                    break;
                case DetectionType.Box:
                    InMaxAgroRadius = EntityAI.CheckBox();
                    break;
                case DetectionType.FOV:
                    InMaxAgroRadius = EntityAI.CheckTargetsInFieldOfView(FOV, maxAgroDistance);
                    break;
                default:
                    break;
            }

            IsGrounded = EntityAI.CheckGround();
            LedgeCheck = EntityAI.CheckLedge();
        }

        public override void Enter()
        {
            base.Enter();

            delayTimer = 0;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
            NextState3 = CheckState(NextState3);
            NextState4 = CheckState(NextState4);

            target = EntityAI.GetFirstTargetTransform(maxAgroDistance);
        }

        public override void Exit()
        {
            base.Exit();

            InMinAgroRange = false;
            InMaxAgroRadius = false;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Transition1

            if (!InMaxAgroRadius)
            {
                delayTimer += Time.deltaTime;
                if (delayTimer >= delayToExitState)
                {
                    if (NextState2 != null && !NextState2.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState2);
                    }

                    if (NextState4 != null && !NextState4.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState4);
                    }
                }
            }
            else
                delayTimer = 0f;

            #endregion

            if (IsGrounded)
            {
                if (LedgeCheck && stopIfNoLedge || !stopIfNoLedge)
                {
                    EntityAI.PlayAnim(FollowAnimation);
                    EntityAI.PlayAudio(FollowAnimation.SoundAsset);
                }
                else
                {
                    EntityAI.PlayAnim(IdleAnimation);
                    EntityAI.PlayAudio(IdleAnimation.SoundAsset);
                }

                #region Transition2

                if (InMinAgroRange)
                {
                    delayTimer += Time.deltaTime;
                    if (delayTimer >= delayToExitState)
                    {
                        if (NextState1 != null && !NextState1.IsInCooldown)
                        {
                            StateMachine.ChangeState(NextState1);
                        }

                        else if (NextState3 != null && !NextState3.IsInCooldown)
                        {
                            StateMachine.ChangeState(NextState3);
                        }
                    }
                }

                #endregion
            }

            EntityAI.FlipToTarget();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (Vector2.Distance(EntityAI.transform.position, target.position) > 1)
            {
                if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                {
                    if (stopIfNoLedge && !LedgeCheck)
                    {
                        EntityAI.SetVelocityX(0f);
                    }
                    else
                        EntityAI.SetVelocityX(speed * EntityAI.FacingDirection);
                }
                else
                {
                    EntityAI.SetVelocity((target.position - EntityAI.transform.position).normalized * speed);
                }
            }
            else
            {
                if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                    EntityAI.SetVelocityX(Mathf.Lerp(speed, 0, 5 * Time.deltaTime));
                else
                {
                    EntityAI.SetVelocity(Vector2.Lerp(EntityAI.Rb.linearVelocity, Vector2.zero, speed * Time.deltaTime));
                }
            }

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

            Debug.DrawRay(EntityAI.transform.position, Vector2.right * minAgroDistance * EntityAI.FacingDirection, Color.cyan);
        }
#endif

    }
}