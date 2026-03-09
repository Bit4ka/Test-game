using MaykerStudio;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using MaykerStudio.Attributes;
using MaykerStudio.Types;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/2D TopDown|Flying Only/Wandering NavMesh State")]
    public class E_WanderingState_NavMesh : EntityState
    {
        #region Variables

        [Help("State only works for Topdown or to flying enemies.")]
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation WanderingAnimation;

        [SerializeField]
        private EntityAnimation IdleAnimation;

        [SerializeField]
        private float MaxDistanceFromCentralPoint = 10f;

        [SerializeField]
        private float minDistanceFromDestination = 0.5f;

        [SerializeField]
        [Range(0, 10f)]
        private float MinDistanceToWander = 2f;

        [SerializeField]
        private float DelayBetweenNextPoints = 1f;

        [SerializeField]
        private float MaxSecondsInPath = 2f;

        [SerializeField]
        private DetectionType detectionType;

        [Min(0.1f)]
        [SerializeField]
        private float MaxDetectionRange = 10f;

        [SerializeField]
        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        [Range(5f, 270f)]
        private float FOV = 25f;
        
        [SerializeField]
        private float Speed = 10f;

        [EndGroup]
        [AgentID]
        [SerializeField]
        private int agentType;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If Target detected")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [Header("If NextState1 in cooldown")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [SerializeField]
        [Disable]
        [EndGroup]
        private EntityState NextState2;

        #endregion

        private NavMeshPath path;
        private NavMeshQueryFilter queryFilter;

        private Vector2 StartPosition, directionToStart, targetPos, targetDir = Vector2.zero;

        private float delayTimer, weight;

        private bool targetDetected;

        public override void Enter()
        {
            base.Enter();

            if (path == null)
            {
                path = new NavMeshPath();
                queryFilter = new NavMeshQueryFilter() { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            }

            EntityAI.Flying = true;
            EntityAI.Rb.gravityScale = 0;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            if (StartPosition == Vector2.zero)
                StartPosition = EntityAI.transform.position;

            EntityAI.PlayAnim(WanderingAnimation);
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override void DoChecks()
        {
            switch (detectionType)
            {
                case DetectionType.Circle:
                    targetDetected = EntityAI.CheckTargetsInRadius(MaxDetectionRange);
                    break;
                case DetectionType.Ray:
                    targetDetected = EntityAI.CheckTargetsInRange(MaxDetectionRange);
                    break;
                case DetectionType.FOV:
                    targetDetected = EntityAI.CheckTargetsInFieldOfView(FOV, MaxDetectionRange);
                    break;
                case DetectionType.Box:
                    targetDetected = EntityAI.CheckBox();
                    break;
                default:
                    break;
            }

        }

        private Vector2 PickDirection()
        {
            weight = Mathf.Clamp01(Vector2.Distance(StartPosition, EntityAI.transform.position) / MaxDistanceFromCentralPoint);
            directionToStart = (StartPosition - (Vector2)EntityAI.transform.position).normalized;
            targetDir.Set(Random.Range(1f, -1f), Random.Range(1f, -1f));

            if (directionToStart != Vector2.zero && weight != 0f)
            {
                targetDir = Vector2.LerpUnclamped(targetDir, directionToStart, weight);
                targetDir = targetDir.normalized;
            }

            RaycastHit2D ray = Physics2D.Raycast(EntityAI.transform.position, targetDir, MaxDistanceFromCentralPoint, EntityAI.entityData.whatIsObstacles);

            if (ray.collider)
            {
                Debug.DrawLine(EntityAI.transform.position, ray.point, Color.yellow, 0.5f);
            }
            else
            {
                Debug.DrawRay(EntityAI.transform.position, targetDir * MaxDistanceFromCentralPoint, Color.yellow, 0.5f);
            }

            //Prevent block when returning to spawn position
            if (weight >= 0.9f && ray.collider)
            {
                targetDir *= Vector2.Distance(StartPosition, EntityAI.transform.position);
            }
            else
            {
                targetDir *= ray.distance > 0 ? Random.Range(MinDistanceToWander, ray.distance - 1f) : Random.Range(MinDistanceToWander, MaxDistanceFromCentralPoint);
            }
            
            targetPos = EntityAI.Rb.position + targetDir;

            NavMesh.CalculatePath(EntityAI.Rb.position, targetPos, queryFilter, path);

            if (path.status != NavMeshPathStatus.PathComplete)
                targetPos = path.corners[path.corners.Length - 1];

            return targetDir;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (targetDetected)
            {
                if(NextState1 != null && !NextState1.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState1);
                    return;
                }
                else if(NextState2 != null && NextState1.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState2);
                    return;
                }
            }

            if (Time.time >= startTime + MaxSecondsInPath)
            {
                delayTimer += Time.deltaTime;

                EntityAI.PlayAnim(IdleAnimation);

                if (delayTimer >= DelayBetweenNextPoints)
                {
                    PickDirection();

                    delayTimer = 0f;
                    startTime = Time.time;

                    EntityAI.PlayAnim(WanderingAnimation);
                }
            }

            if (EntityAI.Rb.linearVelocity.x > 1f && EntityAI.FacingDirection == -1)
                EntityAI.Flip();
            else if (EntityAI.Rb.linearVelocity.x < -1f && EntityAI.FacingDirection == 1)
                EntityAI.Flip();

        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            ExtDebug.DrawEllipse(targetPos, minDistanceFromDestination, minDistanceFromDestination, 32, Color.cyan);

            if(Vector2.Distance(targetPos, EntityAI.Rb.position) > minDistanceFromDestination)
                EntityAI.SetVelocity(targetDir.normalized * Speed);
            else
                EntityAI.SetVelocity(Vector2.zero);
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return port.Connection?.node;
        }

#if UNITY_EDITOR
        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();

            ExtDebug.DrawEllipse(StartPosition, 1f, 1f, 32, Color.cyan);

            ExtDebug.DrawEllipse(StartPosition, MaxDistanceFromCentralPoint, MaxDistanceFromCentralPoint, 32, Color.green);

            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, MaxDetectionRange);
        }
#endif
    }

}