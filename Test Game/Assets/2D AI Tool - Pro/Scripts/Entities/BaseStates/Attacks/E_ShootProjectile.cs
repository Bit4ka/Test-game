using MaykerStudio;
using MaykerStudio.Types;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/Shoot Projectile State")]
    public class E_ShootProjectile : EntityState
    {
        #region Variables
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation shootAnimation;

        [SerializeField]
        [Tooltip("This animation play between the shoots.")]
        private EntityAnimation IdleAnimation;

        [SerializeField]
        private DetectionType detectionType;

        [SerializeField]
        private float TargetMaxDistance = 15f;

        [SerializeField]
        [Range(10f, 270f)]
        [ShowIf(nameof(detectionType), DetectionType.FOV)]
        private float FOV = 15;

        [SerializeField]
        [Tooltip("The name of the transform will hold all the projectiles of the entities. SUGGESTION: choose a empty gameobject to hold the projectiles.")]
        private string objectPoolHolder;

        [SerializeField]
        
        [Tooltip("The spawn position of the prefab will be the attack check position.")]
        private GameObject projectilePrefab;

        [Tooltip("If it is checked, the projectile will fall instead of goes to target. This is good for entities that drop bombs from the sky or is an entity trap like.")]
        [SerializeField]
        private bool dropProjectile = false;

        [Tooltip("If it is checked, the state will shot the first projectile as soon as the state enters. If not, the state will wait the "+nameof(delayBetweenShoots))]
        [SerializeField]
        private bool instantFirstShoot = false;

        [SerializeField]
        [Tooltip("If checked the direction of the projectile will be the entity's facing direction instead of the target's directio. Only works for 2D Platformer.")]
        private bool straightShoot = false;

        [SerializeField]
        [Min(0.1f)]
        private float ProjectileSpeed = 5f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("The distance of the projectile can travel to before get back to the object pool.")]
        private float maxTravelDistance = 15f;

        [SerializeField]
        [Min(0.0f)]
        private float ProjectileDamage = 10f;

        [SerializeField]
        [Min(0)]
        private int ProjectileStunAmount = 10;

        [SerializeField]
        [Min(0.0f)]
        private float delayBetweenShoots = 1f;

        [SerializeField]
        [Min(0.0f)]
        private float delayToExitState = 1f;
        
        [SerializeField]
        [Min(1)]
        private int maxShootsInState = 1;

        [EndGroup]
        [SerializeField]
        private bool EntityFlying;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If maxShots achieve || targets out of range")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState1;

        [Header("If NextState1 in cooldown")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        private EntityState NextState2;

        [Header("If Target Out of Range")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [EndGroup]
        private EntityState NextState3;

        #endregion

        private float delayCounter, delayExitCounter;

        private int shoots;

        private bool _playDamagedAnim, IsGrounded, targetInRange, IsShooting;

        private Transform holderTransform;

        private Transform Target;

        private Vector2 targetPos;

        public override void AnimationFinish()
        {
            base.AnimationFinish();

            if (PlayDamagedAnim)
                return;

            IsShooting = false;

            delayCounter = 0f;
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            EntityAI.FlipToTarget(targetPos, true);

            ShootProjectile();
        }

        public override void AnimationTrigger2()
        {
            base.AnimationTrigger2();
        }

        public override void DoChecks()
        {
            base.DoChecks();

            IsGrounded = EntityAI.CheckGround();

            switch (detectionType)
            {
                case DetectionType.Circle:
                    targetInRange = EntityAI.CheckTargetsInRadius(TargetMaxDistance);
                    break;
                case DetectionType.Ray:
                    targetInRange = EntityAI.CheckTargetsInRange(TargetMaxDistance);
                    break;
                case DetectionType.FOV:
                    targetInRange = EntityAI.CheckTargetsInFieldOfView(FOV, TargetMaxDistance);
                    break;
                case DetectionType.Box:
                    targetInRange = EntityAI.CheckBox();
                    break;
                default:
                    break;
            }
        }

        public override void Enter()
        {
            base.Enter();

            if (holderTransform == null && objectPoolHolder.Length > 0)
                holderTransform = GameObject.Find(objectPoolHolder).transform;

            Target = EntityAI.GetFirstTargetTransform(TargetMaxDistance);

            shoots = 1;

            if(Target)
                EntityAI.FlipToTarget(Target.position, true);

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);
            NextState3 = CheckState(NextState3);

            _playDamagedAnim = PlayDamagedAnim;

            if (shootAnimation.Name.Length == 0)
            {
                Debug.LogError("The shoot animations is not set. This state depends on animations events to work. Entity: " + EntityAI.name);
            }

            if (IdleAnimation.Name.Length == 0)
            {
                Debug.LogError("The Idle animations is not set. This state depends on animations events to work. Entity: " + EntityAI.name);
            }

            if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                EntityAI.SetVelocityX(0f);
            }
            else
            {
                EntityAI.SetVelocity(Vector2.zero);
            }

            EntityAI.Flying = EntityFlying;

            EntityAI.PlayAnim(IdleAnimation);
            EntityAI.PlayAudio(IdleAnimation.SoundAsset);

            if (instantFirstShoot)
            {
                InitShoot();
            }
        }

        public override void Exit()
        {
            base.Exit();

            delayCounter = 0f;

            PlayDamagedAnim = _playDamagedAnim;

            EntityAI.Flying = false;

            IsShooting = false;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            #region Animation playing

            if (IsGrounded || EntityAI.entityData.gameType == D_Entity.GameType.Topdown2D || EntityAI.Flying)
            {
                delayCounter += Time.deltaTime;
                if (delayCounter >= delayBetweenShoots && shoots <= maxShootsInState && !IsShooting)
                {
                    InitShoot();
                }

                else if (isAnimationFinished && !IsShooting)
                {
                    PlayDamagedAnim = _playDamagedAnim;

                    EntityAI.PlayAnim(IdleAnimation);
                    EntityAI.PlayAudio(IdleAnimation.SoundAsset);

                    isAnimationFinished = false;
                }
                else if(IsShooting && EntityAI.CurrentPlayingAnimation != shootAnimation)
                {
                    EntityAI.PlayAnim(shootAnimation);
                    EntityAI.PlayAudio(shootAnimation.SoundAsset);
                }

                #region Transition

                if (shoots > maxShootsInState && !IsShooting || !targetInRange)
                {
                    delayExitCounter += Time.deltaTime;

                    if (delayExitCounter >= delayToExitState)
                    {
                        if (NextState1 != null && !NextState1.IsInCooldown)
                            StateMachine.ChangeState(NextState1);
                        else if (NextState2 != null && !NextState2.IsInCooldown)
                            StateMachine.ChangeState(NextState2);

                        delayExitCounter = 0;
                        shoots = 0;
                    }
                }

                #endregion
            }

            #endregion
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
        }

        private void InitShoot()
        {
            PlayDamagedAnim = false;
            EntityAI.BlockAnim = false;

            if (Target == null)
            {
                if(NextState3 != null)
                    StateMachine.ChangeState(NextState3);

                shoots = maxShootsInState + 1;

                return;
            }
            else
            {
                IsShooting = true;

                targetPos = Target.position;

                EntityAI.FlipToTarget(targetPos, true);

                EntityAI.PlayAnim(shootAnimation);
                EntityAI.PlayAudio(shootAnimation.SoundAsset);
            }
        }

        private void ShootProjectile()
        {
            GameObject projectile = EntityAI.ObjectPool.Get(projectilePrefab, EntityAI.attackCheck.position, holderTransform);

            if(projectile.TryGetComponent(out Projectile projScript))
            {
                projScript.ObjectPool = EntityAI.ObjectPool;

                Vector2 dir;

                if (!straightShoot)
                    dir = (targetPos - (Vector2)EntityAI.attackCheck.position);
                else
                    dir = Vector2.right * EntityAI.FacingDirection;

                if (dropProjectile)
                    dir = Vector2.down;

                projScript.FireProjectile(ProjectileSpeed, dir.normalized, maxTravelDistance, ProjectileDamage, ProjectileStunAmount);

                shoots++;
            }
            else
            {
                Debug.LogError("Cannot fire projectile. The Gameobject: " + projectile.name + " doesn't contain a Projectile component attached.");
            }
            
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);
            NextState3 = GetFromPort("NextState3", port, NextState3);

            return base.GetValue(port);
        }

#if UNITY_EDITOR
        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();

            if (delayCounter >= delayBetweenShoots && shoots <= maxShootsInState && targetPos != Vector2.zero)
                Debug.DrawLine(EntityAI.transform.position, targetPos, Color.red);
            else if (targetPos != Vector2.zero)
                Debug.DrawLine(EntityAI.transform.position, targetPos, Color.yellow);

            ExtDebug.DrawDetectionType(EntityAI, detectionType, FOV, TargetMaxDistance, Color.blue);
        }
#endif
    }
}