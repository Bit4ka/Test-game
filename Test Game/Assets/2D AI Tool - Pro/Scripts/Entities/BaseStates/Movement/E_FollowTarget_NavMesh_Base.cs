using System.Collections;
using MaykerStudio;
using MaykerStudio.Attributes;
using MaykerStudio.Types;
using UnityEngine;
using UnityEngine.AI;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("")]
    public class E_FollowTarget_NavMesh_Base : EntityState
    {
        #region Variables
        [BeginGroup("Variables")]
        [BeginGroup("Animations")]
        [SerializeField]
        protected EntityAnimation FollowAnimation;

        [SerializeField]
        protected EntityAnimation IdleAnimation;

        [HideIf("entityFlying", true)]
        [SerializeField]
        protected EntityAnimation JumpAnimation;

        [HideIf("entityFlying", true)]
        [SerializeField]
        protected EntityAnimation LedgeClimbAnimation;

        [ShowIf(nameof(UseTurnAroundAnim), true)]
        [SerializeField]
        protected EntityAnimation TurnAroundAnimation;

        [HideIf("entityFlying", true)]
        [SerializeField]
        protected EntityAnimation FallAnimation;

        [EndGroup]
        [HideIf("entityFlying", true)]
        [SerializeField]
        protected EntityAnimation LandAnimation;

        [BeginGroup("Target Detection")]
        [SerializeField]
        [NodeEnum]
        protected DetectionType detectionType = DetectionType.Circle;

        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        [SerializeField]
        [Range(5f, 270f)]
        protected float FOV = 15;

        [Min(0.0f)]
        [SerializeField]
        protected float maxAgroDistance = 3f;

        [Min(0.0f)]
        [SerializeField]
        protected float minAgroDistance = 1f;

        [SerializeField]
        [EndGroup]
        [Tooltip("If checked the entity will rotate or flip to the target, if not the entity will face the current waypoint direction.")]
        protected bool faceTarget;

        [BeginGroup("Movement")]
        [NodeEnum]
        [SerializeField]
        protected MovementType movementType = MovementType.Velocity;

        [Min(0.2f)]
        [HideIf("entityFlying", true)]
        [Tooltip("This is basically the speed of the jump, if the duration is too high the jump will be slow, and so on.")]
        [SerializeField]
        protected float JumpDuration = 0.2f;

        [Min(1.5f)]
        [HideIf("entityFlying", true)]
        [SerializeField]
        protected float MaxJumpDistanceX = 10f;

        [Min(5f)]
        [HideIf("entityFlying", true)]
        [SerializeField]
        protected float MaxJumpDistanceY = 10f;

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("Movement speed.")]
        protected float speed = 5f;

        [SerializeField]
        [Min(1f)]
        [ShowIf(nameof(movementType), MovementType.Force)]
        protected float thrust = 2f;

        [SerializeField]
        [ShowIf(nameof(movementType), MovementType.Force)]
        protected float maxVelocity = 10f;

        [Min(0.0f)]
        [SerializeField]
        protected float delayToExitState = .05f;

        [SerializeField]
        protected bool UseTurnAroundAnim;

        [SerializeField]
        [HideIf("entityFlying", true)]
        protected bool JumpLowHeightObstacles;

        [SerializeField]
        [HideIf("entityFlying", true)]
        protected bool UseLedgeClimb;

        [SerializeField]
        [HideIf("entityFlying", true)]
        [Tooltip("If checked, the state will stop everything until the OnAnimationFinishEvent is called.")]
        protected bool stopMovementOnLanding;

        [SerializeField]
        [HideIf("entityFlying", true)]
        [Tooltip("If checked and if the entity wants to jump it'll only do it when called 'AnimationTrigger1'.")]
        protected bool WaitTrigger1ForJump;

        [EndGroup]
        [SerializeField]
        [Tooltip("You can check this if you have a flying entity or a topDown 2D character.")]
        protected bool entityFlying = false;

        [BeginGroup("PathFinding")]
        [AgentID]
        [SerializeField]
        protected int AgentID;
        
        [SerializeField]
        [Min(0f)]
        [Tooltip("The min distance in Units to start slowing down the movement speed to zero. Careful with this value, because the entity can stop " +
            "before reaching a desired location like an aggro range.")]
        protected float DistanceToSlowDown = 1f;

        [SerializeField]
        [Min(0.01f)]
        [ShowIf(nameof(DistanceToSlowDown), 0f, Comparison = UnityComparisonMethod.Greater)]
        protected float SlowDownDuration = 0.1f;

        [Min(0.0f)]
        [Tooltip("If you put a lower value the path will be calculated almost in realtime with a performance cost and some unexpected behaviour")]
        [SerializeField]
        protected float pathUpdateSeconds = 0.1f;

        [EndGroup]
        [EndGroup]
        [SerializeField]
        protected float nextWaypointDistance = 2f;

        #endregion

        protected float delayTimer, jumpStartTime;

        protected bool IsGrounded, LedgeCheck, WallCheck, IsFalling, IsLanding, isJumping, canFollowPath;

        protected int CurrentWaypoint = 0;

        [HideInInspector]
        public Transform Target;

        [HideInInspector]
        public Vector3 TargetPosition;

        protected PathFollow_NavMesh pathFollow;

        protected PhysicsMaterial2D originalMat;

        protected WaitForSeconds updateTime;

        protected NavMeshPath Path;

        protected bool tryingToFindNavMesh;

        protected NavMeshQueryFilter navMeshQueryFilter;

        public override void AnimationFinish()
        {
            base.AnimationFinish();

            if (EntityAI.CurrentPlayingAnimation == LedgeClimbAnimation || EntityAI.CurrentPlayingAnimation == LandAnimation)
            {
                IsLanding = false;
                IsFalling = false;
                EntityAI.BlockAnim = false;
                EntityAI.StopKnockBack = false;

                if (pathFollow.IsWaitingLandingFinish)
                    pathFollow?.FinishLanding();

                if (pathFollow.CanClimbLedge)
                    pathFollow?.FinishClimbLedge();
            }
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            if (WaitTrigger1ForJump)
            {
                pathFollow.StartJump(ref isJumping, ref jumpStartTime);
                EntityAI.StopKnockBack = true;
            }
        }

        public override void AnimationTrigger2()
        {
            base.AnimationTrigger2();
        }

        public override void DoChecks()
        {
            IsGrounded = EntityAI.CheckGround();
            LedgeCheck = EntityAI.CheckLedge();
            WallCheck = EntityAI.CheckWall();

            pathFollow?.UpdateCheckers(IsGrounded, LedgeCheck, WallCheck);
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.Flying = entityFlying;

            navMeshQueryFilter = new NavMeshQueryFilter() {agentTypeID = AgentID,  areaMask = NavMesh.AllAreas};

            if (pathFollow == null)
                pathFollow = new PathFollow_NavMesh(EntityAI, movementType, nextWaypointDistance,
                    JumpDuration, DistanceToSlowDown, SlowDownDuration, thrust, MaxJumpDistanceX, MaxJumpDistanceY,
                    WaitTrigger1ForJump, stopMovementOnLanding, UseLedgeClimb, JumpLowHeightObstacles, UseTurnAroundAnim);

            if (updateTime == null)
                updateTime = new WaitForSeconds(pathUpdateSeconds);

            pathFollow.UpdateCheckers(IsGrounded, LedgeCheck, WallCheck);

            if (Path == null)
                Path = new NavMeshPath();

            System.Array.Clear(Path.corners, 0, Path.corners.Length);

            delayTimer = 0;

            if (entityFlying)
                EntityAI.Rb.gravityScale = 0f;

            EntityAI.StartCoroutine(UpdatePath());

            originalMat = EntityAI.Rb.sharedMaterial;

            pathFollow.OnJumpFinish += OnJumpFinish;
            pathFollow.OnTurnAroundStart += OnTurnAroundStart;

            canFollowPath = true;
        }

        public override void Exit()
        {
            base.Exit();

            pathFollow.OnJumpFinish -= OnJumpFinish;
            pathFollow.OnTurnAroundStart -= OnTurnAroundStart;

            pathFollow.Reset();

            EntityAI.Rb.sharedMaterial = originalMat;

            isJumping = false;
            IsFalling = false;
            EntityAI.BlockAnim = false;

            if (entityFlying)
            {
                EntityAI.Flying = false;
                EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Animations handling

            if (pathFollow.IsTurning)
            {
                if (EntityAI.CurrentPlayingAnimation != TurnAroundAnimation)
                    EntityAI.PlayAnim(TurnAroundAnimation);

                if (!IsGrounded)
                {
                    isAnimationFinished = false;
                    pathFollow.IsTurning = false;
                    EntityAI.Flip();

                    EntityAI.PlayAnim(FallAnimation);
                    EntityAI.PlayAudio(FallAnimation.SoundAsset);
                }

                if (isAnimationFinished || EntityAI.Anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
                {
                    EntityAI.Flip();
                    pathFollow.IsTurning = false;
                    isAnimationFinished = false;
                    EntityAI.Rb.linearVelocity = Vector2.right * EntityAI.FacingDirection;
                }
                else
                    return;
            }

            if (!entityFlying && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                if (IsGrounded)
                {
                    //TODO: Check if the animations are playing correct in all scenarios
                    if (((WaitTrigger1ForJump && pathFollow.CanJump) || (!WaitTrigger1ForJump && isJumping)) && !isJumping)
                    {
                        EntityAI.PlayAnim(JumpAnimation);
                        EntityAI.PlayAudio(JumpAnimation.SoundAsset);

                        if (WaitTrigger1ForJump)
                        {
                            EntityAI.StopKnockBack = true;
                        }
                    }
                    else if (stopMovementOnLanding &&
                        (isJumping && IsFalling && Time.time >= jumpStartTime + 0.05f || IsFalling && !isJumping || !IsFalling && pathFollow.IsWaitingLandingFinish)
                        && !pathFollow.CanClimbLedge)
                    {
                        if (!IsLanding && !pathFollow.IsAdjustingPosition)
                        {
                            EntityAI.PlayAnim(LandAnimation);
                            EntityAI.PlayAudio(LandAnimation.SoundAsset);

                            EntityAI.StopKnockBack = true;
                            IsFalling = false;
                            IsLanding = true;
                            EntityAI.BlockAnim = true;
                            EntityAI.SetVelocityX(0f);

                            if (!isJumping)
                                pathFollow.IsWaitingLandingFinish = true;
                        }
                        else if(EntityAI.Anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
                        {
                            AnimationFinish();
                        }
                    }
                    else if (!isJumping && EntityAI.Rb.linearVelocity.magnitude >= 0.1f)
                    {
                        EntityAI.PlayAnim(FollowAnimation);
                        EntityAI.PlayAudio(FollowAnimation.SoundAsset);
                    }
                    else if (pathFollow.CanClimbLedge && UseLedgeClimb)
                    {
                        EntityAI.PlayAnim(LedgeClimbAnimation);
                        EntityAI.PlayAudio(LedgeClimbAnimation.SoundAsset);
                    }
                    else if (EntityAI.Rb.linearVelocity.magnitude < 0.1f || EntityAI.CurrentPlayingAnimation != LandAnimation
                        && EntityAI.Anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
                    {
                        EntityAI.PlayAnim(IdleAnimation);
                        EntityAI.PlayAudio(IdleAnimation.SoundAsset);
                    }
                }
                else
                {
                    if ((EntityAI.Rb.linearVelocity.y > 5f || pathFollow.IsWaitingLandingFinish) && !EntityAI.IsOnSlope)
                        IsFalling = true;

                    if (EntityAI.Rb.linearVelocity.y < -1f && !EntityAI.IsOnSlope)
                    {
                        EntityAI.PlayAnim(FallAnimation);
                        EntityAI.PlayAudio(FallAnimation.SoundAsset);

                        IsFalling = true;
                    }

                    if (pathFollow.CanClimbLedge && UseLedgeClimb)
                    {
                        EntityAI.PlayAnim(LedgeClimbAnimation);
                        EntityAI.PlayAudio(LedgeClimbAnimation.SoundAsset);
                    }
                }

            }
            else if (EntityAI.Rb.linearVelocity.magnitude > 0)
            {
                EntityAI.PlayAnim(FollowAnimation);
                EntityAI.PlayAudio(FollowAnimation.SoundAsset);
            }
            else
            {
                EntityAI.PlayAnim(IdleAnimation);
                EntityAI.PlayAudio(IdleAnimation.SoundAsset);
            }

            #endregion
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (Path.corners.Length >= 2 || EntityAI.Rb.linearVelocity.magnitude < 0.1f)
                EntityAI.Rb.sharedMaterial = originalMat;
            else
                EntityAI.Rb.sharedMaterial = null;

            if (Path != null && (Target != null || TargetPosition != Vector3.zero) && canFollowPath)
            {
                pathFollow.Move(true, faceTarget, Path.corners, TargetPosition != Vector3.zero ? TargetPosition : Target.position, 
                    speed, maxVelocity, ref CurrentWaypoint, ref isJumping, ref jumpStartTime);
            }
        }

        protected virtual IEnumerator UpdatePath()
        {
            yield return 0;

            while (StateMachine.CurrentState == this)
            {
                if (!tryingToFindNavMesh && Target)
                    NavMesh.CalculatePath(EntityAI.transform.position, Target.position, navMeshQueryFilter, Path);

                CurrentWaypoint = 1;

                if (Path.status == NavMeshPathStatus.PathInvalid && !tryingToFindNavMesh)
                {
                    NavMeshTriangulation t = NavMesh.CalculateTriangulation();

                    if (t.vertices.Length == 0)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("Error calculating NavMesh triangulation. Make sure the there's a baked NavMesh on scene");
#endif
                        break;
                    }

                    if (NavMesh.FindClosestEdge(t.vertices[Mathf.RoundToInt(t.vertices.Length / 2)], out NavMeshHit hit, NavMesh.AllAreas))
                    {
                        Debug.DrawLine(EntityAI.transform.position, hit.position, Color.cyan, 2f);

                        NavMesh.CalculatePath(hit.position, Target ? Target.position : TargetPosition, NavMesh.AllAreas, Path);
                        tryingToFindNavMesh = true;

#if UNITY_EDITOR
                        Debug.LogWarning(EntityAI.name + " is outside the navMesh, or the target position is outside the NavMesh. \nTrying to find the closest position to NavMesh.");
#endif
                    }
                }

                if (NavMesh.SamplePosition(EntityAI.transform.position, out _, 1.0f, NavMesh.AllAreas))
                {
                    tryingToFindNavMesh = false;
                }

                yield return new WaitForSeconds(pathUpdateSeconds);
            }
        }

        private void OnJumpFinish()
        {
            NavMesh.CalculatePath(EntityAI.transform.position, Target ? Target.position : TargetPosition, NavMesh.AllAreas, Path);

            if(EntityAI.CurrentPlayingAnimation == LedgeClimbAnimation)
            {
                EntityAI.PlayAnim(FollowAnimation);
            }
        }

        private void OnTurnAroundStart()
        {
            isAnimationFinished = false;
            EntityAI.PlayAnim(TurnAroundAnimation);
        }

#if UNITY_EDITOR
        public override void DrawGizmosDebug()
        {
            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, maxAgroDistance);

            ExtDebug.DrawEllipse(EntityAI.targetCheck.position, minAgroDistance, minAgroDistance, 32, Color.cyan);

            ExtDebug.DrawPathFinding(Path.corners);
        }
#endif

    }
}