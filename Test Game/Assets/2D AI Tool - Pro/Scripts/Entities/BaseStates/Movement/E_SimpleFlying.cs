using MaykerStudio;
using MaykerStudio.Types;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Simple Flying State")]
    public class E_SimpleFlying : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation IdleAnimation;

        [SerializeField]
        private EntityAnimation FlyingAnimation;

        [NodeEnum]
        [SerializeField]
        private DetectionType detectionType;

        [SerializeField]
        [Tooltip("Choose values between -1 and 1. Keep in mind that for Platformer2D the wall detection will be on the facing direction of the entity.")]
        private Vector2 MoveDirection = Vector2.right;

        [Min(0.0f)]
        [SerializeField]
        private float MoveSpeed = 5f;

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("The delay when the entity faces a all and goes to flip the direction.")]
        private float delayBetweenFlips = 1f;

        [Min(0.0f)]
        [SerializeField]
        private float targetAgroDistance = 5f;

        [EndGroup]
        [Range(5f, 270f)]
        [SerializeField]
        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        private float FOV = 15f;

        [BeginGroup("Sine Wave")]
        [SerializeField]
        private float frequency = 5f;

        [EndGroup]
        [SerializeField]
        private float magnitude = 5f;

        #endregion

        #region Transition

        [BeginGroup("Transitions")]
        [Header("If target in agro range")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [Header("If NextState1 in cooldown")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [EndGroup]
        private EntityState NextState2;

        #endregion

        private float delayTimer;

        private bool wallCheck, InAgroRange, canMove;

        private Vector2 _MoveDirection;
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

            wallCheck = EntityAI.CheckWall();

            switch (detectionType)
            {
                case DetectionType.Circle:
                    InAgroRange = EntityAI.CheckTargetsInRadius(targetAgroDistance);
                    break;
                case DetectionType.Ray:
                    InAgroRange = EntityAI.CheckTargetsInRange(targetAgroDistance);
                    break;
                case DetectionType.Box:
                    InAgroRange = EntityAI.CheckBox();
                    break;
                case DetectionType.FOV:
                    InAgroRange = EntityAI.CheckTargetsInFieldOfView(FOV, targetAgroDistance);
                    break;
                default:
                    break;
            }
        }

        public override void Enter()
        {
            base.Enter();

            _MoveDirection = MoveDirection;

            EntityAI.IgnoreFallClamp = true;
            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            EntityAI.Rb.gravityScale = 0f;

            canMove = true;
        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.IgnoreFallClamp = false;
            EntityAI.SetVelocityX(0f);

            EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Transition

            if (InAgroRange)
            {
                if (NextState1 != null && !NextState1.IsInCooldown)
                    StateMachine.ChangeState(NextState1);
                else if (NextState2 != null && !NextState2.IsInCooldown)
                    StateMachine.ChangeState(NextState2);
            }

            #endregion

            if (wallCheck)
            {
                EntityAI.PlayAnim(IdleAnimation);
                EntityAI.PlayAudio(IdleAnimation.SoundAsset);

                delayTimer += Time.deltaTime;

                canMove = false;

                if (delayTimer >= delayBetweenFlips)
                {
                    if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                        EntityAI.Flip();
                    else
                        InvertDirection();

                    delayTimer = 0f;


                }
            }
            else
            {
                canMove = true;

                EntityAI.PlayAnim(FlyingAnimation);
                EntityAI.PlayAudio(FlyingAnimation.SoundAsset);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (canMove)
            {
                if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                {
                    EntityAI.SetVelocityX(_MoveDirection.normalized.x * MoveSpeed * EntityAI.FacingDirection);
                    EntityAI.SetVelocityY((_MoveDirection.normalized.y * MoveSpeed) + MoveSpeed * Mathf.Sin(Time.time * frequency) * magnitude);
                }
                else
                {
                    Vector2 sineWaveWithAngle = GetSineWithAngle();

                    EntityAI.SetVelocity(_MoveDirection.normalized * MoveSpeed);
                    EntityAI.Rb.linearVelocity = new Vector2(EntityAI.Rb.linearVelocity.x + sineWaveWithAngle.x,
                        EntityAI.Rb.linearVelocity.y + sineWaveWithAngle.y);


                    EntityAI.FlipToTarget(EntityAI.Rb.linearVelocity.normalized, false);
                }
            }
            else
            {
                EntityAI.SetVelocity(Vector2.zero);
            }
        }

        public Vector2 GetSineWithAngle()
        {
            return (Quaternion.Euler(0, 0, -90) * _MoveDirection) * Mathf.Sin(Time.time * frequency) * magnitude;
        }

        void InvertDirection()
        {
            _MoveDirection *= -1;

            canMove = true;
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return base.GetValue(port);
        }


#if UNITY_EDITOR

        public override void DrawGizmosDebug()
        {
            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, targetAgroDistance);
        }
#endif
    }
}