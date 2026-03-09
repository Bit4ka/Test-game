using MaykerStudio;
using MaykerStudio.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/2D TopDown|Flying Only/Avoid Target NavMesh")]
    public class E_AvoidTarget_NavMesh : EntityState
    {
        [Help("State only works for Topdown or to flying enemies.")]
        [BeginGroup("Variables")]
        [SerializeField]
        protected EntityAnimation FollowAnimation;

        [SerializeField]
        protected EntityAnimation IdleAnimation;

        [SerializeField]
        protected EntityAnimation AvoidAnimation;

        [SerializeField]
        [Tooltip("The radius to avoid the target.")]
        protected float AvoidRadius = 1f;

        [SerializeField]
        [Tooltip("The number of rays that will be cast along with the angle.")]
        protected int NumberOfDirections = 5;

        [SerializeField]
        [Tooltip("The radius of surroundings detection.")]
        protected float DetectionRadius = 1f;

        [SerializeField]
        [Tooltip("The angle of the detection.")]
        protected float Angle = 90f;

        [SerializeField]
        protected DetectionType targetDetectionType;

        [SerializeField]
        [Range(5f, 270)]
        [ShowIf(nameof(targetDetectionType), DetectionType.FOV)]
        protected float FOV = 15f;

        [SerializeField]
        protected float MaxTargetDetectionRange = 10f;

        [SerializeField]
        private float MinTargetDetectionRange = 5f;

        [SerializeField]
        protected float MoveSpeed = 5f;

        [SerializeField]
        [EndGroup]
        protected bool FaceTarget;

        [BeginGroup("Transitions")]
        [SerializeField]
        [Header("If target In Min Distance")]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        protected EntityState NextState1;

        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Header("If target Out of Max Distance")]
        [EndGroup]
        protected EntityState NextState2;
        public Transform Target { get; protected set; }

        private float EndReachedDistance;

        protected bool TargetInMaxDistance, TargetInMinDistance;

        protected bool canFollow;

        protected List<Vector2> directions = new List<Vector2>();

        private static Vector2 targetDirection = Vector2.zero;

        private readonly Comparison<Vector2> compareDots = (a, b) => Vector2.Dot(a, targetDirection).CompareTo(Vector2.Dot(b, targetDirection));

#if UNITY_EDITOR
        private Color fullDot = new Color(0, 1, 0, 1f);
        private Color normalDot = new Color(0, 1, 0, .25f);
        private Color negDot = new Color(1, 0, 0, 1f);
#endif

        public override void DoChecks()
        {
            TargetInMinDistance = EntityAI.CheckTargetsInRadius(MinTargetDetectionRange);

            switch (targetDetectionType)
            {
                case DetectionType.Circle:
                    TargetInMaxDistance = EntityAI.CheckTargetsInRadius(MaxTargetDetectionRange);
                    break;
                case DetectionType.Ray:
                    TargetInMaxDistance = EntityAI.CheckTargetsInRange(MaxTargetDetectionRange);
                    break;
                case DetectionType.FOV:
                    TargetInMaxDistance = EntityAI.CheckTargetsInFieldOfView(FOV, MaxTargetDetectionRange);
                    break;
                case DetectionType.Box:
                    TargetInMaxDistance = EntityAI.CheckBox();
                    break;
                default:
                    break;
            }
        }

        public override void Enter()
        {
            base.Enter();

            canFollow = true;

            EntityAI.Flying = true;
            EntityAI.Rb.gravityScale = 0f;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            if (directions.Count == 0)
            {
                PopulateDirections();
            }

            EntityAI.SeeThroughWalls = true;
            Target = EntityAI.GetFirstTargetTransform();
            EntityAI.SeeThroughWalls = CanSeeThroughWalls;

            EntityAI.StartCoroutine(DisableAIPathOnDamageHop());
        }

        public override void Exit()
        {
            base.Exit();
        }

        protected virtual void PopulateDirections()
        {
            for (int i = 0; i < NumberOfDirections; i++)
            {
                Quaternion rotation = EntityAI.transform.rotation;

                Quaternion rotationMod = Quaternion.AngleAxis(i / ((float)NumberOfDirections) * Angle * 2 - Angle, EntityAI.transform.forward);

                Vector2 direction = (rotation * rotationMod * Vector3.right).normalized;

                directions.Add(direction);
            }
        }

        protected virtual Vector2 GetBestDirections(Vector2 targetDir)
        {
            Vector2 bestDirection = Vector2.zero;

            targetDirection = targetDir;

            directions.Sort(compareDots);

            for (int i = 0; i < directions.Count; i++)
            {
                Vector2 direction = directions[i];

                if (!Physics2D.Raycast(EntityAI.transform.position, direction, DetectionRadius + (DetectionRadius / 2), EntityAI.entityData.whatIsObstacles))
                {
                    bestDirection += direction * DetectionRadius;

                    if (EntityAI.Rb.IsTouchingLayers(EntityAI.entityData.whatIsObstacles))
                    {
                        bestDirection -= (1.0f / directions.Count) * direction * DetectionRadius;
                    }

                    Debug.DrawRay((Vector2)EntityAI.transform.position, direction * (DetectionRadius + (DetectionRadius / 2)));

                    break;
                }
                else
                {
                    bestDirection -= (1.0f  / directions.Count) * direction;
                }
            }

            return bestDirection + (Vector2)EntityAI.transform.position;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            HandleAnimations();

            if (!TargetInMaxDistance)
            {
                if(NextState2 != null && !NextState2.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState2);
                    return;
                }
            }
            else if (TargetInMinDistance)
            {
                if (NextState1 != null && !NextState1.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState1);
                    return;
                }
            }

            if (FaceTarget)
            {
                if (Target.transform.position.x + 0.5f > EntityAI.transform.position.x && EntityAI.FacingDirection == -1)
                    EntityAI.Flip();
                else if (Target.transform.position.x - 0.5f < EntityAI.transform.position.x && EntityAI.FacingDirection == 1)
                    EntityAI.Flip();
            }
            else
            {
                if (EntityAI.Rb.linearVelocity.x > 0.5f && EntityAI.FacingDirection == -1)
                    EntityAI.Flip();
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (!Target || !canFollow)
                return;

            Vector2 targetDir = (Target.position - EntityAI.transform.position).normalized;

            if(Vector2.Distance(Target.position, EntityAI.transform.position) < AvoidRadius)
            {
                Vector2 pos = GetBestDirections(targetDir);

                EntityAI.SetVelocity((pos - EntityAI.Rb.position).normalized * MoveSpeed);
            }
            else if(Vector2.Distance(Target.position, EntityAI.transform.position) > AvoidRadius + 1f)
            {
                EntityAI.SetVelocity(targetDir * MoveSpeed);
            }
            else
            {
                EntityAI.SetVelocity(Vector2.zero);
            }
        }

        public virtual void HandleAnimations()
        {
            if (!Target)
                return;

            if (Vector2.Distance(Target.position, EntityAI.transform.position) < AvoidRadius)
            {
                EntityAI.PlayAnim(AvoidAnimation);
                EntityAI.PlayAudio(AvoidAnimation.SoundAsset);
            }
            else if (Vector2.Distance(Target.position, EntityAI.transform.position) > AvoidRadius + 1f)
            {
                EntityAI.PlayAnim(FollowAnimation);
                EntityAI.PlayAudio(FollowAnimation.SoundAsset);
            }
            else
            {
                EntityAI.PlayAnim(IdleAnimation);
                EntityAI.PlayAudio(IdleAnimation.SoundAsset);
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return port.Connection?.node;
        }

        private IEnumerator DisableAIPathOnDamageHop()
        {
            while(StateMachine.CurrentState == this)
            {
                if (EntityAI.IsKnockback)
                {
                    canFollow = false;
                }
                else
                {
                    canFollow = true;
                }

                yield return null;
            }
        }


#if UNITY_EDITOR
        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();

            if (Target)
                ExtDebug.DrawEllipse(Target.position, AvoidRadius, AvoidRadius, 32, Color.yellow);

            ExtDebug.DrawEllipse(EntityAI.transform.position, MinTargetDetectionRange, MinTargetDetectionRange, 16, Color.cyan);

            ExtDebug.DrawDetectionType(EntityAI, targetDetectionType, FOV, MaxTargetDetectionRange, Color.blue);

            ExtDebug.DrawEllipse(EntityAI.transform.position, DetectionRadius, DetectionRadius, 16, Color.white);

            Vector2 targetDir = (Target.position - EntityAI.transform.position).normalized;

            for (int i = 0; i < NumberOfDirections; i++)
            {
                Quaternion rotation = EntityAI.transform.rotation;

                Quaternion rotationMod = Quaternion.AngleAxis(i / ((float)NumberOfDirections + 1) * Angle * 2 - Angle, EntityAI.transform.forward);

                Vector2 direction = (rotation * rotationMod * Vector3.right).normalized;

                float dot = Vector2.Dot(direction, -targetDir);

                if (dot <= -0.9f)
                {
                    Debug.DrawRay(EntityAI.transform.position, direction * dot * DetectionRadius, fullDot);
                }
                else if (dot < 0)
                {
                    Debug.DrawRay(EntityAI.transform.position, direction * dot * DetectionRadius, normalDot);
                }
                else
                {
                    Debug.DrawRay(EntityAI.transform.position, direction * -dot * DetectionRadius, negDot);
                }
            }
        }
#endif

    }
}