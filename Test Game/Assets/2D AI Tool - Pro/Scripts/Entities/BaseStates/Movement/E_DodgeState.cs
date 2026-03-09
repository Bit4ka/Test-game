using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Movement/Dodge State")]
    public class E_DodgeState : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation dodgeAnimation;

        [SerializeField]
        [HideIf(nameof(gameType), D_Entity.GameType.Topdown2D)]
        private EntityAnimation fallingAnimation;

        [SerializeField]
        [HideIf(nameof(gameType), D_Entity.GameType.Topdown2D)]
        private EntityAnimation landingAnimation;

        [SerializeField]
        [Tooltip("Select the same type you use on EntityData ")]
        private D_Entity.GameType gameType;

        [SerializeField]
        [HideIf(nameof(gameType), D_Entity.GameType.Topdown2D)]
        [Tooltip("This is the direction of the dodge, you can put normalized values from 0 to 1. SUGGESTION: Put 1 and 1 to make like a jump angle, or 1 and 0 to just a dash.")]
        private Vector2 DodgeAngle;

        [SerializeField]
        [HideIf(nameof(gameType), D_Entity.GameType.Platformer2D)]
        [Tooltip("The obstacle detection range the entity will look for.")]
        private float dodgeDetectionRange = 1f;

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("The speed of the dodge.")]
        private float speed = 1f;

        [Min(0.0f)]
        [SerializeField]
        private float dodgeDuration = .5f;

        [SerializeField]
        [Tooltip("The state will flip the entity first, if it doesn't detect a ledge in its final position. If is unchecked, the entity will not perform the dodge.")]
        private bool FlipIfNoLedge = true;

        [SerializeField]
        [Tooltip("The entity can be damaged or not in the state.")]
        private bool CanBeDamaged = true;

        [EndGroup]
        [SerializeField]
        private bool stopVelocityOnEnd = true;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If dodge is over")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [Header("If NextState1 in Cooldown")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [EndGroup]
        private EntityState NextState2;

        #endregion

        private float timer;

        private bool IsGrounded;

        private Vector2 FuturePosition, FutureOppositePosition;

        private List<Vector2> nonHitDir = new List<Vector2>();

        private DodgeDirection dodgeDirection = new DodgeDirection();

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

            IsGrounded = EntityAI.CheckGround();
        }

        public override void Enter()
        {
            base.Enter();

            dodgeDirection.detectionRange = dodgeDetectionRange;
            dodgeDirection.layerMask = EntityAI.entityData.whatIsObstacles;
            dodgeDirection.position = EntityAI.transform.position;
            dodgeDirection.nonHitDir = nonHitDir;

            EntityAI.StopKnockBack = false;
            EntityAI.CanBeDamaged = CanBeDamaged;

            EntityAI.FlipToTarget();

            EntityAI.PlayAnim(dodgeAnimation);
            EntityAI.PlayAudio(dodgeAnimation.SoundAsset);

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            timer = 0f;

            PlayDamagedAnim = false;

            if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                //TODO: Test jump like dodge.
                FuturePosition.Set(DodgeAngle.x * speed * -EntityAI.FacingDirection, DodgeAngle.y);
                FutureOppositePosition.Set(DodgeAngle.x * speed * EntityAI.FacingDirection, DodgeAngle.y);

                Vector2 pos = (Vector2)EntityAI.transform.position + FuturePosition * (dodgeDuration + 0.1f) + EntityAI.ledgeCheck.localPosition * Vector2.up;
                Vector2 pos2 = (Vector2)EntityAI.transform.position + FutureOppositePosition * (dodgeDuration + 0.1f) + EntityAI.ledgeCheck.localPosition * Vector2.up;

#if UNITY_EDITOR
                MaykerStudio.ExtDebug.DrawEllipse(pos, 1, 1, 16, Color.yellow, 1f);

                Debug.DrawRay(pos, Vector2.down *EntityAI.entityData.ledgeCheckDistance * 2, Color.yellow, 1f);

                MaykerStudio.ExtDebug.DrawEllipse(pos2, 1, 1, 16, Color.yellow, 1f);

                Debug.DrawRay(pos2, Vector2.down * EntityAI.entityData.ledgeCheckDistance * 2, Color.yellow, 1f);

#endif
                if (!Physics2D.Raycast(pos, Vector2.down, EntityAI.entityData.ledgeCheckDistance * 2, EntityAI.entityData.whatIsObstacles))
                {
                    if (FlipIfNoLedge && Physics2D.Raycast(pos2, Vector2.down, EntityAI.entityData.ledgeCheckDistance * 2, EntityAI.entityData.whatIsObstacles))
                        EntityAI.Flip();
                    else
                    {
                        timer += dodgeDuration;
                        return;
                    }
                }

                EntityAI.SetVelocity(speed, DodgeAngle, -EntityAI.FacingDirection);
            }
            else
            {
                EntityAI.SetVelocity(GetDodgeDirection() * speed);
            }
        }
        /// <summary>
        /// This function get a random direction that doesn't collide with a obstacle and make the entity dodge to that direction
        /// </summary>
        /// <returns> Direction of the dodge </returns>
        private Vector2 GetDodgeDirection()
        {
            Transform target = EntityAI.GetFirstTargetTransform();

            dodgeDirection.CheckDir();

            if (dodgeDirection.nonHitDir.Count > 0)
            {
                Vector2 notHitObstacleDirection = dodgeDirection.nonHitDir[Random.Range(0, nonHitDir.Count)];

                Debug.DrawRay(EntityAI.transform.position, notHitObstacleDirection * dodgeDetectionRange, Color.blue, 1f);

                return notHitObstacleDirection;
            }

            if (target)
            {
                return (target.position - EntityAI.transform.position).normalized;
            }
            else
            {
                return EntityAI.transform.forward;
            }

        }

        public override void Exit()
        {
            base.Exit();

            EntityAI.StopKnockBack = false;
            EntityAI.CanBeDamaged = true;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
            {
                if (IsGrounded && Time.time >= startTime + 0.1f)
                    timer += Time.deltaTime;
            }
            else
                timer += Time.deltaTime;

            if (Time.time >= startTime + 0.1f)
            {
                if (EntityAI.Rb.linearVelocity.y < -0.1f && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                {
                    EntityAI.PlayAnim(landingAnimation);
                    EntityAI.PlayAudio(landingAnimation.SoundAsset);
                }

                if (timer >= dodgeDuration)
                {
                    if (stopVelocityOnEnd)
                        EntityAI.SetVelocity(Vector2.zero);

                    if (NextState1 != null && !NextState1.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState1);
                    }
                    else if (NextState2 != null && !NextState2.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState2);
                    }
                }
            }
            else if (EntityAI.Rb.linearVelocity.y < 0f && EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                timer = 0f;
                EntityAI.PlayAnim(fallingAnimation);
                EntityAI.PlayAudio(fallingAnimation.SoundAsset);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (IsGrounded && !EntityAI.CheckLedgeBehind())
            {
                EntityAI.SetVelocityX(0f);
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return base.GetValue(port);
        }

        public class DodgeDirection
        {
            public List<Vector2> nonHitDir { get; set; }
            public Vector3 position { get; set; }
            public float detectionRange { get; set; }
            public LayerMask layerMask { get; set; }

            public void CheckDir()
            {
                nonHitDir.Clear();

                RaycastHit2D N = Physics2D.Raycast(position, new Vector2(0, 1), detectionRange, layerMask);
                RaycastHit2D NE = Physics2D.Raycast(position, new Vector2(1, 1), detectionRange, layerMask);
                RaycastHit2D E = Physics2D.Raycast(position, new Vector2(1, 0), detectionRange, layerMask);
                RaycastHit2D SE = Physics2D.Raycast(position, new Vector2(1, -1), detectionRange, layerMask);
                RaycastHit2D S = Physics2D.Raycast(position, new Vector2(0, -1), detectionRange, layerMask);
                RaycastHit2D SW = Physics2D.Raycast(position, new Vector2(-1, -1), detectionRange, layerMask);
                RaycastHit2D W = Physics2D.Raycast(position, new Vector2(-1, 0), detectionRange, layerMask);
                RaycastHit2D NW = Physics2D.Raycast(position, new Vector2(-1, 1), detectionRange, layerMask);

                if (!N)
                    nonHitDir.Add(new Vector2(0, 1));
                if (!NE)
                    nonHitDir.Add(new Vector2(1, 1));
                if (!E)
                    nonHitDir.Add(new Vector2(1, 0));
                if (!SE)
                    nonHitDir.Add(new Vector2(1, -1));
                if (!S)
                    nonHitDir.Add(new Vector2(0, -1));
                if (!SW)
                    nonHitDir.Add(new Vector2(-1, -1));
                if (!W)
                    nonHitDir.Add(new Vector2(-1, 0));
                if (!NW)
                    nonHitDir.Add(new Vector2(-1, 1));
            }

        }
    }
}