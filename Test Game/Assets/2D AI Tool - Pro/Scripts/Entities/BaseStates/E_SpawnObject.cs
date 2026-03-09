using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Others/Spawn Object State")]
    public class E_SpawnObject : EntityState
    {
        #region Variables

        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation SpawnAnimation;

        [SerializeField]
        [HideIf(nameof(delayBetweenSpawns), 0f)]
        private EntityAnimation IdleAnimation;

        [SerializeField]
        
        private GameObject prefab;

        [Min(1)]
        [SerializeField]
        [Tooltip("The amount of items that will be spawn.")]
        private int amountOfSpawns = 1;

        [Min(0)]
        [SerializeField]
        [Tooltip("The delay between each spawn.")]
        private float delayBetweenSpawns = 1f;

        [SerializeField]
        [Tooltip("The prefab will be spawned on the selected transform position.")]
        private PositionTransform positionTransform;

        [SerializeField]
        [ShowIf(nameof(positionTransform), PositionTransform.Specific)]
        private string transformName;

        [SerializeField]
        [Tooltip("If checked the prefab will only spawn when AnimationTrigger1 is called. If not, it will spawn on state enter.")]
        private bool waitForTrigger1 = true;

        [EndGroup]
        [Min(0.0f)]
        [SerializeField]
        private float delayToExitState = 0;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [Header("If objects spawned")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        private EntityState NextState1;

        [Header("If NextState1 in cooldown.")]
        [SerializeField]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        [EndGroup]
        private EntityState NextState2;

        #endregion

        private int spawnCounter = 1;

        private float delayExit, delaySpawn;

        private bool canSpawn;

        private Transform specificTransform;

        public override void AnimationFinish()
        {
            base.AnimationFinish();

            if (waitForTrigger1)
            {
                canSpawn = true;

                EntityAI.PlayAnim(IdleAnimation);
                EntityAI.PlayAudio(IdleAnimation.SoundAsset);
            }
        }

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            if (waitForTrigger1)
            {
                SpawnObject();
            }
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

            canSpawn = true;

            NextState1 = CheckState(NextState1);
            NextState2 = CheckState(NextState2);

            if (positionTransform == PositionTransform.Specific && specificTransform == null)
            {
                if (transformName.Length > 0)
                    specificTransform = GameObject.Find(transformName).transform;
            }

            spawnCounter = 1;
            delayExit = 0;
            delaySpawn = 0;

            if (waitForTrigger1)
            {
                canSpawn = false;
                EntityAI.PlayAnim(SpawnAnimation);
                EntityAI.PlayAudio(SpawnAnimation.SoundAsset);
            }
            else
            {
                EntityAI.PlayAnim(IdleAnimation);
                EntityAI.PlayAudio(IdleAnimation.SoundAsset);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (spawnCounter > amountOfSpawns)
            {
                if (waitForTrigger1)
                {
                    if(canSpawn)
                        delayExit += Time.deltaTime;
                }
                else
                {
                    delayExit += Time.deltaTime;
                }
                
                if (delayExit >= delayToExitState)
                {
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
            else
            {
                if (canSpawn)
                {
                    delaySpawn += Time.deltaTime;

                    if(delaySpawn >= delayBetweenSpawns)
                    {
                        if (!waitForTrigger1)
                        {
                            SpawnObject();
                            delaySpawn = 0f;
                        }
                        else
                        {
                            canSpawn = false;
                            delaySpawn = 0f;
                        }

                        EntityAI.PlayAnim(SpawnAnimation);
                        EntityAI.PlayAudio(SpawnAnimation.SoundAsset);
                    }
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (EntityAI.entityData.gameType == D_Entity.GameType.Platformer2D)
                EntityAI.SetVelocityX(0f);
            else
                EntityAI.SetVelocity(Vector2.zero);
        }

        private void SpawnObject()
        {
            if (positionTransform == PositionTransform.Specific)
            {
                Instantiate(prefab, specificTransform.position, prefab.transform.rotation);
            }
            else
            {
                switch (positionTransform)
                {
                    case PositionTransform.entity:
                        Instantiate(prefab, EntityAI.transform.position, prefab.transform.rotation);
                        break;
                    case PositionTransform.AttackCheck:
                        Instantiate(prefab, EntityAI.attackCheck.position, prefab.transform.rotation);
                        break;
                    case PositionTransform.TargetCheck:
                        Instantiate(prefab, EntityAI.targetCheck.position, prefab.transform.rotation);
                        break;
                    case PositionTransform.LedgeCheck:
                        Instantiate(prefab, EntityAI.ledgeCheck.position, prefab.transform.rotation);
                        break;
                    case PositionTransform.WallCheck:
                        Instantiate(prefab, EntityAI.wallCheck.position, prefab.transform.rotation);
                        break;
                    default:
                        break;
                }
            }

            spawnCounter++;
        }




        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);
            NextState2 = GetFromPort("NextState2", port, NextState2);

            return base.GetValue(port);
        }

        public enum PositionTransform
        {
            entity,
            AttackCheck,
            TargetCheck,
            LedgeCheck,
            WallCheck,
            Specific,
        }
    }
}