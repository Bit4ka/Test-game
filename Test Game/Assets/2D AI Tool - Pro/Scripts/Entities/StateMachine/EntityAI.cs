using MaykerStudio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI2DTool
{
    public class EntityAI : Entity
    {
        public bool enableDebug = true;

        [Tooltip("Check this if you manually place an entity prefab instance on the scene and wants to use pooling. Make sure the name of the prefab is the same as the original prefab, remove all the '(n)' and '(clone') from the name.")]
        public bool usePooling;

        [ShowIf(nameof(usePooling), true)]
        [NotNull("Assign a entity pool")]
        public EntityObjectPool entityPool;

        public int initialFacingDirection;

        public EntityState FirstState { get; private set; }
        public EntityState DeadState { get; private set; }
        public EntityState StunState { get; private set; }
        public Dictionary<int, EntityState> States { get; private set; }

        public EntityObjectPool ObjectPool { get; private set; }

        [NotNull("EntityGraph cannot be null")]
        public EntityGraph entityGraph;

        [Disable]
        public EntityState currentState;

        [ProgressBar("Health %", 0f, 100f, HexColor = "#FF0000")]
        [SerializeField]
        private float InspectorHealth;

        [ProgressBar("Aggro %", 0f, 100f, HexColor = "#00FF00")]
        [SerializeField]
        private float InspectorAggro;

        [ProgressBar("Stun Gauge", 0f, 100f, HexColor = "#d6883a")]
        [SerializeField]
        private float InspectorStunGauge;

        #region Event-Related

        private readonly List<OnEventNodeBase> OnEventsNodesList = new List<OnEventNodeBase>();

        private delegate void ObjectsCloseAction(params object[] arg);

        private ObjectsCloseAction OnObjectCloses;

        private delegate void PlayerInputAction(params object[] arg);

        private PlayerInputAction OnPlayerInput;

        #endregion

        public override void Start()
        {
            base.Start();

            if (ObjectPool == null)
            {
                if(TryGetComponent(out EntityObjectPool pool))
                    Destroy(pool);

                ObjectPool = gameObject.AddComponent<EntityObjectPool>();
            }

            if (States == null)
                States = new Dictionary<int, EntityState>();

            FirstState = entityGraph.GetFirstState();

            DeadState = entityGraph.GetDeadState();

            StunState = entityGraph.GetStunState();

            if (!CheckSpecialNodesReferences())
                return;

            CheckOnEventNodes();

            FacingDirection = initialFacingDirection;

            FirstState = GetState(FirstState);
            DeadState = GetState(DeadState);
            StunState = GetState(StunState);

            StateMachine.Initialize(FirstState);

            if (usePooling)
            {
                if (entityPool != null)
                {
                    entityPool.AddKey(gameObject);
                }
                else
                {
                    Debug.LogWarning("Please, assign a entity pool.");
                }
            }
        }

        public override void Update()
        {
#if UNITY_EDITOR
            InspectorHealth = (CurrentHealth / MaxHealth) * 100f;
            InspectorAggro = CurrentAggroValue;
            if (entityData.stunResistance > 0)
                InspectorStunGauge = (CurrentStunResistance / entityData.stunResistance) * 100f;
#endif

            if (entityGraph.nodes.Count == 0)
            {
                Debug.LogError("There's no states on " + gameObject.name);

                return;
            }

            base.Update();

            currentState = StateMachine.CurrentState;

#if UNITY_EDITOR
            if (enableDebug)
            {
                currentState.DrawGizmosDebug();
            }
#endif

            if (currentState == DeadState && !IsDead)
            {
                IsDead = true;
            }
        }

        /// <summary>
        /// This function duplicates the original scriptable object and put the copy inside a dict, so every instance of an entity can use the same scriptable object but with different calculations.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public EntityState GetState(EntityState state)
        {
            if (state == null)
                return null;

            if (States.TryGetValue(state.GetInstanceID(), out EntityState st))
            {
                return st;
            }

            st = Instantiate(state);

            st.InitState(this);

            st.AlreadyExists = true;
            States.Add(state.GetInstanceID(), st);

            return st;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (!EntityDelegates)
            {
                TryGetComponent(out EntityDelegates entityDelegates);
                this.EntityDelegates = entityDelegates;
            }

            if (FirstState != null && StateMachine != null)
                ResetEntity();

            if (EntityDelegates != null)
            {
                CheckOnEventNodes();
            }
        }

        public override void ResetEntity()
        {
            base.ResetEntity();

            StateMachine.ChangeState(FirstState);
        }

        public override void Damage(DamageDetails damageDetails)
        {
            base.Damage(damageDetails);

            if (IsDead)
            {
                if (StateMachine.CurrentState != DeadState)
                    StateMachine.ChangeState(DeadState);
            }
        }

        protected override void OnStun()
        {
            if(!StateMachine.CurrentState.CanStopStun)
                base.OnStun();

            if (IsStunned)
            {
                if (StunState != null && !StunState.IsInCooldown && currentState != StunState)
                {
                    StateMachine.ChangeState(StunState);
                    EntityDelegates.OnEntityStunned?.Invoke(this);
                }
            }
        }

#if UNITY_EDITOR
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Vector3 originTweak = entityData.originOffset;
            originTweak.x *= transform.right.x;

            //Debug for the aggro box 2D
            ExtDebug.BoxCast(transform.position + originTweak, entityData.boxDetectionSize, entityData.boxDetectionAngle, Vector2.zero,
                0, entityData.whatIsTarget);
        }
#endif
        public void InvokeOnObjectClose(GameObject obj)
        {
            OnObjectCloses?.Invoke(obj);
        }

        public void InvokeOnPlayerInput(params object[] args)
        {
            OnPlayerInput?.Invoke(args);
        }

        #region Private Methods

        private void CheckOnEventNodes()
        {
            if (OnEventsNodesList.Count == 0)
            {
                List<XNode.Node> OnEventsNodesList = entityGraph.GetOnEventNodes();

                for (int i = 0; i < OnEventsNodesList.Count; i++)
                {
                    OnEventNodeBase node = OnEventsNodesList[i] as OnEventNodeBase;

                    node = Instantiate(node);

                    node.entityAI = this;

                    SubscribeOnEventNodeToEvents(node);

                    this.OnEventsNodesList.Add(node);
                }
            }
            else
            {
                for (int i = 0; i < OnEventsNodesList.Count; i++)
                {
                    OnEventNodeBase node = OnEventsNodesList[i];

                    SubscribeOnEventNodeToEvents(node);
                }
            }
        }

        private void SubscribeOnEventNodeToEvents(OnEventNodeBase node)
        {
            if (node.GetType() == typeof(OnEntityHealthNode) || node.GetType() == typeof(OnDamagedDirectionNode))
                EntityDelegates.OnDamageReceive += node.TryTransitionToNextState;

            else if (node.GetType() == typeof(OnObjectsCloseNode))
                OnObjectCloses += node.TryTransitionToNextState;

            else if (node.GetType() == typeof(OnPlayerInputNode))
                OnPlayerInput += node.TryTransitionToNextState;
        }

        private void UnSubscribeOnEventNodeToEvents(OnEventNodeBase node)
        {
            if (node.GetType() == typeof(OnEntityHealthNode) || node.GetType() == typeof(OnDamagedDirectionNode))
                EntityDelegates.OnDamageReceive -= node.TryTransitionToNextState;

            else if (node.GetType() == typeof(OnObjectsCloseNode))
                OnObjectCloses -= node.TryTransitionToNextState;

            else if (node.GetType() == typeof(OnPlayerInputNode))
                OnPlayerInput -= node.TryTransitionToNextState;
        }

        private void OnParticleCollision(GameObject other)
        {
            //If you want to Damage entity with particle system, but keep in mind that the function needs a DamageDetails struct you can't send a null.]
            //if(other.TryGetComponent(out AttackDetailsHolder detailsHolder))
            //{
            //    Damage(detailsHolder.details);
            //}
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            //You can damage the entity if the trigger collider layer is inside the entityData.whatIsTarget, in short, if the trigger's collider is a target.
            //This only works if both entity and the other object can collide with each other

            //if ((entityData.whatIsTarget & 1 << collision.gameObject.layer) != 0)
            //{
            //    if (collision.gameObject.TryGetComponent(out AttackDetailsHolder detailsH))
            //    {
            //        DamageDetails details = detailsH.details;

            //        Damage(details);
            //    }
            //}
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private void OnDisable()
        {
            if (States == null)
                return;

            FacingDirection = initialFacingDirection;

            IEnumerable<EntityState> states = States.Values.Where(s => s.IsInCooldown);

            foreach (EntityState item in states)
            {
                item.IsInCooldown = false;
            }

            for (int i = 0; i < OnEventsNodesList.Count; i++)
            {
                OnEventNodeBase node = OnEventsNodesList[i];

                UnSubscribeOnEventNodeToEvents(node);

                node.HasTransitionOnce = false;
            }
        }

        private bool CheckSpecialNodesReferences()
        {
            if (FirstState == null)
            {
                Debug.LogError(name + ": First state is not set.");
                return false;
            }
            if (DeadState == null)
            {
                Debug.LogError(name + ": Dead State is not set.");
                return false;
            }

            if (StunState == null && entityData.stunResistance > 0)
            {
                Debug.LogWarning(name + ": Stun State is not set.");
            }

            return true;
        }

        #endregion
    }
}