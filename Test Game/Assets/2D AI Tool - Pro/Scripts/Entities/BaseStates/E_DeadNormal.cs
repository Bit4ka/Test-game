using UnityEngine;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Others/Dead Normal State")]
    public class E_DeadNormal : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation DeadAnimation;

        [SerializeField]
        [Tooltip("This will be the layer of the entity when the states starts. Suggestion: Create a layer called 'Dead' and assign here, set this layer to collide only with the ground.")]
        [Layer]
        private int DeadLayer;

        [SerializeField]
        [Tooltip("If checked, the entity will be destroyed or added to the pool after the AnimationFinish event call.")]
        private bool WaitForAnimationFinish;

        [SerializeField]
        [Tooltip("If false the entity will be added to an entity object pool.")]
        private bool DestroyEntity;

        [SerializeField]
        [Tooltip("If true the entity will remain enabled until an external script handles it.")]
        [HideIf(nameof(DestroyEntity), true)]
        private bool HandleByExternal;

        [SerializeField]
        [Tooltip("This prefab will spawn when the state is started at the entity position.")]
        private GameObject SpawnOnStateEnter;

        [SerializeField]
        private int ScorePointsValue = 10;

        [EndGroup]
        [SerializeField]
        [Tooltip("If checked the entity velocity will be zero, meaning it will just stops where they are. If not, the gravity will act into the entity.")]
        private bool entityFlying;

        #endregion

        private LayerMask originalLayer;

        public override void AnimationFinish()
        {
            base.AnimationFinish();

            if (WaitForAnimationFinish)
            {
                if (DestroyEntity)
                    Destroy(EntityAI.gameObject);
                else
                {
                    if (EntityPoolSingleton.Instance && !HandleByExternal)
                    {
                        EntityPoolSingleton.Instance.AddToPool(EntityAI.gameObject);
                    }
                    else if(!HandleByExternal)
                    {
                        Debug.LogWarning("EntityPoolSingleton.Instance is null, please add a gameobject with EntityPoolSingleton on the scene if you want to use pooling.");
                        EntityAI.gameObject.SetActive(false);
                    }
                }
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
        }

        public override void Enter()
        {
            base.Enter();

            EntityAI.Update();

            EntityAI.BlockAnim = false;

            EntityAI.PlayAnim(DeadAnimation);
            EntityAI.PlayAudio(DeadAnimation.SoundAsset);

            EntityAI.PacifyEntity(true);
            EntityAI.EntityDelegates.OnTargetNotDetected?.Invoke(EntityAI);

            EntityAI.Flying = entityFlying;
            EntityAI.Rb.gravityScale = entityFlying ? 0 : EntityAI.Rb.gravityScale;

            originalLayer = EntityAI.gameObject.layer;

            EntityAI.gameObject.layer = DeadLayer;

            if (SpawnOnStateEnter != null)
            {
                Instantiate(SpawnOnStateEnter, EntityAI.transform.position, SpawnOnStateEnter.transform.rotation);
            }

            if (DestroyEntity && !WaitForAnimationFinish)
            {
                Destroy(EntityAI.gameObject);
            }

            if (!WaitForAnimationFinish && !DestroyEntity)
            {
                if (EntityPoolSingleton.Instance && !HandleByExternal)
                {
                    EntityPoolSingleton.Instance.AddToPool(EntityAI.gameObject);
                }
                else if(!HandleByExternal)
                {
                    Debug.LogWarning("EntityPoolSingleton.Instance is null, please add a gameobject with EntityPoolSingleton on the scene if you want to use pooling.");
                    EntityAI.gameObject.SetActive(false);
                }
            }

            EntityAI.EntityDelegates.OnEntityDead?.Invoke(EntityAI, ScorePointsValue);
        }

        public override void Exit()
        {
            EntityAI.gameObject.layer = originalLayer;
            EntityAI.Flying = false;
        }

        public override void LogicUpdate()
        {
            EntityAI.PlayAnim(DeadAnimation);
            EntityAI.PlayAudio(DeadAnimation.SoundAsset);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (EntityAI != null)
            {
                if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D && !EntityAI.Flying)
                    EntityAI.SetVelocityX(0f);
                else if (EntityAI.Flying || EntityAI.entityData.gameType == D_Entity.GameType.Topdown2D)
                    EntityAI.SetVelocity(Vector2.zero);
            }
        }

    }
}