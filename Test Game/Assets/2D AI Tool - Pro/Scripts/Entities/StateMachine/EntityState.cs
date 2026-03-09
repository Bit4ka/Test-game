using MaykerStudio;
using System;
using System.Collections;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [NodeTint(41, 83, 143, 120)]
    [NodeWidth(350)]
    [CreateNodeMenu("")]
    public class EntityState : Node
    {
        [Input]
        [Disable]
        [Tooltip("This is input port that will return to this class to a output port. This variable should always be public and serializable.")]
        public EntityState In;

        [BeginGroup("Base Variables")]
        public bool CanSeeThroughWalls;

        [Tooltip("When this is checked the entity will play the damaged anim when it receive damage, some states will only play this animation when it can be stopped, like an attack with 'can be stoped' checked. ")]
        public bool PlayDamagedAnim;

        [Tooltip("If true, this state can prevent the entity to get stunned when the stun gauge is full.")]
        public bool CanStopStun;

        [ShowIf("PlayDamagedAnim", true)]
        [Tooltip("This animation will play when the entity is damaged if the " + nameof(PlayDamagedAnim) + " is checked.")]
        public EntityAnimation DamagedAnimation;


        [Min(0.0f)]
        [Tooltip("The others states cannot transition to this state when in cooldown. This is the value in seconds.")]
        public float stateCooldown;

        [Min(0.0f)]
        [Tooltip("If the value is greater than 0 the state will transition to 'TimerIsOver' if not null and not in cooldown. The duration doesn't reset on state exit or enter")]
        public float stateDuration;

        [Tooltip("Check this if you want to reset the current duration to zero when exit state, meaning, this not resets the 'State Duration' but the current duration of the state.")]
        [HideIf(nameof(stateDuration), 0f)]
        public bool resetDurationOnExit;

        [BeginGroup]
        [Tooltip("Should the detection use an 'Aggro Bar fill' instead of instant detect targets?")]
        public bool UseAggroFill;

        [Min(0.0f)]
        [Tooltip("How long the aggro will built from 0 to 100.")]
        [ShowIf(nameof(UseAggroFill), true)]
        public float FillDuration = 2f;

        [Min(0f)]
        [Tooltip("How long the aggro will decrease from 100 to 0.")]
        [ShowIf(nameof(UseAggroFill), true)]
        public float DecreaseDuration = 4f;

        [Min(1f)]
        [Tooltip("The distance in units a target needs to be to double the aggro fill speed. Hint: You can use a value that's half of the current detection radius.")]
        [ShowIf(nameof(UseAggroFill), true)]
        public float MinDistanceToMultiplyAggro = 5f;

        [EndGroup]
        [Min(1f)]
        [Tooltip("The multiplier to speed up the Aggro fill when in min distance.")]
        [ShowIf(nameof(UseAggroFill), true)]
        public float AggroMultiplier = 2f;

        [EndGroup]
        [Disable]
        [Header("If time is over")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        public EntityState TimeIsOver;

        public bool AlreadyExists { get; set; }
        public bool IsInCooldown { get; set; }
        public Coroutine CooldownCoroutine { get; set; }
        protected bool IsExitingState { get; private set; }
        public FiniteStateMachine StateMachine { get; set; }
        public EntityAI EntityAI { get; set; }

        protected float startTime;

        protected float currentStateDuration;

        protected bool isAnimationFinished;

        protected DamageDetails damageDetails;

        protected override void Init()
        {
            In = this;

            foreach (NodePort port in Ports)
            {
                port.GetOutputValue();
            }
        }

        public virtual void InitState(EntityAI entityAI)
        {
            EntityAI = entityAI;
            StateMachine = EntityAI.StateMachine;
            damageDetails.sender = entityAI.gameObject;
        }

        public EntityState CheckState(EntityState state)
        {
            if (state != null)
            {
                if (!state.AlreadyExists)
                    return EntityAI.GetState(state);
                else
                {
                    return state;
                }
            }
            else
            {
                return null;
            }
        }

        public virtual void AnimationTrigger1() { }

        public virtual void AnimationTrigger2() { }

        public virtual void AnimationFinish()
        {
            if (!EntityAI.BlockAnim)
            {
                isAnimationFinished = true;
            }
        }

        public virtual void DoChecks() { }

        public virtual void Enter()
        {
            TimeIsOver = CheckState(TimeIsOver);

            IsExitingState = false;
            isAnimationFinished = false;

            startTime = Time.time;

            EntityAI.SeeThroughWalls = CanSeeThroughWalls;

            DoChecks();
        }

        public virtual void Exit()
        {
            EntityAI.BlockAnim = false;

            IsExitingState = true;

            EntityAI.SeeThroughWalls = false;

            EntityAI.StopAudio();

            EntityAI.SetVelocity(Vector2.zero);

            if (stateCooldown > 0)
                CooldownCoroutine = EntityAI.StartCoroutine(CooldownCounter(stateCooldown));

            if (resetDurationOnExit)
                currentStateDuration = 0;
        }

        public virtual void LogicUpdate()
        {
            damageDetails.position = EntityAI.transform.position;

            if (stateDuration > 0)
            {
                currentStateDuration += Time.deltaTime;

                if (currentStateDuration >= stateDuration)
                {
                    if (TimeIsOver != null && !TimeIsOver.IsInCooldown)
                    {
                        StateMachine.ChangeState(TimeIsOver);
                        return;
                    }
                }
            }
        }

        public virtual void PhysicsUpdate()
        {
            DoChecks();
        }

        public virtual IEnumerator CooldownCounter(float amount)
        {
            IsInCooldown = true;

            yield return new WaitForSeconds(amount);

            IsInCooldown = false;

            if(EntityAI && EntityAI.enableDebug)
                Debug.Log(name + " cooldown has ended.");
        }

        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            from.GetOutputValue();
        }

        public override void OnRemoveConnection(NodePort port)
        {
            port.GetOutputValue();
        }

        public virtual void DrawGizmosDebug() { }

        public override object GetValue(NodePort port)
        {
            TimeIsOver = GetFromPort("TimeIsOver", port, TimeIsOver);

            return port.Connection?.node;
        }

        /// <summary>
        /// Required by all the transitions states, is used to return the value in the node editor when a new connection is made.
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="portWhoAsks"></param>
        /// <param name="currentState"></param>
        /// <returns>The reference of the assigned node in the Node editor or the currentState if the "field" required is not equals to the "field" name of the state.</returns>
        public EntityState GetFromPort(string stateName, NodePort portWhoAsks, EntityState currentState)
        {
            if (GetPort(portWhoAsks.fieldName) != null)
            {
                if (portWhoAsks.fieldName == stateName)
                {
                    return portWhoAsks.Connection?.node as EntityState;
                }
            }

            //If is not the port field or the port is empty just return the same value that the state send on "CurrentState"
            return currentState;
        }
    }
}