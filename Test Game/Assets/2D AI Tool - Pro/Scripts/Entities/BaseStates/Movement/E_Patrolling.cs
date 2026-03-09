using MaykerStudio;
using MaykerStudio.Types;
using System.Collections;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Patrolling State")]
    public class E_Patrolling : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation PatrolingAnimation;

        [SerializeField]
        private EntityAnimation IdleAnimation;

        [ShowIf(nameof(UseTurnAroundAnim), true)]
        [SerializeField]
        private EntityAnimation TurnAroundAnimation;

        [NodeEnum]
        [SerializeField]
        private DetectionType detectionType = DetectionType.Circle;

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("Put values between 1 and 0. ONLY WORKS FOR FLYING ENEMIES AND TOPDOWN_2D game mode.")]
        private Vector2 moveDirection;

        [Min(0.0f)]
        [SerializeField]
        private float moveSpeed = 5f;

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("When the entity faces a wall or step in a ledge it will stop and flips the direction with this delay. ONLY WORKS IN PLATFORMER_2D.")]
        private float delayBetweenFlips = 1f;

        [Min(0.0f)]
        public float targetAgroDistance = 10f;

        [Min(0.0f)]
        [SerializeField]
        private float delayToExitState = 1f;

        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        [Range(5f, 270f)]
        [SerializeField]
        private float FOV = 15f;

        [SerializeField]
        private bool UseTurnAroundAnim;

        [SerializeField]
        [Tooltip("If checked, the state will flip the entity to a random direction when the state enter. ONLY WORK FOR PLATFORMER 2D.")]
        private bool PickRandomDirection;

        [SerializeField]
        [Tooltip("If checked, the current state duration will be reset to zero when the entity finds a ledge or wall.")]
        private bool ResetStateDurationOnIdle;

        [SerializeField]
        [EndGroup]
        private bool entityFlying;

        #endregion

        #region Transition

        [BeginGroup("Transitions")]
        [Header("If target in max agro range")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState1;

        [Header("If NextState1 in cooldown")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        [EndGroup]
        private EntityState NextState2;

        #endregion

        private bool inMaxAgroRange, ledgeCheck, wallCheck, canMove, IsTurning;
        private float delayFlipTimer, delayExitTimer;
        private Vector2 _MoveDirection;

        public override void AnimationFinish()
        {
            base.AnimationFinish();

            if (IsTurning)
            {
                canMove = true;
                IsTurning = false;
                EntityAI.PlayAnim(PatrolingAnimation);

                EntityAI.StartCoroutine(FlipCO());
            }
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

            ledgeCheck = EntityAI.CheckLedge();

            wallCheck = EntityAI.CheckWall();

            if (NextState1 == null)
                return;

            switch (detectionType)
            {
                case DetectionType.Circle:
                    inMaxAgroRange = EntityAI.CheckTargetsInRadius(targetAgroDistance);
                    break;
                case DetectionType.Ray:
                    inMaxAgroRange = EntityAI.CheckTargetsInRange(targetAgroDistance);
                    break;
                case DetectionType.FOV:
                    inMaxAgroRange = EntityAI.CheckTargetsInFieldOfView(FOV, targetAgroDistance);
                    break;
                case DetectionType.Box:
                    inMaxAgroRange = EntityAI.CheckBox();
                    break;
                default:
                    break;
            }
        }

        public override void Enter()
        {
            base.Enter();

            delayFlipTimer = 0f;
            _MoveDirection = moveDirection.normalized;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            if (entityFlying)
            {
                EntityAI.Flying = true;
                EntityAI.Rb.gravityScale = 0f;
            }

            if (PickRandomDirection)
            {
                switch(Random.Range(0, 2))
                {
                    case 0:
                        EntityAI.Flip();
                    break;
                }
            }

            canMove = true;
        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.Flying = false;
            EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Animations handling

            if (!IsTurning)
            {
                if (!canMove)
                {
                    EntityAI.PlayAnim(IdleAnimation);
                    EntityAI.PlayAudio(IdleAnimation.SoundAsset);
                }
                else
                {
                    EntityAI.PlayAnim(PatrolingAnimation);
                    EntityAI.PlayAudio(PatrolingAnimation.SoundAsset);
                }
            }
            else if(EntityAI.CurrentPlayingAnimation != TurnAroundAnimation)
            {
                EntityAI.PlayAnim(TurnAroundAnimation);
            }

            #endregion

            #region Transition

            if (inMaxAgroRange && !IsTurning)
            {
                delayExitTimer += Time.deltaTime;

                if(delayExitTimer >= delayToExitState)
                {
                    if (NextState1 != null && !NextState1.IsInCooldown)
                        StateMachine.ChangeState(NextState1);
                    else if (NextState2 != null && !NextState2.IsInCooldown)
                        StateMachine.ChangeState(NextState2);
                }
            }
            else
            {
                delayExitTimer = 0f;
            }

            #endregion

            if (wallCheck || (!ledgeCheck && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying))
            {
                if (ResetStateDurationOnIdle)
                    currentStateDuration = 0;

                canMove = false;
                delayFlipTimer += Time.deltaTime;
                if (delayFlipTimer >= delayBetweenFlips)
                {
                    delayFlipTimer = 0f;
                    wallCheck = false;
                    ledgeCheck = true;

                    if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                    {
                        if (UseTurnAroundAnim)
                        {
                            if (!IsTurning)
                            {
                                EntityAI.PlayAnim(TurnAroundAnimation);
                                IsTurning = true;
                                canMove = false;
                            }
                        }
                        else
                        {
                            EntityAI.Flip();
                            canMove = true;
                        }
                        
                    }
                    else
                    {
                        _MoveDirection = -_MoveDirection;
                        EntityAI.FlipToTarget(_MoveDirection, false);
                    }
                }
            }
            else
            {
                canMove = true;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
            {
                if (canMove)
                    EntityAI.SetVelocityX(EntityAI.FacingDirection * moveSpeed);
                else
                    EntityAI.SetVelocityX(0);
            }
            else
            {
                if (canMove)
                    EntityAI.SetVelocity(_MoveDirection * moveSpeed);
                else
                    EntityAI.SetVelocity(Vector2.zero);
            }

        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return base.GetValue(port);
        }

        private IEnumerator FlipCO()
        {
            yield return new WaitForEndOfFrame();

            EntityAI.Flip();
        }


#if UNITY_EDITOR

        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();

            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, targetAgroDistance);
        }

#endif
    }
}