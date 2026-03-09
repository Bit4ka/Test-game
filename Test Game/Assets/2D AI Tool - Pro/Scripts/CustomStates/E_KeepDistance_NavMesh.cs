using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Keep Distance NavMesh")]
    public class E_KeepDistance_NavMesh : E_FollowTarget_NavMesh_Base
    {
        [BeginGroup("Keep Distance settings")]
        [SerializeField]
        protected LayerMask whatIsGround;

        [SerializeField]
        protected float ForwardMovementDistance = 4f;

        [SerializeField]
        protected float backwardMovementDistance = 3f;

        [SerializeField]
        protected float DistanceToChase = 10f;

        [SerializeField]
        protected EntityAnimation ForwardAnimation;

        [EndGroup]
        [SerializeField]
        protected EntityAnimation BackwardAnimation;

        [BeginGroup("Transitions")]
        [Disable]
        [SerializeField]
        [EndGroup]
        [Header("If in Distance to chase")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        protected EntityState NextState1;

        protected bool backwards, forwards;

        protected bool locked;

        protected bool realocate, waitForRealocationFinish;

        protected float tempSpeedMultiplier = 1f;

        public override void Enter()
        {
            base.Enter();

            locked = false;
            realocate = false;
            waitForRealocationFinish = false;

            NextState1 = CheckState(NextState1);

            EntityAI.SeeThroughWalls = true;
            Target = EntityAI.GetFirstTargetTransform();
            EntityAI.SeeThroughWalls = CanSeeThroughWalls;
        }

        public override void LogicUpdate()
        {
            if (stateDuration > 0)
            {
                currentStateDuration += Time.deltaTime;

                if (currentStateDuration >= stateDuration)
                {
                    if (TimeIsOver != null && !TimeIsOver.IsInCooldown)
                    {
                        StateMachine.ChangeState(TimeIsOver);
                        return;
                    }
                }
            }

            #region Animation Handling

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

                if (isAnimationFinished)
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
                    if (((WaitTrigger1ForJump && pathFollow.CanJump) || (!WaitTrigger1ForJump && isJumping)) && !isJumping)
                    {
                        EntityAI.PlayAnim(JumpAnimation);
                        EntityAI.PlayAudio(JumpAnimation.SoundAsset);

                        if (WaitTrigger1ForJump)
                        {
                            EntityAI.StopKnockBack = true;
                        }
                    }
                    else if ((isJumping && IsFalling && Time.time >= jumpStartTime + 0.05f || IsFalling && !isJumping || !IsFalling && pathFollow.IsWaitingLandingFinish) && !pathFollow.CanClimbLedge)
                    {
                        EntityAI.PlayAnim(LandAnimation);
                        EntityAI.PlayAudio(LandAnimation.SoundAsset);

                        if (stopMovementOnLanding && !IsLanding && !pathFollow.IsAdjustingPosition)
                        {
                            EntityAI.StopKnockBack = true;
                            IsLanding = true;
                            EntityAI.BlockAnim = true;
                            EntityAI.SetVelocityX(0f);

                            if (!isJumping)
                                pathFollow.IsWaitingLandingFinish = true;
                        }
                    }
                    else if (!isJumping && EntityAI.Rb.linearVelocity.magnitude >= 0.1f)
                    {
                        if(forwards)
                            EntityAI.PlayAnim(ForwardAnimation);
                        else if(backwards)
                            EntityAI.PlayAnim(BackwardAnimation);
                    }
                    else if (pathFollow.CanClimbLedge && UseLedgeClimb)
                    {
                        EntityAI.PlayAnim(LedgeClimbAnimation);
                        EntityAI.PlayAudio(LedgeClimbAnimation.SoundAsset);
                    }
                    else if (EntityAI.Rb.linearVelocity.magnitude < 0.1f)
                    {
                        EntityAI.PlayAnim(IdleAnimation);
                        EntityAI.PlayAudio(IdleAnimation.SoundAsset);
                    }
                }
                else
                {
                    if (EntityAI.Rb.linearVelocity.y > 5f || pathFollow.IsWaitingLandingFinish)
                        IsFalling = true;

                    if (EntityAI.Rb.linearVelocity.y < -1f)
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

            backwards = Vector2.Distance(Target.position, EntityAI.Collider.bounds.center) <= backwardMovementDistance;

            forwards = Vector2.Distance(Target.position, EntityAI.Collider.bounds.center) >= ForwardMovementDistance;

            if (Vector2.Distance(Target.position, EntityAI.Collider.bounds.center) >= DistanceToChase)
            {
                if(NextState1 != null && !NextState1.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState1);
                    return;
                }
            }

            if (!realocate)
            {
                if (forwards)
                {
                    Vector3 dir = (Target.position - EntityAI.Collider.bounds.center).normalized;
                    dir.y = 0f;

                    TargetPosition = EntityAI.Collider.bounds.center + dir * 1.5f;
                }
                else if (backwards)
                {
                    Vector3 dir = (Target.position - EntityAI.Collider.bounds.center).normalized;
                    dir.y = 0f;

                    TargetPosition = EntityAI.Collider.bounds.center - dir * 1.5f;
                }
                else
                {
                    TargetPosition = Vector3.zero;
                }
            }
        }

        public override void PhysicsUpdate()
        {
            DoChecks();

            if (EntityAI.Rb.linearVelocity.magnitude > 1f)
                EntityAI.Rb.sharedMaterial = originalMat;
            else
                EntityAI.Rb.sharedMaterial = null;

            if (Path != null && Target != null && TargetPosition != Vector3.zero && !locked)
            {
                pathFollow.Move(true, faceTarget, Path.corners, Target.position,
                    speed * tempSpeedMultiplier, maxVelocity, ref CurrentWaypoint, ref isJumping, ref jumpStartTime);

#if UNITY_EDITOR
                Debug.DrawRay(TargetPosition, EntityAI.Collider.bounds.size.y * Vector2.down, Color.green);

                MaykerStudio.ExtDebug.DrawEllipse(TargetPosition, 0.05f, 0.05f, 32, Color.green);
#endif

                if (!realocate && (Physics2D.OverlapPoint(TargetPosition, whatIsGround) 
                    || !Physics2D.Raycast(TargetPosition, Vector2.down, EntityAI.Collider.bounds.size.y, whatIsGround)))
                {
                    Vector3 dir = (Target.position - EntityAI.Collider.bounds.center).normalized;
                    dir.y = 0.1f;
                    realocate = true;
                    TargetPosition = Target.position + dir * 2f;
                    tempSpeedMultiplier = 1.5f;
                }
                else if (realocate)
                {
                    if(Vector2.Distance(TargetPosition, EntityAI.transform.position) < 0.5f)
                    {
                        waitForRealocationFinish = false;
                        realocate = false;
                        tempSpeedMultiplier = 1f;
                    }

                    if(Physics2D.OverlapPoint(TargetPosition, whatIsGround)
                    || !Physics2D.Raycast(TargetPosition, Vector2.down, EntityAI.Collider.bounds.size.y, whatIsGround))
                    {
                        TargetPosition = EntityAI.transform.position;
                        locked = true;
                    }
                }
            }
            else
            {
                EntityAI.SetVelocityX(0f);
            }
        }

        protected override IEnumerator UpdatePath()
        {
            while (StateMachine.CurrentState == this)
            {
                if ((IsGrounded || EntityAI.Flying))
                {
                    if (TargetPosition != Vector3.zero)
                    {
                        if (realocate && !waitForRealocationFinish || !realocate)
                        {
                            if(realocate)
                                waitForRealocationFinish = true;

                            NavMesh.CalculatePath(EntityAI.transform.position, TargetPosition, NavMesh.AllAreas, Path);
                        }
                    }
                }

                yield return updateTime;
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);

            return base.GetValue(port);
        }
    }
}