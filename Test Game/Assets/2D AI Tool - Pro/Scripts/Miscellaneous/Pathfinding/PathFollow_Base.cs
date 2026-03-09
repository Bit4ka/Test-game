using MaykerStudio.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;

namespace AI2DTool
{
    public class PathFollow_Base
    {
        public Action OnJumpFinish;

        public Action OnTurnAroundStart;

        public Action<Entity> OnDestinationArrived;

        public bool CanJump, CanClimbLedge, IsWaitingLandingFinish, FinishLedgeClimb, IsAdjustingPosition, IsTurning;

        public bool StopJumping, IsGrounded, WallCheck, LedgeCheck, IsLedgeJump, CanMove, JumpFailed;

        protected float JumpHeight, JumpForceX, JumpForceY, gravity, Speed, lerpSpeed;

        protected Coroutine StopMovementCO;

        #region ReadOnly Vars

        protected readonly float OriginalMass, nextWaypointDistance, JumpDuration, DistanceToSlowDown, SlowDownDuration, Thrust, MaxJumpDistanceX, MaxJumpDistanceY;

        protected readonly bool WaitTrigger1ForJump, StopMovementOnlanding, UseLedgeClimb, UseTurnAroundAnim, JumpLowHeightObstacles, NoTilemap;

        public readonly EntityAI EntityAI;

        protected readonly MovementType movementType;

        protected readonly NormalJumpModule normalJumpModule;

        protected readonly LedgeJumpModule ledgeJumpModule;

        protected readonly Tilemap tileMap;

        #endregion

        #region Collider Positions

        /// <summary>
        /// In Local Space
        /// </summary>
        public readonly Vector2 CenterRight;

        /// <summary>
        /// In Local Space
        /// </summary>
        public readonly Vector2 CenterLeft;

        /// <summary>
        /// In Local Space
        /// </summary>
        public readonly Vector2 TopCenter;

        /// <summary>
        /// In Local Space
        /// </summary>
        public readonly Vector2 BottomCenter;

        /// <summary>
        /// In World Space
        /// </summary>
        public Vector2 Center => EntityAI.Collider.bounds.center;

        /// <summary>
        /// In World Space
        /// </summary>
        public Vector2 TopRight => EntityAI.Collider.bounds.max;

        /// <summary>
        /// In World Space
        /// </summary>
        public Vector2 BottomLeft => EntityAI.Collider.bounds.min;

        /// <summary>
        /// In World Space
        /// </summary>
        public Vector2 BottomRight => EntityAI.Collider.bounds.max - new Vector3(0f, EntityAI.Collider.bounds.size.y);

        /// <summary>
        /// In World Space
        /// </summary>
        public Vector2 TopLeft => EntityAI.Collider.bounds.min + new Vector3(0f, EntityAI.Collider.bounds.size.y);


        #endregion

        #region AuxVariables

        protected Vector2 _oldVelocity, _velocity;
        protected Vector2 ForceVelocity = Vector2.zero;
        protected Vector2 LastTargetPositionSinceFlipping;

        public Vector2 finalJumpDestination, cornerDestination1, LedgePos, feetPosition = Vector2.zero;
        protected Vector2 JumpDirection, CornerDirection = Vector2.zero;

        public ContactFilter2D WallContact = new ContactFilter2D();
        protected ContactFilter2D groundContactFilter = new ContactFilter2D();

        private float adjustingStartTime;
        private float lastTimeFliped;

        private ContactPoint2D[] contactPoint2Ds = new ContactPoint2D[10];

        #endregion

        public PathFollow_Base(EntityAI entity, MovementType movementType,
            float nextWaypointDistance, float JumpDuration,
            float DistanceToSlowDown, float SlowDownDuration, float Thrust, float MaxJumpDistanceX, float MaxJumpDistanceY,
            bool WaitTrigger1ForJump, bool StopMovementOnlanding, bool UseLedgeClimb, bool JumpLowHeightObstacles, bool UseTurnAroundAnim)
        {
            EntityAI = entity;
            OriginalMass = entity.Rb.mass;
            this.movementType = movementType;
            this.nextWaypointDistance = nextWaypointDistance;
            this.JumpDuration = JumpDuration;
            this.StopMovementOnlanding = StopMovementOnlanding;
            this.WaitTrigger1ForJump = WaitTrigger1ForJump;
            this.DistanceToSlowDown = DistanceToSlowDown;
            this.SlowDownDuration = SlowDownDuration;
            this.Thrust = Thrust;
            this.UseLedgeClimb = UseLedgeClimb;
            this.JumpLowHeightObstacles = JumpLowHeightObstacles;
            this.MaxJumpDistanceX = MaxJumpDistanceX;
            this.MaxJumpDistanceY = MaxJumpDistanceY;
            this.UseTurnAroundAnim = UseTurnAroundAnim;

            WallContact.SetLayerMask(EntityAI.entityData.whatIsObstacles);

            groundContactFilter.useNormalAngle = true;
            groundContactFilter.layerMask = EntityAI.entityData.whatIsObstacles;
            groundContactFilter.minNormalAngle = EntityAI.entityData.groundMinNormalAngle;
            groundContactFilter.maxNormalAngle = EntityAI.entityData.groundMaxNormalAngle;

            BottomCenter = new Vector2 { x = 0, y = -EntityAI.Collider.bounds.size.y * 0.5f };
            TopCenter = new Vector2 { x = 0, y = EntityAI.Collider.bounds.size.y * 0.5f };
            CenterLeft = new Vector2 { x = -EntityAI.Collider.bounds.size.x * 0.55f, y = 0 };
            CenterRight = new Vector2 { x = EntityAI.Collider.bounds.size.x * 0.55f, y = 0 };

            normalJumpModule = new NormalJumpModule(this);
            ledgeJumpModule = new LedgeJumpModule(this);

            CanMove = true;

            if (!EntityAI.Flying)
            {
                RaycastHit2D ray = Physics2D.Raycast(EntityAI.Rb.position, Vector2.down, 100f, EntityAI.entityData.whatIsObstacles);

                if (!ray.collider || !ray.collider.TryGetComponent(out tileMap))
                {
					NoTilemap = true;
					
                    GameObject TempTileMap = GameObject.Find("TempTileMap");

                    if (TempTileMap == null)
                    {
                        Grid grid = new GameObject("TempGrid").AddComponent<Grid>();
                        grid.cellSize = Vector2.one;

                        tileMap = new GameObject("TempTileMap").AddComponent<Tilemap>();
                        tileMap.transform.parent = grid.transform;
                    }
                    else
                    {
                        tileMap = TempTileMap.GetComponent<Tilemap>();
                    }

                    Debug.LogWarning("No tilemap found bellow the entity. Using non-tiled code for jump algorithm.");
                }
            }

        }

        public void UpdateCheckers(bool IsGrounded, bool LedgeCheck, bool WallCheck)
        {
            this.IsGrounded = IsGrounded;
            this.LedgeCheck = LedgeCheck;
            this.WallCheck = WallCheck;

            if (EntityAI.FacingDirection == 1)
                WallContact.SetNormalAngle(175f, 181f);
            else
                WallContact.SetNormalAngle(-1f, 15f);

            EntityAI.Rb.GetContacts(contactPoint2Ds);

            /*
            if (EntityAI.enableDebug)
            {
                MaykerStudio.ExtDebug.DrawEllipse(Center + TopCenter, .1f, .1f, 16, Color.green);

                MaykerStudio.ExtDebug.DrawEllipse(Center + BottomCenter, .1f, .1f, 16, Color.yellow);

                MaykerStudio.ExtDebug.DrawEllipse(Center + CenterRight, .1f, .1f, 16, Color.red);

                MaykerStudio.ExtDebug.DrawEllipse(Center + CenterLeft, .1f, .1f, 16, Color.black);
            }
            */
        }

        public virtual void Move(bool IsAstar, bool faceTarget, Vector3[] Path, Vector2 targetPosition, float speed, float maxVelocity, ref int CurrentWaypoint, ref bool IsJumping, ref float JumpStartTime)
        {
            Vector2 waypointDirection;

            Speed = speed;

            if (!EntityAI.Flying && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                if (CurrentWaypoint < Path.Length && finalJumpDestination == Vector2.zero && MaxJumpDistanceX > 0 && MaxJumpDistanceY > 0)
                {
                    Vector2 directionJ;
                    Vector2 jumpNodePosition;

                    directionJ = ((Vector2)Path[CurrentWaypoint] - EntityAI.Rb.position).normalized;
                    jumpNodePosition = Path[CurrentWaypoint];

                    if (!IsWaitingLandingFinish && IsGrounded)
                    {
                        directionJ.x = (float)Math.Round(directionJ.x, 1);
                        if (directionJ.y >= 0.65f || directionJ.x != 0f && directionJ.y >= -0.5f && !LedgeCheck && EntityAI.IsFacingPosition(Path[CurrentWaypoint])
                        || JumpLowHeightObstacles && EntityAI.Rb.IsTouching(WallContact) 
                        && (EntityAI.FacingDirection == 1 && !TopRightCheck() || EntityAI.FacingDirection == -1 && !TopLeftCheck()) )
                        {
                            CalculateJumpPosition(Path);
                        }
                    }
                }

                JumpHandler(speed, ref IsJumping, ref JumpStartTime, ref CurrentWaypoint);

                if (!IsJumping && !IsWaitingLandingFinish && !IsAdjustingPosition && (WaitTrigger1ForJump && !CanJump || !WaitTrigger1ForJump))
                {
                    MoveToNextWaypoint(faceTarget, CurrentWaypoint, Path, speed, maxVelocity, targetPosition);
                }
            }
            else
            {
                if (CurrentWaypoint < Path.Length)
                {
                    waypointDirection = ((Vector2)Path[CurrentWaypoint] - EntityAI.Rb.position).normalized;

                    //Flying entities or 2D top-down
                    if (movementType == MovementType.Velocity)
                    {
                        if (DistanceToSlowDown == 0 || Vector2.Distance(EntityAI.Rb.position, targetPosition) > DistanceToSlowDown)
                        {
                            lerpSpeed = 0f;
                            EntityAI.SetVelocity(waypointDirection * speed);
                        }
                        else
                        {
                            lerpSpeed += Time.deltaTime;
                            EntityAI.SetVelocity(Vector2.Lerp(waypointDirection * speed, Vector2.zero, lerpSpeed / SlowDownDuration));
                        }
                    }
                    else if(!EntityAI.IsKnockback)
                    {
                        if (DistanceToSlowDown == 0 || Vector2.Distance(EntityAI.Rb.position, targetPosition) > DistanceToSlowDown)
                        {
                            lerpSpeed = 0f;
                            ForceVelocity.Set(waypointDirection.x * speed, waypointDirection.y * speed);
                            EntityAI.Rb.AddForce(ForceVelocity * Thrust * Time.fixedDeltaTime, ForceMode2D.Impulse);
                        }
                        else
                        {
                            lerpSpeed += Time.deltaTime;
                            EntityAI.SetVelocity(Vector2.Lerp(Thrust * Time.fixedDeltaTime * ForceVelocity, Vector2.zero, lerpSpeed / SlowDownDuration));
                        }

                        //Using force can make then too fast, so this will clamp the velocity so you don't need to mess with the rigid-body drag values
                        EntityAI.Rb.linearVelocity = Vector2.ClampMagnitude(EntityAI.Rb.linearVelocity, maxVelocity);
                    }

                    if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                    {
                        if (faceTarget)
                            CheckIfShouldFlip((targetPosition - EntityAI.Rb.position).normalized, targetPosition);
                        else
                            CheckIfShouldFlip(waypointDirection, targetPosition);
                    }
                    else
                    {
                        if (EntityAI.entityData.rotateTheTransform)
                        {
                            if (faceTarget)
                                EntityAI.LookAt2D((targetPosition - EntityAI.Rb.position).normalized, true);
                            else
                                EntityAI.LookAt2D(waypointDirection, true);
                        }
                        else
                        {
                            if (faceTarget)
                                EntityAI.FlipToTarget(targetPosition, true);
                            else
                                EntityAI.FlipToTarget(waypointDirection, false);
                        }
                    }
                }
                else if (Vector2.Distance(EntityAI.Rb.position, targetPosition) <= DistanceToSlowDown)
                {
                    EntityAI.SetVelocity(Vector2.zero);
                }
            }

            if (CurrentWaypoint < Path.Length)
            {
#if UNITY_EDITOR
                Debug.DrawLine(EntityAI.Rb.position, Path[CurrentWaypoint], Color.white, 0);
#endif
                Vector2 tweakedPos = EntityAI.Rb.position;
                tweakedPos.y = Path[CurrentWaypoint].y < TopRight.y && Path[CurrentWaypoint].y > BottomLeft.y ? Path[CurrentWaypoint].y : Center.y;

                float distance = Vector2.Distance(tweakedPos, Path[CurrentWaypoint]);

                if (distance < nextWaypointDistance)
                {
                    CurrentWaypoint++;
                }
            }
        }

        public void Reset()
        {
            finalJumpDestination = Vector2.zero;
            cornerDestination1 = Vector2.zero;

            _velocity = _oldVelocity = JumpDirection = Vector2.zero;

            IsLedgeJump = false;
            CanJump = false;
            FinishLedgeClimb = false;
            CanClimbLedge = false;
            IsAdjustingPosition = false;
            EntityAI.BlockDamagedAnim = false;

            EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
            EntityAI.Rb.mass = OriginalMass;
            JumpHeight = 0f;
            JumpForceX = JumpForceY = 0f;
            gravity = 0f;
        }

        public void StartJump(ref bool IsJumping, ref float JumpStartTime)
        {
            if (finalJumpDestination != Vector2.zero)
            {
                EntityAI.Rb.gravityScale = 1f;

                if (UseLedgeClimb && !IsLedgeJump)
                {
                    JumpHeight = feetPosition.y - EntityAI.Rb.position.y;
                }
                else if (IsLedgeJump)
                {
                    JumpHeight = (finalJumpDestination.y + 0.5f - EntityAI.Rb.position.y);
                }
                else
                {
                    JumpHeight = (finalJumpDestination.y + EntityAI.Rb.gravityScale) - EntityAI.Rb.position.y;
                }

                gravity = -2 * JumpHeight / (JumpDuration * JumpDuration);

                JumpForceY = 2 * JumpHeight / JumpDuration;

                JumpForceX = ((!IsLedgeJump ? 2f : 1f)
                    * Mathf.Abs(finalJumpDestination.x - EntityAI.Rb.position.x) / JumpDuration) * EntityAI.FacingDirection;

                _velocity.y = JumpForceY;
                _velocity.x = JumpForceX;
                _velocity.x = Mathf.Abs(finalJumpDestination.x - EntityAI.Rb.position.x) <= 2f && !IsLedgeJump ? 0 : _velocity.x;

                IsJumping = true;
                JumpStartTime = Time.time;

                EntityAI.SetVelocityX(0f);
            }
        }

        public void FinishLanding() => IsWaitingLandingFinish = false;

        public void FinishClimbLedge()
        {
            if(CanClimbLedge)
                FinishLedgeClimb = true;
        }

        protected virtual void MoveToNextWaypoint(bool faceTarget, int currentWaypoint, Vector3[] Path, float speed, float maxVelocity, Vector2 targetPosition)
        {
            Vector2 direction = Vector2.zero;

            if (currentWaypoint < Path.Length && CanMove)
                direction = ((Vector2)Path[currentWaypoint] - EntityAI.Rb.position).normalized;
            else if (lerpSpeed == 0f)
                return;

            if (Mathf.Sign(direction.x) == EntityAI.FacingDirection && EntityAI.Rb.IsTouching(WallContact) || IsTurning)
            {
                EntityAI.SetVelocityX(0f);

                return;
            }

            if (Vector2.Distance(LastTargetPositionSinceFlipping, targetPosition) <= 1 && Time.time <= lastTimeFliped + 1f)
            {
                if (CheckIfShouldFlip((targetPosition - EntityAI.Rb.position).normalized, targetPosition))
                {
                    EntityAI.SetVelocityX(0f);
                    return;
                }
            }

            Vector2 tweakedPos = EntityAI.Rb.position;

            if (Vector2.Distance(targetPosition, tweakedPos) <= DistanceToSlowDown || lerpSpeed > 0)
            {
                lerpSpeed += Time.deltaTime;

                if (movementType == MovementType.Velocity)
                {
                    float lerpSpeed = Mathf.Lerp(speed, 0f, this.lerpSpeed / SlowDownDuration);

                    if (direction.x > 0.2f && direction.y < 1f)
                        EntityAI.SetVelocityX(lerpSpeed > 0f ? lerpSpeed : 0f);

                    else if (direction.x < -0.2f && direction.y < 1f)
                        EntityAI.SetVelocityX(-lerpSpeed < 0f ? -lerpSpeed : 0f);

                    else
                        EntityAI.SetVelocityX(lerpSpeed != 0f ? lerpSpeed * EntityAI.FacingDirection : 0f);
                }
                else
                {
                    ForceVelocity = Vector2.Lerp(EntityAI.Rb.linearVelocity, Vector2.zero, lerpSpeed / SlowDownDuration);

                    EntityAI.Rb.AddForce(ForceVelocity, ForceMode2D.Impulse);

                    EntityAI.Rb.linearVelocity = Vector2.ClampMagnitude(EntityAI.Rb.linearVelocity, maxVelocity);
                }

                if (lerpSpeed / SlowDownDuration >= 1f)
                {
                    OnDestinationArrived?.Invoke(EntityAI);

                    lerpSpeed = 0f;
                }
            }
            else
            {
                lerpSpeed = 0f;

                if (movementType == MovementType.Velocity)
                {
                    if (direction.x >= 0.2f && direction.y < 0.95f)
                        EntityAI.SetVelocityX(speed / (IsGrounded ? 1f : 10f));

                    else if (direction.x <= -0.2f && direction.y < 0.95f)
                        EntityAI.SetVelocityX(-speed / (IsGrounded ? 1f : 10f));
                    else
                        EntityAI.SetVelocityX(EntityAI.FacingDirection * speed / (IsGrounded ? 2f : 10f));
                }
                else
                {
                    if (direction.x > 0)
                        ForceVelocity.Set(speed, EntityAI.Rb.linearVelocity.y);

                    else if (direction.x < 0)
                        ForceVelocity.Set(-speed, EntityAI.Rb.linearVelocity.y);

                    else
                        ForceVelocity.Set(speed * EntityAI.FacingDirection, EntityAI.Rb.linearVelocity.y);

                    EntityAI.Rb.AddForce(ForceVelocity * Thrust * Time.fixedDeltaTime, ForceMode2D.Impulse);

                    EntityAI.Rb.linearVelocity = Vector2.ClampMagnitude(EntityAI.Rb.linearVelocity, maxVelocity);
                }
            }

            if(currentWaypoint < Path.Length && Vector2.Distance(Path[currentWaypoint], Center) > nextWaypointDistance)
            {
                if (faceTarget)
                    CheckIfShouldFlip((targetPosition - EntityAI.Rb.position).normalized, (Vector2)Path[currentWaypoint]);
                else
                    CheckIfShouldFlip(direction, (Vector2)Path[currentWaypoint]);
            }
        }

        public bool CheckIfShouldFlip(Vector2 direction, Vector3 targetPosition)
        {
            // Evita virar constantemente se a direção no eixo Y for muito alta
            if (direction.y > 0.7f && Mathf.Abs(direction.x) < 0.2f)
                return false;

            bool flipCondiction =
                (direction.x >= 0.1f && EntityAI.FacingDirection == -1) ||
                (direction.x < -0.1f && EntityAI.FacingDirection == 1);

            if (flipCondiction && CheckEntityFlyingOrGrounded() && !IsTurning && finalJumpDestination == Vector2.zero && !IsWaitingLandingFinish)
            {
                if (UseTurnAroundAnim)
                {
                    OnTurnAroundStart?.Invoke();
                    IsTurning = true;
                }
                else
                {
                    EntityAI.FlipToTarget(direction, false);
                }

                LastTargetPositionSinceFlipping = targetPosition;
                lastTimeFliped = Time.time;
            }

            return flipCondiction;
        }


        private bool CheckEntityFlyingOrGrounded()
        {
            return EntityAI.Flying || !EntityAI.Flying && IsGrounded;
        }

        #region Jump related

        protected void JumpHandler(float speed, ref bool IsJumping, ref float JumpStartTime, ref int CurrentWaypoint)
        {
            #region Before Jumping

            if (finalJumpDestination != Vector2.zero)
            {
                JumpDirection = (finalJumpDestination - EntityAI.Rb.position).normalized;
                CornerDirection = (cornerDestination1 - EntityAI.Rb.position).normalized;

                if (!IsJumping && Mathf.Abs(JumpDirection.x) > 0.1f)
                    EntityAI.FlipToTarget(finalJumpDestination, true);

                if (EntityAI.enableDebug)
                {
                    Debug.DrawLine(EntityAI.Rb.position, cornerDestination1, Color.blue);
                    Debug.DrawRay(EntityAI.Rb.position, Vector3.up * MaxJumpDistanceY, Color.white);
                    Debug.DrawRay(EntityAI.Rb.position, EntityAI.transform.right * MaxJumpDistanceX, Color.white);
                }

                float dot = Vector2.Dot(EntityAI.transform.right, CornerDirection);

                if ((dot < 0.75f || !LedgeCheck || EntityAI.Rb.IsTouching(WallContact)) && !IsJumping)
                {
                    if (!LedgeCheck || dot < 0.05 && EntityAI.CheckWall())
                    {
                        CheckIfCanJump(ref IsJumping, ref JumpStartTime);
                    }
                    else if (!LedgeCheck || dot > -0.1f)
                    {
                        CheckIfCanJump(ref IsJumping, ref JumpStartTime);
                    }
                    else if (!IsAdjustingPosition && !CanJump)
                    {
                        AdjustPositionBeforeJump(Speed / 1.5f);
                    }
                    else if (!CanJump && !IsJumping)
                    {
                        if (cornerDestination1.x > EntityAI.Rb.position.x)
                        {
                            if (!CanNotTouchHeadCollider())
                                EntityAI.SetVelocityX(Speed / 1.5f);
                        }
                        else if (cornerDestination1.x < EntityAI.Rb.position.x)
                        {
                            if (!CanNotTouchHeadCollider())
                                EntityAI.SetVelocityX(-Speed / 1.5f);
                        }
                    }
                }

                if (IsAdjustingPosition && EntityAI.Rb.linearVelocity.magnitude < 1.5f && !CanJump && !IsJumping)
                {
                    if (JumpDirection.x > 0)
                    {
                        if (TopLeftCheck())
                            EntityAI.SetVelocityX(speed);
                        else
                            EntityAI.SetVelocityX(-speed);
                    }
                    else
                    {
                        if (TopRightCheck())
                            EntityAI.SetVelocityX(-speed);
                        else
                            EntityAI.SetVelocityX(speed);
                    }
                }
                else if (IsAdjustingPosition && !LedgeCheck)
                {
                    JumpFailed = true;
                    IsAdjustingPosition = false;
                    Reset();
                }

                #region Jumping

                if (IsJumping)
                {
                    adjustingStartTime = Time.time;

                    if ((finalJumpDestination - EntityAI.Rb.position).sqrMagnitude > 1f && (UseLedgeClimb && !CanClimbLedge || !UseLedgeClimb))
                    {
                        StopJumping = false;

                        Debug.DrawLine(EntityAI.Rb.position, feetPosition, Color.red);

                        if (!IsGrounded)
                        {
                            if (_velocity.x == 0)
                            {
                                if (Mathf.Abs(JumpDirection.x) >= 0.5f || EntityAI.Rb.position.y >= LedgePos.y)
                                {
                                    if(!UseLedgeClimb)
                                        EntityAI.SetVelocityX(JumpDirection.x * speed * 2f);
                                    else
                                        EntityAI.SetVelocityX(JumpDirection.x * speed * 2f * -gravity * Time.fixedDeltaTime);
                                }
                                else
                                    EntityAI.SetVelocityX(0f);
                            }
                            else
                            {
                                EntityAI.SetVelocityX(0f);
                            }

                            if (JumpDirection.y <= -0.9f)
                                _velocity.y = -speed;
                        }
                        else
                        {
                            EntityAI.FlipToTarget(JumpDirection, false);
                        }

                        _oldVelocity = _velocity;
                        _velocity.y += gravity * Time.fixedDeltaTime;

                        if (!IsLedgeJump)
                        {
                            if (_velocity.x != 0f && !Physics2D.Linecast(EntityAI.Rb.position, finalJumpDestination,
                            EntityAI.entityData.whatIsObstacles))
                            {
                                _velocity.x += JumpForceX > 0 ? gravity * Time.fixedDeltaTime : gravity * -Time.fixedDeltaTime;

                                if (JumpForceX < 0)
                                    _velocity.x = _velocity.x >= 0 ? 0f : _velocity.x;
                                else
                                    _velocity.x = _velocity.x <= 0 ? 0f : _velocity.x;

                                EntityAI.SetVelocity((_oldVelocity + _velocity) * 0.5f);
                            }
                            else
                            {
                                EntityAI.SetVelocityY((_oldVelocity.y + _velocity.y) * 0.5f);
                            }
                        }
                        else
                        {
                            EntityAI.SetVelocity(JumpDirection * (Mathf.Abs(JumpForceX) + TopCenter.y * 4));
                        }

                        #region LedgeClimb Logic

                        if (UseLedgeClimb && !CanClimbLedge && !IsLedgeJump)
                        {
                            if (EntityAI.Rb.IsTouching(WallContact) && Time.time >= JumpStartTime + 0.05f &&
                                feetPosition.y >= EntityAI.Rb.position.y && (LedgePos - EntityAI.Rb.position).sqrMagnitude < 2f)
                            {
                                CanClimbLedge = true;
                                StopJumping = true;
                                _velocity.y = 0;
                                EntityAI.Rb.gravityScale = 0;

                                CurrentWaypoint++;

                                EntityAI.SetVelocity(Vector2.zero);

                                LedgePos.x = EntityAI.Rb.position.x;

                                EntityAI.transform.position = LedgePos;
                            }
                        }

                        #endregion
                    }
                    else if (!StopJumping)
                    {
                        StopJumping = true;
                        _velocity.y = 0;
                        EntityAI.SetVelocity(Vector2.zero);

                        if (!UseLedgeClimb || IsLedgeJump)
                        {
                            EntityAI.Rb.position = finalJumpDestination;
                            EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
                        }
                    }

                    if (IsGrounded && Time.time >= JumpStartTime + JumpDuration / 2 && !IsWaitingLandingFinish && !CanClimbLedge || FinishLedgeClimb)
                    {
                        IsJumping = false;

                        EntityAI.SetVelocity(Vector2.zero);

                        if (UseLedgeClimb)
                            EntityAI.transform.position = finalJumpDestination;

                        _velocity.y = 0;

                        Reset();

                        OnJumpFinish?.Invoke();
                    }
                }

                //Using this to prevent the entity to get stuck on the Adjusting position state.
                if (IsAdjustingPosition && Time.time >= adjustingStartTime + speed / 3f && !IsJumping)
                {
                    Reset();
                    FinishLanding();
                    FinishClimbLedge();
                }
                #endregion
            }

            #endregion
        }

        private void CheckIfCanJump(ref bool IsJumping, ref float JumpStartTime)
        {
            if ((!WaitTrigger1ForJump && !IsJumping || WaitTrigger1ForJump && !CanJump))
            {
                if (!IsJumping && (EntityAI.Rb.IsTouching(WallContact) || CanNotTouchHeadCollider()))
                {
                    //Start jumping or wait for the Animation to jump
                    IsAdjustingPosition = false;

                    EntityAI.SetVelocity(Vector2.zero);

                    if (WaitTrigger1ForJump)
                        CanJump = true;
                    else
                        StartJump(ref IsJumping, ref JumpStartTime);

                    if (StopMovementOnlanding && (!UseLedgeClimb || IsLedgeJump))
                        IsWaitingLandingFinish = true;
                }
                else if (!IsJumping)
                {
                    if (!IsAdjustingPosition)
                    {
                        AdjustPositionBeforeJump(Speed / 1.5f);
                    }
                }
            }
        }

        protected void CalculateJumpPosition(Vector3[] Path)
        {
            //TODO: Maybe remove this
            if (!IsGrounded)
                return;

            for (int i = 0; i < Path.Length; i++)
            {
                Vector2 currentPos = Path[i];

                MaykerStudio.ExtDebug.DrawEllipse(currentPos, .1f, .1f, 15, Color.green, 0.2f);

                if (LedgeCheck)
                {
                    normalJumpModule.Update(Path, currentPos, i);

                    if (normalJumpModule.Break)
                    {
                        normalJumpModule.Break = false;
                        break;
                    }
                }
                else
                {
                    ledgeJumpModule.Update(Path, currentPos, i);

                    if (ledgeJumpModule.Break)
                    {
                        ledgeJumpModule.Break = false;
                        break;
                    }
                }
            }
        }

        public bool CheckJumpGroundPosition(Vector3[] Path, int startIndex)
        {
            Vector2 groundPosition;

            var tuple = GetCellsAlongPath(Path, startIndex);

            groundPosition = tuple.Item1;

            if (groundPosition != default)
            {
                feetPosition = groundPosition + Vector2.up * 0.1f;

                finalJumpDestination = feetPosition - BottomCenter;

                if (EntityAI.enableDebug)
                {
                    MaykerStudio.ExtDebug.DrawEllipse(tuple.Item1, .1f, .1f, 16, Color.gray, 3f);
                    MaykerStudio.ExtDebug.DrawEllipse(tuple.Item2, .1f, .1f, 16, Color.gray, 3f);
                    MaykerStudio.ExtDebug.DrawEllipse(finalJumpDestination, .1f, .1f, 16, Color.green, 3f);
                }

                if (tuple.Item1 != default)
                {
                    if (tuple.Item2.x < tuple.Item1.x)
                        cornerDestination1 = finalJumpDestination + Vector2.left;
                    else if (tuple.Item2.x != tuple.Item1.x)
                        cornerDestination1 = finalJumpDestination + Vector2.right;
                    else
                    {
                        Reset();
                        return false;
                    }
                }

                IsAdjustingPosition = false;

                LedgePos = feetPosition - (finalJumpDestination - cornerDestination1) - TopCenter;

                if (finalJumpDestination.y + 0.5f < EntityAI.Rb.position.y && LedgeCheck ||
                    finalJumpDestination.y < EntityAI.Rb.position.y + BottomCenter.y && LedgeCheck ||
                    LedgeCheck && feetPosition.y - 0.1f < EntityAI.Rb.position.y + BottomCenter.y ||
                    !EntityAI.IsFacingPosition(finalJumpDestination) && !EntityAI.Rb.IsTouching(WallContact) ||
                    IsSlopeTile(feetPosition) ||
                    EntityAI.IsOnSlope && feetPosition.y < EntityAI.Rb.position.y + TopCenter.y ||
                    Physics2D.OverlapPoint(feetPosition, EntityAI.entityData.whatIsObstacles))
                {
                    Reset();
                    return false;
                }

                float distanceX = Mathf.Abs(EntityAI.transform.position.x - finalJumpDestination.x);
                float distanceY = Mathf.Abs(EntityAI.transform.position.y - finalJumpDestination.y);

                if (Mathf.Round(distanceX) > MaxJumpDistanceX || Mathf.Round(distanceY) > MaxJumpDistanceY)
                {
                    EntityAI.SetVelocityX(0f);

                    if (StopMovementCO == null)
                    {
                        StopMovementCO = EntityAI.StartCoroutine(StopMovement(1f));

                        if (EntityAI.enableDebug)
                            Debug.LogWarning(EntityAI.name + " couldn't not jump because the distance exceeded the limit specified in the state.");
                    }

                    JumpFailed = true;
                    Reset();
                    return false;
                }

                if (EntityAI.enableDebug)
                {
                    MaykerStudio.ExtDebug.DrawEllipse(feetPosition, .1f, .1f, 16, Color.yellow, 3f);

                    MaykerStudio.ExtDebug.DrawEllipse(finalJumpDestination, .1f, .1f, 16, Color.green, 3f);

                    MaykerStudio.ExtDebug.DrawEllipse(cornerDestination1, .1f, .1f, 16, Color.cyan, 3f);

                    MaykerStudio.ExtDebug.DrawEllipse(LedgePos, .1f, .1f, 16, Color.magenta, 3f);
                }

                EntityAI.BlockDamagedAnim = true;

                return true;
            }

            return false;
        }

        private void AdjustPositionBeforeJump(float speed)
        {
            IsAdjustingPosition = true;
            adjustingStartTime = Time.time;

            Vector2 cornerToFinalDir = (finalJumpDestination - cornerDestination1).normalized;

            if (cornerToFinalDir.x > 0)
            {
                if (EntityAI.Rb.position.x > finalJumpDestination.x)
                    EntityAI.SetVelocityX(-speed);
                else
                    EntityAI.SetVelocityX(speed);
            }
            else
            {
                if (EntityAI.Rb.position.x < finalJumpDestination.x)
                    EntityAI.SetVelocityX(speed);
                else
                    EntityAI.SetVelocityX(-speed);
            }
        }

        private bool CanNotTouchHeadCollider()
        {
            Vector2 origin = ((Vector2)EntityAI.Collider.bounds.center + cornerDestination1) / 2f;
            origin.x = EntityAI.Collider.bounds.center.x;
            Vector2 size = new Vector2() { x = EntityAI.Collider.bounds.size.x, y = Mathf.Abs(EntityAI.Rb.position.y - finalJumpDestination.y) };

            if (origin.y < Center.y)
                return true;

            if(EntityAI.enableDebug)
                MaykerStudio.ExtDebug.BoxCast(origin, size, 0f, Vector2.zero, 0f, EntityAI.entityData.whatIsObstacles);

            return !Physics2D.BoxCast(origin, size, 0f, Vector2.zero, 0f, EntityAI.entityData.whatIsObstacles);
        }

        private bool TopLeftCheck() => Physics2D.Raycast(Center + CenterLeft, Vector2.up, Mathf.Abs(EntityAI.Rb.position.y - finalJumpDestination.y), EntityAI.entityData.whatIsObstacles);

        private bool TopRightCheck() => Physics2D.Raycast(Center + CenterRight, Vector2.up, Mathf.Abs(EntityAI.Rb.position.y - finalJumpDestination.y), EntityAI.entityData.whatIsObstacles);

        #endregion

        public (Vector3, Vector3) GetCellsAlongPath(Vector3[] path, int startIndex)
        {
            Vector3Int previousPos = default;
            Vector2 currentPos = default;
            Vector2 nextPos = default;

            for (int i = startIndex; i < path.Length; i++)
            {
                if (i + 1 < path.Length)
                {
                    currentPos = path[i];
                    nextPos = path[i + 1];

                    float distanceX = Mathf.Abs(EntityAI.transform.position.x - currentPos.x);
                    float distanceY = Mathf.Abs(EntityAI.transform.position.y - currentPos.y);

                    if (distanceX > MaxJumpDistanceX || distanceY > MaxJumpDistanceY)
                    {
                        break;
                    }

                    var points = GetPointsOnPath(currentPos, ref nextPos, ref previousPos);

                    if (points != default)
                    {
                       return points;
                    }
                }
            }

            if (HasTile(previousPos + Vector3Int.down * 2))
            {
                Vector3Int prev = tileMap.WorldToCell(currentPos);
                Vector2 end = path[path.Length - 1];
                return GetPointsOnPath(tileMap.CellToWorld(previousPos + Vector3Int.down), ref end, ref prev);
            }
            else if (HasTile(previousPos + Vector3Int.right + Vector3Int.down * 2))
            {
                Vector3Int prev = tileMap.WorldToCell(path[path.Length - 2]);
                Vector2 end = path[path.Length - 1];
                return GetPointsOnPath(tileMap.CellToWorld(previousPos + Vector3Int.right + Vector3Int.down), ref end, ref prev);
            }
            else if (HasTile(previousPos + Vector3Int.left + Vector3Int.down * 2))
            {
                Vector3Int prev = tileMap.WorldToCell(path[path.Length - 2]);
                Vector2 end = path[path.Length - 1];
                return GetPointsOnPath(tileMap.CellToWorld(previousPos + Vector3Int.left + Vector3Int.down), ref end, ref prev);
            }

            return default;
        }

        /// <summary>
        /// Bresenham's line algorithm
        /// </summary>
        /// <returns></returns>
        protected virtual (Vector3, Vector3) GetPointsOnPath(Vector2 start, ref Vector2 end, ref Vector3Int previousPos)
        {
            float difX = end.x - start.x;
            float difY = end.y - start.y;
            float dist = Mathf.Abs(difX) + Mathf.Abs(difY);

            float dx = difX / dist;
            float dy = difY / dist;

            float x, y;

            for (int i = 0; i <= dist; i++)
            {
                x = (start.x + dx * i);
                y = (start.y + dy * i);

                if (start == end)
                    return default;

                Vector2 pos = new Vector2() { x = x, y = y };

                Vector3Int cellPos = tileMap.WorldToCell(pos);

                if(EntityAI.enableDebug)
                    MaykerStudio.ExtDebug.DrawBox(cellPos + Vector3.one / 2, Vector3.one / 2, Quaternion.identity, Color.magenta);

                if (HasTile(cellPos + Vector3Int.down))
                {
                    if (LedgeCheck || !LedgeCheck && Vector2.Distance(cellPos + Vector3.right * 0.5f, EntityAI.Rb.position) >= 2)
                        return RefinePoints(cellPos + Vector3.right * 0.5f, previousPos + Vector3.right * 0.5f);
                }
                else if ((!LedgeCheck || EntityAI.Rb.IsTouching(WallContact)) && HasTile(cellPos + Vector3Int.down * 2))
                {
                    if (Vector3.Distance(cellPos + Vector3.right * 0.5f, EntityAI.Rb.position) >= 2)
                        return RefinePoints(cellPos + Vector3.right * 0.5f + Vector3.down, previousPos + Vector3.right * 0.5f + Vector3.down);
                }

                end = (Vector2Int)(cellPos + (tileMap.WorldToCell(end) - cellPos));
                previousPos = cellPos;

            }

            return default;
        }

        private (Vector3, Vector3) RefinePoints(Vector3 pos1, Vector3 pos2)
        {
            Vector3 dir = (pos2 - pos1).normalized;

            if (dir.x > 0)
            {
                if (HasTile(tileMap.WorldToCell(pos1 + Vector3.down + Vector3.right)))
                {
                    if (!HasTile(tileMap.WorldToCell(pos1 + Vector3.down + Vector3.right * 2))
                        && HasTile(tileMap.WorldToCell(pos1 + Vector3.down + Vector3.left)))
                    {
                        pos1 += Vector3.right;
                        pos2 += Vector3.right;
                    }
                }
            }
            else
            {
                if (HasTile(tileMap.WorldToCell(pos1 + Vector3.down + Vector3.left)))
                {
                    if (!HasTile(tileMap.WorldToCell(pos1 + Vector3.down + Vector3.left * 2))
                        && HasTile(tileMap.WorldToCell(pos1 + Vector3.down + Vector3.right)))
                    {
                        pos1 += Vector3.left;
                        pos2 += Vector3.left;
                    }
                }
            }

            if (pos1.x == pos2.x)
            {
                Vector3Int cellPos = tileMap.WorldToCell(pos1 + Vector3.left);

                if (!HasTile(cellPos + Vector3Int.down))
                {
                    pos2 = cellPos + Vector3.right * 0.5f;

                    return (pos1, pos2);
                }

                cellPos = tileMap.WorldToCell(pos1 + Vector3.right);

                if (!HasTile(cellPos + Vector3Int.down))
                {
                    pos2 = cellPos + Vector3.right * 0.5f;
                    return (pos1, pos2);
                }
            }

            return (pos1, pos2);
        }

        public bool HasTile(Vector3Int pos)
        {
            if (NoTilemap)
            {
                if(EntityAI.enableDebug)
                    MaykerStudio.ExtDebug.DrawEllipse(tileMap.CellToWorld(pos) + Vector3.one / 2f, 0.45f, 0.45f, 32, Color.blue, 0.5f);

                return Physics2D.OverlapCircle(tileMap.CellToWorld(pos) + Vector3.one / 2f, 0.45f, EntityAI.entityData.whatIsObstacles);
            }
            else
            {
                return tileMap.HasTile(pos);
            }
        }

        protected bool IsSlopeTile(Vector3 pos)
        {
            RaycastHit2D info = Physics2D.Raycast(pos, Vector2.down, 2f, EntityAI.entityData.whatIsObstacles);

            if (Mathf.Abs(info.normal.x) > 0.1f)
                return true;

            return false;
        }

        private IEnumerator StopMovement(float duration)
        {
            CanMove = false;

            yield return new WaitForSeconds(duration);

            CanMove = true;

            StopMovementCO = null;
        }
    }
}