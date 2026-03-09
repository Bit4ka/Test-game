using MaykerStudio;
using MaykerStudio.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Patrolling NavMesh State")]
    public class E_Patrolling_NavMesh : E_FollowTarget_NavMesh_Base
    {
        #region Variables
        [BeginGroup("Patrolling Options")]
        [SerializeField]
        [Tooltip("Check this if you want to use the children of a parent transform as patrol points. Leave the PatrolPoints array empty if you check this.")]
        private bool UseParentTransform;

        [SerializeField]
        [ShowIf("UseParentTransform", true)]
        private string ParentTransformName;

        [Tooltip("Make sure all the waypoint the you named will be on the same scene as the entity")]
        [SerializeField]
        [ReorderableList]
        private PatrolPoint[] PatrolPoints;

        [SerializeField]
        [Tooltip("The minimum distance from a patrol point before the entity switchs to the next one.")]
        private float nextPatrolPointDistance = 1f;

        [EndGroup]
        [SerializeField]
        [Tooltip("When the entity arrive in the current patrol position it will stop and wait for this delay.")]
        private float delayBetweenPoints = 2f;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If target in agroRange")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        private EntityState NextState1;

        [Header("If NextState1 in cooldown")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        private EntityState NextState2;

        [Header("If all points reached")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        [EndGroup]
        private EntityState NextState3;

        #endregion

        private float JumpStartTime, delayPointTimer;

        private bool InMaxAgroDistance, canMove;

        private int CurrentPatrolPointIndex = 0;

        private Transform CurrentPatrolPoint;

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

        public override void Enter()
        {
            base.Enter();

            canMove = true;

            delayPointTimer = 0;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
            NextState3 = CheckState(NextState3);

            if (UseParentTransform && PatrolPoints.Length == 0)
            {
                if (ParentTransformName.Length > 0)
                {
                    List<Transform> list = new List<Transform>();

                    GameObject go = GameObject.Find(ParentTransformName);

                    if (go == null)
                    {
                        Debug.LogError("Could not find the " + ParentTransformName + " game object in current active scene.");
                        return;
                    }

                    Transform parent = go.transform;

                    foreach (Transform child in parent)
                    {
                        if (child.gameObject.activeSelf)
                            list.Add(child);
                    }

                    PatrolPoints = new PatrolPoint[list.Count];

                    for (int i = 0; i < PatrolPoints.Length; i++)
                    {
                        PatrolPoints[i] = new PatrolPoint();

                        PatrolPoint p = PatrolPoints[i];
                        p.PointName = list[i].name;
                        p.PatrolPointTransform = list[i];
                    }
                }
                else
                {
                    Debug.LogError("You need to specify the name of the gameObject.");
                }
            }

            if (PatrolPoints.Length == 0)
            {
                Debug.LogError("Patrol points list is empty");
                return;
            }

            SetPatrolPoint();
        }

        public override void Exit()
        {
            base.Exit();

            isJumping = false;

            if (entityFlying)
            {
                EntityAI.Flying = false;
                EntityAI.Rb.gravityScale = EntityAI.OriginalGravityScale;
                EntityAI.IgnoreFallClamp = false;
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (PatrolPoints == null)
                return;

            if (PatrolPoints.Length == 0)
                return;

            #region Transition

            if (InMaxAgroDistance)
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
                    else if (NextState2 != null && !NextState2.IsInCooldown)
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
            else
                delayTimer = 0f;

            #endregion

            #region Patrol point logic

            if (PatrolPoints.Length == 0)
                return;

            if (Vector2.Distance(EntityAI.transform.position, CurrentPatrolPoint.position) < nextPatrolPointDistance)
            {
                canMove = false;

                delayPointTimer += Time.deltaTime;

                EntityAI.PlayAnim(IdleAnimation);
                EntityAI.PlayAudio(IdleAnimation.SoundAsset);

                if (delayPointTimer >= delayBetweenPoints)
                {
                    if (CurrentPatrolPointIndex + 1 < PatrolPoints.Length)
                    {
                        CurrentPatrolPointIndex++;
                    }
                    else
                    {
                        CurrentPatrolPointIndex = 0;

                        if (NextState3 != null && !NextState3.IsInCooldown)
                            StateMachine.ChangeState(NextState3);
                    }
                    canMove = true;
                    delayPointTimer = 0;

                    SetPatrolPoint();
                }
            }
            else
            {
                delayPointTimer = 0;
                canMove = true;
            }

            #endregion
        }

        public override void PhysicsUpdate()
        {
            DoChecks();

            if (Path.corners.Length >= 2 || EntityAI.Rb.linearVelocity.magnitude < 0.1f)
                EntityAI.Rb.sharedMaterial = originalMat;
            else
                EntityAI.Rb.sharedMaterial = null;

            if (PatrolPoints == null)
                return;

            if (PatrolPoints.Length == 0)
                return;

            if (Path != null)
            {
                if (canMove)
                {
                    pathFollow.Move(true, faceTarget, Path.corners, TargetPosition != Vector3.zero ? TargetPosition : Target.position,
                    speed, maxVelocity, ref CurrentWaypoint, ref isJumping, ref jumpStartTime);
                }
                else
                {
                    if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                        EntityAI.SetVelocityX(Mathf.Lerp(speed * EntityAI.FacingDirection, 0, speed * 10 * Time.deltaTime));
                    else
                    {
                        EntityAI.SetVelocity(Vector2.Lerp(EntityAI.Rb.linearVelocity, Vector2.zero, speed * 10 * Time.deltaTime));
                    }
                }
            }
            else
            {
                if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                    EntityAI.SetVelocityX(Mathf.Lerp(speed * EntityAI.FacingDirection, 0, speed * 10 * Time.deltaTime));
                else
                {
                    EntityAI.SetVelocity(Vector2.Lerp(EntityAI.Rb.linearVelocity, Vector2.zero, speed * 10 * Time.deltaTime));
                }
            }
        }

        private void SetPatrolPoint()
        {
            if (PatrolPoints[CurrentPatrolPointIndex].PatrolPointTransform == null)
                PatrolPoints[CurrentPatrolPointIndex].PatrolPointTransform = GameObject.Find(PatrolPoints[CurrentPatrolPointIndex].PointName).transform;

            CurrentPatrolPoint = PatrolPoints[CurrentPatrolPointIndex].PatrolPointTransform;
            Target = CurrentPatrolPoint;
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);
            NextState3 = GetFromPort("NextState3", port, NextState3);

            return base.GetValue(port);
        }

        [Serializable]
        public class PatrolPoint
        {
            [Tooltip("The state will use this string to search for the point on scene, make sure the point transform is on the same scene of the entity")]
            public string PointName;

            public Transform PatrolPointTransform { get; set; }

        }
    }
}