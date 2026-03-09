using MaykerStudio;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/Attacks State")]
    public class E_Attacks : EntityState
    {
        #region Variables
        [BeginGroup("Variables")]
        [ReorderableList(ListStyle.Round, "Attack")]
        [SerializeField]
        [Tooltip("USE ANIMATION EVENTS ON THE ATTACK ANIMATION FOR THE PROPER WORK: AttackTrigger1 to make damage, AttackTrigger2 to movement and animationFinish to finish animation")]
        private Attack[] attacks;

        [SerializeField]
        [TagSelector]
        [ReorderableList(ListStyle.Round, "Tag")]
        [Tooltip("Use this list to filter the targets by tag.")]
        private List<string> targetsTag = new List<string>();

        [Min(0.0f)]
        [SerializeField]
        [Tooltip("The delay before start the first attack")]
        private float delayBeforeStart = 0.5f;

        [SerializeField]
        private bool flipToTarget;

        [SerializeField]
        private bool entityFlying;

        [Disable]
        [SerializeField]
        [EndGroup]
        private Attack CurrentAttack;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If animation finished")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState1;

        [EndGroup]
        [Header("If attack stopped by target")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState2;

        #endregion

        private bool hasHittedTarget;

        private bool _playDamagedAnim;

        private bool move;

        private int attackIndex;

        private float movementTimer;

        private Vector2 moveDirection = Vector2.zero;

        public override void AnimationFinish()
        {
            base.AnimationFinish();

            hasHittedTarget = false;

            CurrentAttack.HasFinished = true;

            if (attackIndex + 1 < attacks.Length)
            {
                attackIndex++;

                CurrentAttack = attacks[attackIndex];

                isAnimationFinished = false;
            }
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            damageDetails.damageAmount = CurrentAttack.damage;
            damageDetails.stunDamageAmount = CurrentAttack.stunAmount;

            Collider2D[] cols = Physics2D.OverlapCircleAll(EntityAI.attackCheck.position, CurrentAttack.radius, EntityAI.entityData.whatIsTarget);

            foreach (Collider2D col in cols)
            {
                if ((targetsTag.Contains(col.tag) || targetsTag.Count == 0) 
                    && col.gameObject != EntityAI.gameObject && EntityAI.attackCheck.gameObject.activeSelf && !IsExitingState && !hasHittedTarget)
                {
                    hasHittedTarget = true;

                    Vector2 knockBackDirection = new Vector2() { x = CurrentAttack.knockBackDirection.x * EntityAI.FacingDirection, y = CurrentAttack.knockBackDirection.y };

                    EntityAI.SendDamage(damageDetails, col.gameObject, CurrentAttack.knockBackLevel, CurrentAttack.knockBackDuration, knockBackDirection);
                }
            }
        }

        public override void AnimationTrigger2()
        {
            base.AnimationTrigger2();

            if (CurrentAttack.movementSpeed > 0 && CurrentAttack.movementDuration > 0)
            {
                movementTimer = 0f;

                if (flipToTarget)
                {
                    Transform target = EntityAI.GetFirstTargetTransform();

                    if (target)
                    {
                        moveDirection = (target.position - EntityAI.transform.position).normalized;
                        EntityAI.FlipToTarget(target.position, true);
                    }
                }
                else
                {
                    moveDirection = Vector2.right * EntityAI.FacingDirection;
                }

                move = true;
            }
        }

        public override void DoChecks()
        {
            base.DoChecks();
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.Flying = entityFlying;

            hasHittedTarget = false;
            attackIndex = 0;

            foreach (Attack a in attacks)
            {
                if (a.Animation.Name.Length == 0)
                    Debug.LogError("Some attack animations is not set. This state depends of animations to work.");

                a.HasFinished = false;
            }

            EntityAI.SetVelocityX(0f);

            CurrentAttack = attacks[0];

            EntityAI.FlipToTarget();

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            _playDamagedAnim = PlayDamagedAnim;

            EntityAI.EntityDelegates.OnAttacksStart?.Invoke(EntityAI);
        }

        public override void Exit()
        {
            base.Exit();

            entityFlying = false;

            PlayDamagedAnim = _playDamagedAnim;

            EntityAI.EntityDelegates.OnAttacksEnd?.Invoke(EntityAI);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (attacks.Length == 0)
            {
                Debug.LogError("Attack list is empty");
                return;
            }

            if (isAnimationFinished)
            {
                if (NextState1 != null && attacks[attacks.Length - 1].HasFinished && !NextState1.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState1);
                    return;
                }
            }
            else if (Time.time >= startTime + delayBeforeStart)
            {
                EntityAI.PlayAnim(CurrentAttack.Animation);
                EntityAI.PlayAudio(CurrentAttack.Animation.SoundAsset);
            }

            if (EntityAI.IsDamaged)
            {
                if (CurrentAttack.canBeStopped)
                {
                    PlayDamagedAnim = _playDamagedAnim;

                    if (NextState2 != null && !NextState2.IsInCooldown)
                    {
                        StateMachine.ChangeState(NextState2);
                        return;
                    }
                }
                else
                {
                    PlayDamagedAnim = false;
                }
            }
            else
            {
                PlayDamagedAnim = false;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (move)
            {
                movementTimer += Time.deltaTime;

                if (movementTimer < CurrentAttack.movementDuration)
                {
                    if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                    {
                        EntityAI.SetVelocityX(CurrentAttack.movementSpeed * EntityAI.FacingDirection);
                    }
                    else if (EntityAI.Flying || EntityAI.entityData.gameType == D_Entity.GameType.Topdown2D)
                    {
                        EntityAI.SetVelocity(moveDirection * CurrentAttack.movementSpeed);
                    }
                }
                else
                {
                    move = false;
                    EntityAI.SetVelocity(Vector2.zero);
                }
            }
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
            base.DrawGizmosDebug();

            ExtDebug.DrawEllipse(EntityAI.attackCheck.transform.position, CurrentAttack.radius, CurrentAttack.radius, 32, Color.red);
        }
#endif
    }

    [System.Serializable]
    public class Attack
    {
        public EntityAnimation Animation;

        [Tooltip("If checked, the state will be interrupted when entity receive damage.")]
        public bool canBeStopped;

        [Tooltip("Use AnimationTrigger1 function to set when the damage will happen.")]
        [Min(0f)]
        public float damage;

        [Min(0)]
        public int stunAmount;

        public int knockBackLevel = 4;

        [Range(0f, 1f)]
        public float knockBackDuration = 0.2f;

        public Vector2 knockBackDirection = Vector2.right;

        [Tooltip("Radius damage detection. See the red circle when the animation starts.")]
        [Min(0f)]
        public float radius;

        [Tooltip("For 2D platformer the entity will move to the facing direction, for 2D topdown the entity will move in the target direction." +
            "Use AnimationTrigger2 function to set when to move.")]
        [Min(0f)]
        public float movementSpeed;

        [Min(0f)]
        public float movementDuration;

        public bool HasFinished { get; set; }
    }
}