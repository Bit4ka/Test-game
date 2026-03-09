using MaykerStudio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI2DTool
{
    [RequireComponent(typeof(EntityDelegates), typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class Entity : MonoBehaviour, IDamageable
    {
        #region Public Variables

        public FiniteStateMachine StateMachine;

        public D_Entity entityData;

        public Rigidbody2D Rb { get; private set; }

        public Animator Anim { get; private set; }

        public Collider2D Collider { get; private set; }

        public Transform wallCheck;

        public Transform ledgeCheck;

        public Transform attackCheck;

        public Transform targetCheck;

        [Tooltip("This particles will play when the entity is damaged. Please, put all the particles as child of the entity.")]
        public ParticleSystem[] hitParticles;

        #endregion

        #region Private and protected variables

        private float lastDamageTime;
        private float damagedTimer;
        private float previousYVelocity;
        private float getTargetsDelayCounter;
        private float TargetAngle;
        private float PreviousAngle;
        private float lerpTime;

        private readonly string directionXName = "entity_directionX";
        private readonly string directionYName = "entity_directionY";
        private readonly string prevDirectionXName = "entity_prevDirectionX";
        private readonly string prevDirectionYName = "entity_prevDirectionY";
        private readonly string animSpeedName = "AnimSpeed";

        private bool RotateRigidbody;

        private Animator m_LastAnimatorCache;
        private readonly Dictionary<string, int> m_AnimatorParamCache = new Dictionary<string, int>();

        private Vector2 velocityWorkspace;
        private Vector2 velocityWorkspace2;
        private Vector2 movementDirection = Vector2.zero;
        private Vector2 prevMovementDirection = Vector2.zero;
        private ContactFilter2D groundContactFilter = new ContactFilter2D();
        private ContactFilter2D targetsContactFilter = new ContactFilter2D();

        private List<Collider2D> targetsBuffer = new List<Collider2D>();
        private Vector3 previousTargetPos;

        private AudioSource audioSource;

        #endregion

        #region Public Variables references
        public int FacingDirection { get; set; }
        public int DamageDirectionX { get; private set; }
        public bool BlockDamagedAnim { get; set; }
        public bool DisableAggroDecreasing { get; set; }
        public bool IsDead { get; protected set; }
        public bool IsDamaged { get; set; }
        public bool IsStunned { get; set; }
        public bool Flying { get; set; }
        public bool SeeThroughWalls { get; set; }
        public bool StopKnockBack { get; set; }
        public bool CanBeDamaged { get; set; }
        public bool BlockAnim { get; set; }
        public bool IgnoreFallClamp { get; set; }
        public bool IsInAggroState { get; protected set; }
        public bool IsPacifyState { get; protected set; }
        public bool IsKnockback { get; protected set; }
        public bool IsOnSlope { get; protected set; }
        public float OriginalGravityScale { get; private set; }
        public float CurrentHealth { get; protected set; }
        public float CurrentStunResistance { get; protected set; }
        public float CurrentAggroValue { get; protected set; }
        public float MaxHealth { get; protected set; }
        public RaycastHit2D LastGroundInfo { get; protected set; }
        public Vector2 DamageDirectionVector { get; private set; }
        public EntityDelegates EntityDelegates { get; protected set; }
        public EntityAnimation CurrentPlayingAnimation { get; private set; }
        public EntityAnimation PreviousAnimation { get; private set; }
        public Collider2D CurrentTargetaggroed { get; private set; }

        #endregion

        #region Unity callback functions

        public virtual void OnEnable()
        {
            if (FacingDirection == -1 && Mathf.Approximately(transform.eulerAngles.y, 0f) ||
                FacingDirection == 1 && Mathf.Approximately(transform.eulerAngles.y, 180f))
                transform.Rotate(0f, 180f, 0f);

            targetsContactFilter.SetLayerMask(entityData.whatIsTarget);

            getTargetsDelayCounter = 0f;

            GetAllTargets();
        }

        public virtual void Start()
        {
            CanBeDamaged = true;
            MaxHealth = entityData.maxHealth;
            CurrentHealth = MaxHealth;
            CurrentStunResistance = entityData.stunResistance;

            Rb = GetComponent<Rigidbody2D>();

            CanBeDamaged = true;
            OriginalGravityScale = Rb.gravityScale;

            Collider2D[] colliders = new Collider2D[10];

            Rb.GetAttachedColliders(colliders);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D c = colliders[i];

                if (c)
                {
                    if (!c.isTrigger)
                    {
                        Collider = c;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            TryGetComponent(out Animator a);
            TryGetComponent(out audioSource);
            TryGetComponent(out EntityDelegates ed);

            EntityDelegates = ed;

            SetupGroundCheck();

            targetsContactFilter.SetLayerMask(entityData.whatIsTarget);

            if (a == null)
            {
                if (GetComponentInChildren<Animator>() == null)
                {
                    Debug.LogError("No animator found in parent or children of " + gameObject.name);
                }
                else
                {
                    Anim = GetComponentInChildren<Animator>();
                    Anim.keepAnimatorStateOnDisable = true;
                }
            }
            else
            {
                Anim = a;
                Anim.keepAnimatorStateOnDisable = true;
            }

            StateMachine = new FiniteStateMachine();
        }

        public virtual void Update()
        {
            #region TopDown2D animator Parameters

            movementDirection = Rb.linearVelocity.normalized;

            if (movementDirection != Vector2.zero)
                prevMovementDirection = movementDirection;

            if (entityData.gameType == D_Entity.GameType.Topdown2D && !entityData.rotateTheTransform)
            {
                if (TryGetAnimatorParam(Anim, directionXName, out int Hash1)) Anim.SetFloat(Hash1, movementDirection.x);
                if (TryGetAnimatorParam(Anim, directionYName, out int Hash2)) Anim.SetFloat(Hash2, movementDirection.y);
                if (TryGetAnimatorParam(Anim, prevDirectionXName, out int Hash3)) Anim.SetFloat(Hash3, prevMovementDirection.x);
                if (TryGetAnimatorParam(Anim, prevDirectionYName, out int Hash4)) Anim.SetFloat(Hash4, prevMovementDirection.y);
            }

            #endregion

            if (IsDamaged)
            {
                damagedTimer += Time.deltaTime;
                if (damagedTimer > entityData.maxDamagedTimer)
                {
                    IsDamaged = false;
                    damagedTimer = 0;
                }
            }

            if (IsKnockback)
            {
                Rb.linearVelocity = velocityWorkspace;
                if (IsStunned)
                {
                    IsKnockback = false;

                    if (!StopKnockBack)
                        SetVelocityX(0f);
                }
            }

            if (Time.time >= lastDamageTime + entityData.stunRecoveryTime)
            {
                ResetStunResistance();
            }

            #region FallDamage calculator
            if (entityData.receiveFallDamage && entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                if (Rb.linearVelocity.y < -1f)
                {
                    previousYVelocity = Rb.linearVelocity.y;
                }
                else
                {
                    if (previousYVelocity <= entityData.fallYVelocity)
                    {
                        DamageDetails details = new DamageDetails
                        {
                            damageAmount = entityData.fallDamageAmount,
                            position = transform.position
                        };
                        Damage(details);

                        previousYVelocity = 0f;
                    }
                }
            }
            #endregion

            getTargetsDelayCounter += Time.unscaledDeltaTime;

            if (getTargetsDelayCounter >= 1)
            {
                GetAllTargets();
                getTargetsDelayCounter = 0;
            }

            StateMachine.CurrentState.LogicUpdate();
        }

        public virtual void FixedUpdate()
        {
            if (!Flying && entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                if (Rb.gravityScale != 0 && entityData.GravityScaleMultiplier > 1)
                {
                    if (Rb.gravityScale < 0 && Rb.linearVelocity.y > 1f || Rb.gravityScale > 0 && Rb.linearVelocity.y < - 1f)
                    {
                        Rb.gravityScale = OriginalGravityScale * entityData.GravityScaleMultiplier * (Rb.gravityScale > 0 ? 1f : -1f);
                    }
                    else
                    {
                        Rb.gravityScale = OriginalGravityScale * (Rb.gravityScale > 0 ? 1f : -1f);
                    }
                }
            }


            if (RotateRigidbody)
            {
                lerpTime += (entityData.rotationSpeed / 10f) * Time.fixedDeltaTime;

                if (!Mathf.Approximately(Rb.rotation, TargetAngle))
                {
                    Rb.SetRotation(Mathf.LerpAngle(PreviousAngle, TargetAngle, lerpTime));
                }
                else
                {
                    RotateRigidbody = false;
                    lerpTime = 0f;
                }
            }

            StateMachine.CurrentState.PhysicsUpdate();

            if (!IgnoreFallClamp && entityData.gameType == D_Entity.GameType.Platformer2D && !Flying)
            {
                if(Rb.gravityScale > 0)
                    velocityWorkspace2.Set(Rb.linearVelocity.x, Mathf.Clamp(Rb.linearVelocity.y, entityData.maxFallSpeed, Mathf.Infinity));
                else
                    velocityWorkspace2.Set(Rb.linearVelocity.x, Mathf.Clamp(Rb.linearVelocity.y, entityData.maxFallSpeed, -entityData.maxFallSpeed));

                Rb.linearVelocity = velocityWorkspace2;
            }
        }

        private void SetupGroundCheck()
        {
            groundContactFilter.useNormalAngle = true;
            groundContactFilter.useLayerMask = true;
            groundContactFilter.layerMask = entityData.whatIsObstacles;
            groundContactFilter.minNormalAngle = entityData.groundMinNormalAngle;
            groundContactFilter.maxNormalAngle = entityData.groundMaxNormalAngle;
        }

        private bool TryGetAnimatorParam(Animator animator, string paramName, out int hash)
        {
            if ((m_LastAnimatorCache == null || m_LastAnimatorCache != animator) && animator != null) // Rebuild cache
            {
                m_LastAnimatorCache = animator;
                m_AnimatorParamCache.Clear();

                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    int paramHash = Animator.StringToHash(param.name); // could use param.nameHash property but this is clearer
                    m_AnimatorParamCache.Add(param.name, paramHash);
                }
            }

            if (m_AnimatorParamCache != null && m_AnimatorParamCache.TryGetValue(paramName, out hash))
            {
                return true;
            }
            else
            {
                hash = 0;
                return false;
            }
        }

        #endregion

        #region Set Functions

        public virtual void SetVelocity(Vector2 velocity)
        {
            if (IsKnockback)
                return;

            velocityWorkspace.Set(velocity.x, velocity.y);
            Rb.linearVelocity = velocityWorkspace;
        }

        public virtual void SetVelocity(float velocity, Vector2 angle, int direction)
        {
            if (IsKnockback)
                return;

            angle.Normalize();
            velocityWorkspace.Set(angle.x * velocity * direction, angle.y * velocity);
            Rb.linearVelocity = velocityWorkspace;
        }

        public virtual void SetVelocityX(float velocity)
        {
            if (IsKnockback)
                return;

            velocityWorkspace.Set(velocity, Rb.linearVelocity.y);
            Rb.linearVelocity = velocityWorkspace;

            ProcessSlopeMovement(velocity);
        }

        public virtual void SetVelocityY(float velocity)
        {
            if (IsKnockback)
                return;

            velocityWorkspace.Set(Rb.linearVelocity.x, velocity);
            Rb.linearVelocity = velocityWorkspace;
        }

        private void ProcessSlopeMovement(float velocity)
        {
            if (entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                Quaternion slopeRotation = Quaternion.FromToRotation(Vector2.up, LastGroundInfo.normal);
                Vector2 adjustedVelocity = slopeRotation * (velocity * Vector2.right);

                if (adjustedVelocity.y < 0.5f && IsOnSlope)
                {
                    Rb.linearVelocity = adjustedVelocity;
                }
                else if (Rb.linearVelocity.y > 0.5f && !IsOnSlope)
                {
                    velocityWorkspace.Set(velocity, 0f);
                    Rb.linearVelocity = velocityWorkspace;
                }
            }
        }

        #endregion

        #region Check Functions

        /// <summary>
        /// Used by <see cref="Entity"/> to cast a ray to the right from the wallCheck transform.
        /// </summary>
        /// <returns>A boolean value</returns>
        public virtual bool CheckWall()
        {
            if (entityData.gameType == D_Entity.GameType.Platformer2D)
                return Physics2D.Raycast(wallCheck.position, transform.right, entityData.wallCheckDistance, entityData.whatIsObstacles);
            else
            {
                if (prevMovementDirection != Vector2.zero)
                    return Physics2D.Linecast(wallCheck.position, wallCheck.position + (Vector3)prevMovementDirection * entityData.wallCheckDistance, entityData.whatIsObstacles);
                else
                    return Physics2D.Linecast(wallCheck.position, wallCheck.position + transform.right * entityData.wallCheckDistance, entityData.whatIsObstacles);
            }
        }

        /// <summary>
        /// Used by <see cref="Entity"/> to cast a ray to the bottom from the ledgeCheck transform.
        /// </summary>
        /// <returns>A boolean
        public virtual bool CheckLedge()
        {
            if (entityData.gameType == D_Entity.GameType.Topdown2D)
                return false;

            return Physics2D.Raycast(ledgeCheck.position, -transform.up, entityData.ledgeCheckDistance, entityData.whatIsObstacles);
        }

        public virtual bool CheckLedgeBehind()
        {
            if (entityData.gameType == D_Entity.GameType.Topdown2D)
                return false;

            Vector2 p = ledgeCheck.position;

            p.x -= Mathf.Abs(transform.position.x - p.x) * 2 * transform.right.x;

            return Physics2D.Raycast(p, -transform.up, entityData.ledgeCheckDistance, entityData.whatIsObstacles);
        }

        /// <summary>
        /// Used by <see cref="Entity"/> to get a <see cref="bool"/> from the RigidBody2D.IsTouching using a contact filter.
        /// </summary>
        /// <returns>true if rigidbody is touching a collider in the right angles in the right layers.</returns>
        public virtual bool CheckGround()
        {
            if (entityData.gameType == D_Entity.GameType.Topdown2D)
                return false;

#if UNITY_EDITOR
            Debug.DrawRay((FacingDirection > 0 ? Collider.bounds.min + Collider.bounds.size.x * Vector3.right : Collider.bounds.min) + 0.5f * Vector3.up, Vector3.down * 1.5f);
#endif
            LastGroundInfo = Physics2D.Raycast((FacingDirection > 0 ? Collider.bounds.min + Collider.bounds.size.x * Vector3.right : Collider.bounds.min)
                + 0.5f * Vector3.up,
                 Vector2.down, 1.5f, entityData.whatIsObstacles);

            IsOnSlope = Mathf.Abs(LastGroundInfo.normal.x) > 0.1f;

            return Rb.IsTouching(groundContactFilter);
        }

        /// <summary> Used by <see cref="Entity"/> to cast a overlapBox within a specified size, angle, direction and distance specified on <see cref="D_Entity"/> entityData and draw a debug line if it detects a target </summary> <returns>A boolean value if there's any collisions</returns>
        public virtual bool CheckBox()
        {
            if (entityData.boxDetectionSize.x > 0 && entityData.boxDetectionSize.y > 0)
            {

                Vector3 originTweak = entityData.originOffset;
                originTweak.x *= transform.right.x;

                Collider2D collider = Physics2D.OverlapBox(transform.position + originTweak,
                 entityData.boxDetectionSize, entityData.boxDetectionAngle, entityData.whatIsTarget);

                if (collider && !SeeThroughWalls)
                {
                    RaycastHit2D rayCheckForObstacle = Physics2D.Linecast(targetCheck.position, collider.transform.position, entityData.whatIsObstacles);

                    if (rayCheckForObstacle)
                    {
                        //There's a obstacle
                        collider = null;
                    }
#if UNITY_EDITOR
                    else
                    {
                        Debug.DrawLine(targetCheck.position, collider.transform.position, Color.blue, 0f);
                    }
#endif
                }

                return HandleAggroAndTarget(collider);
            }
            else
            {
                Debug.LogWarning("BoxDetection size must be greater than zero.");
                return false;
            }
        }

        /// <summary> Used by <see cref="Entity"/> to cast a ray within a specified range and draw a debug line if it detects a target </summary> <returns>A boolean value if there's any collisions</returns>
        public virtual bool CheckTargetsInRange(float agroRange)
        {
            RaycastHit2D rayToTarget = Physics2D.Raycast(targetCheck.position, transform.right * FacingDirection, agroRange, entityData.whatIsTarget);

            if (rayToTarget && !SeeThroughWalls)
            {
                RaycastHit2D rayCheckForObstacle = Physics2D.Raycast(transform.position, (rayToTarget.transform.position - transform.position).normalized, rayToTarget.distance, entityData.whatIsObstacles);

                if (rayCheckForObstacle)
                {
                    //There's a obstacle
                    rayToTarget = new RaycastHit2D();
                }
            }

#if UNITY_EDITOR
            if (rayToTarget)
            {
                Debug.DrawLine(targetCheck.position, rayToTarget.transform.position, Color.blue, 0f);
            }
#endif
            return HandleAggroAndTarget(rayToTarget.collider ? rayToTarget.collider : null);
        }

        /// <summary> Used by <see cref="Entity"/> to cast a circle within a specified radius and draw a debug line if it detects a target </summary> <returns>A boolean value if there's any collisions</returns>
        public virtual bool CheckTargetsInRadius(float radius)
        {
            Collider2D col = GetBestTargetInRadius(radius);

#if UNITY_EDITOR
            if (col)
            {
                Debug.DrawLine(transform.position, col.transform.position, Color.blue, 0f);
            }
#endif
            return HandleAggroAndTarget(col);
        }

        /// <summary> Used by <see cref="Entity"/> to cast a circle and find if the target is inside the field of view and draw a debug of the FOV </summary> <returns>A boolean value if there's any collisions</returns>
        public virtual bool CheckTargetsInFieldOfView(float fov, float radius)
        {
            Collider2D targetInRadius = GetBestTargetInRadius(radius);

            if (targetInRadius && !SeeThroughWalls)
            {
                if (CheckFieldOfView(targetInRadius, fov))
                {
                    RaycastHit2D rayCheckForObstacle = Physics2D.Linecast(targetCheck.position, targetInRadius.transform.position, entityData.whatIsObstacles);

                    if (rayCheckForObstacle || targetInRadius.gameObject == gameObject)
                    {
                        //There's a obstacle
                        return HandleAggroAndTarget(null);
                    }
                    else
                    {
#if UNITY_EDITOR
                        Debug.DrawLine(targetCheck.position, targetInRadius.transform.position, Color.blue, 0f);
#endif
                        return HandleAggroAndTarget(targetInRadius);
                    }

                }
                else
                {
                    return HandleAggroAndTarget(null);
                }
            }

            else if (targetInRadius)
            {
                Vector2 dirToTarget = (targetInRadius.transform.position - targetCheck.position).normalized;

                if (Vector2.Angle(targetCheck.up, dirToTarget) < fov / 2 && targetInRadius.gameObject != gameObject)
                {
#if UNITY_EDITOR
                    Debug.DrawLine(targetCheck.position, targetInRadius.transform.position, Color.blue, 0.5f);
#endif
                    return HandleAggroAndTarget(targetInRadius);
                }
                else
                {
                    return HandleAggroAndTarget(null);
                }
            }

            return HandleAggroAndTarget(targetInRadius);
        }

        /// <summary> Used by <see cref="Entity"/> to get targets direction within a incremental range </summary> <returns> The first transform detected that is NOT the self entity. Null if there's no target around 100f radius or the entity can't see through walls.</returns>
        public virtual Transform GetFirstTargetTransform(float radius = 100f)
        {
            Collider2D col = GetBestTargetInRadius(radius);

            if (col)
            {
                return col.gameObject.transform;
            }

            return null;
        }

        private Collider2D GetBestTargetInRadius(float maxRadius = 100f)
        {
            previousTargetPos = Vector3.zero;

            for (int i = 0; i < 10; i++)
            {
                Collider2D c = null;

                if (i < targetsBuffer.Count)
                    c = targetsBuffer[i];

                if (c)
                {
                    if (c.gameObject != gameObject && Vector2.Distance(c.bounds.center, targetCheck.position) <= maxRadius)
                    {
                        if (!SeeThroughWalls)
                        {
                            if (Vector2.Distance(c.bounds.center, previousTargetPos) >= 1f)
                            {
                                if (!Physics2D.Linecast(targetCheck.position, c.bounds.center, entityData.whatIsObstacles))
                                {
                                    return c;
                                }
                            }
                            previousTargetPos = c.transform.position;
                        }
                        else
                        {
                            return c;
                        }
                    }
                    else
                    {
                        break;
                    }

                }
                else
                {
                    break;
                }
            }

            return null;
        }


        private bool CheckFieldOfView(Collider2D collider, float fov)
        {
            Vector2 dirToCenter = (collider.bounds.center - targetCheck.position).normalized;
            Vector2 dirToTop = (collider.bounds.max - targetCheck.position).normalized;
            Vector2 dirToBottom = (collider.bounds.min - targetCheck.position).normalized;

            if (Vector2.Angle(targetCheck.up, dirToCenter) < fov / 2 || 
                Vector2.Angle(targetCheck.up, dirToTop) < fov / 2 || 
                Vector2.Angle(targetCheck.up, dirToBottom) < fov / 2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get all the targets that's inside the 'WhatIsTarget' layer mask every 1 second in a radius of 100, and sort all the values based on distance to this entity.
        /// </summary>
        /// <returns></returns>
        private void GetAllTargets()
        {
            Physics2D.OverlapCircle(transform.position, 100f, targetsContactFilter, targetsBuffer);

            //List.Sort generate less GC alloc than OrderBy.
            if (targetsBuffer.Count > 1)
                targetsBuffer.Sort((a, b) => (a.transform.position - transform.position).sqrMagnitude.CompareTo((b.transform.position - transform.position).sqrMagnitude));
        }


        #endregion

        #region AnimFunctions
        public void PlayAnim(EntityAnimation animation)
        {
            if (BlockAnim || animation.Name.Length == 0 || !gameObject.activeInHierarchy)
            {
                if (BlockAnim)
                    PreviousAnimation = animation;

                return;
            }

            PreviousAnimation = CurrentPlayingAnimation;

            if (animation.TransitionLength > 0)
            {
                if (!Anim.IsInTransition(0))
                {
                    if (!Anim.GetCurrentAnimatorStateInfo(0).IsName(animation.Name))
                    {
                        if (TryGetAnimatorParam(Anim, animSpeedName, out int hash))
                        {
                            Anim.SetFloat(hash, animation.SpeedMultiplier);
                        }
                        Anim.CrossFade(animation.Name, animation.TransitionLength);
                        CurrentPlayingAnimation = animation;
                    }
                }
            }
            else
            {
                if (TryGetAnimatorParam(Anim, animSpeedName, out int hash))
                {
                    Anim.SetFloat(hash, animation.SpeedMultiplier);
                }

                Anim.Play(animation.Name, 0);
                CurrentPlayingAnimation = animation;
            }
        }

        private void PlayDamagedAnim()
        {
            if (BlockDamagedAnim)
                return;

            if (StateMachine.CurrentState.DamagedAnimation.Name.Length > 0 && StateMachine.CurrentState.DamagedAnimation.TransitionLength > 0)
            {
                Anim.CrossFade(StateMachine.CurrentState.DamagedAnimation.Name, StateMachine.CurrentState.DamagedAnimation.TransitionLength);
            }
            else
            {
                Anim.Play(StateMachine.CurrentState.DamagedAnimation.Name);
            }

            PreviousAnimation = CurrentPlayingAnimation;

            CurrentPlayingAnimation = StateMachine.CurrentState.DamagedAnimation;

            PlayAudio(StateMachine.CurrentState.DamagedAnimation.SoundAsset);
        }

        #endregion

        #region AudioFunctions

        /// <summary>
        /// Basic audio function to play audios using <see cref="Sound"/> scriptable objects.
        /// </summary>
        /// <param name="sound"></param>
        public void PlayAudio(Sound sound)
        {
            if (audioSource == null || audioSource.loop && (sound != null && sound.clip == audioSource.clip) || audioSource.isPlaying && (sound != null && audioSource.clip == sound.clip))
                return;

            if (!sound)
            {
                if (audioSource.loop)
                    audioSource.Stop();

                return;
            }

            if (audioSource.loop && sound.clip != audioSource.clip)
                audioSource.Stop();

            if (audioSource.outputAudioMixerGroup != sound.mixerGroup)
                audioSource.outputAudioMixerGroup = sound.mixerGroup;

            audioSource.loop = sound.loop;

            audioSource.clip = sound.clip;

            audioSource.volume = sound.volume * (1f + UnityEngine.Random.Range(-sound.volumeVariance / 2f, sound.volumeVariance / 2f));
            audioSource.pitch = sound.pitch * (1f + UnityEngine.Random.Range(-sound.pitchVariance / 2f, sound.pitchVariance / 2f));

            audioSource.Play();
        }

        /// <summary>
        /// With this overload you can play sounds without needing to wait for the current sound. Calling this function can play more than one song at the time.
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="OneShot"></param>
        public void PlayAudio(Sound sound, bool OneShot)
        {
            if (OneShot && sound)
            {
                if (audioSource.loop && sound.clip != audioSource.clip)
                    audioSource.Stop();

                if (audioSource.outputAudioMixerGroup != sound.mixerGroup)
                    audioSource.outputAudioMixerGroup = sound.mixerGroup;

                audioSource.loop = sound.loop;

                audioSource.clip = sound.clip;

                audioSource.volume = sound.volume * (1f + UnityEngine.Random.Range(-sound.volumeVariance / 2f, sound.volumeVariance / 2f));
                audioSource.pitch = sound.pitch * (1f + UnityEngine.Random.Range(-sound.pitchVariance / 2f, sound.pitchVariance / 2f));

                audioSource.PlayOneShot(sound.clip);
            }
            else if (sound)
            {
                PlayAudio(sound);
            }
        }

        public void StopAudio()
        {
            if (audioSource == null || !audioSource.isPlaying)
                return;

            audioSource.Stop();
        }

        #endregion

        #region Others Functions

        protected virtual void OnStun() => IsStunned = true;

        protected virtual IEnumerator KnockbackTimer(float duration)
        {
            yield return new WaitForSeconds(duration);

            IsKnockback = false;
        }

        public void ResetStunResistance()
        {
            EntityDelegates.OnEntityRecoveredStunned?.Invoke(this);
            CurrentStunResistance = entityData.stunResistance;
        }

        public void AddHealing(float amount)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, MaxHealth);
        }

        public void IncreaseAggro(float duration, Transform Target = null)
        {
            if (!IsPacifyState)
            {
                if(Target && Vector2.Distance(transform.position, Target.position) <= StateMachine.CurrentState.MinDistanceToMultiplyAggro)
                    CurrentAggroValue += (Time.deltaTime / duration * StateMachine.CurrentState.AggroMultiplier) * 100f;
                else
                    CurrentAggroValue += (Time.deltaTime / duration) * 100f;

                CurrentAggroValue = Mathf.Clamp(CurrentAggroValue, 0, 100);
            }
        }

        public void DecreaseAggro()
        {
            if (!IsPacifyState && !DisableAggroDecreasing)
            {
                CurrentAggroValue -= (Time.deltaTime / StateMachine.CurrentState.DecreaseDuration) * 100f;

                CurrentAggroValue = Mathf.Clamp(CurrentAggroValue, 0, 100);
            }
        }

        public bool HandleAggroAndTarget(Collider2D TargetCol)
        {
            if (StateMachine.CurrentState.UseAggroFill)
            {
                if (TargetCol)
                {
                    IncreaseAggro(StateMachine.CurrentState.FillDuration, TargetCol.transform);

                    if (CurrentAggroValue >= 100f)
                    {
                        EntityDelegates.OnTargetDetected?.Invoke(this);
                        IsInAggroState = true;
                        CurrentTargetaggroed = TargetCol;
                    }
                }
                else
                {
                    DecreaseAggro();

                    if (CurrentAggroValue <= 0)
                    {
                        EntityDelegates.OnTargetNotDetected?.Invoke(this);
                        IsInAggroState = false;
                        CurrentTargetaggroed = null;
                    }
                }

            }
            else
            {
                if (TargetCol)
                {
                    EntityDelegates.OnTargetDetected?.Invoke(this);
                    CurrentTargetaggroed = TargetCol;
                    return true;
                }
                else
                {
                    if (StateMachine.CurrentState.GetType() != typeof(E_CheckTargetsDetected))
                        EntityDelegates.OnTargetNotDetected?.Invoke(this);

                    CurrentTargetaggroed = null;
                    return false;
                }
            }

            return IsInAggroState;
        }


        public void InstantFillAggro()
        {
            if (!IsPacifyState)
            {
                CurrentAggroValue = 100f;
            }
        }

        public void PacifyEntity(bool ClearAggro)
        {
            IsPacifyState = true;
            if (ClearAggro)
            {
                IsInAggroState = false;
                CurrentAggroValue = 0f;
            }
        }

        public void UnPacifyEntity() => IsPacifyState = false;

        /// <summary>
        /// Instantiates a prefab at the position of the entity.
        /// </summary>
        /// <param name="prefab"></param>
        public void InstantiatePrefab(GameObject prefab)
        {
            Instantiate(prefab, transform.position, prefab.transform.rotation);
        }

        /// <summary>
        /// This functions stops the movement of the entity and apply knockback force
        /// </summary>
        /// <param name="details"></param>
        public void KnockBack(float knockBackLevel, float knockBackDuration, Vector2 knockBackDirection)
        {
            if (StopKnockBack)
                return;

            velocityWorkspace = knockBackLevel * knockBackDirection;

            Rb.linearVelocity = velocityWorkspace;

            IsKnockback = true;

            StartCoroutine(KnockbackTimer(knockBackDuration));
        }

        /// <summary>
        /// This can be used by states when they find a target and want to send damage
        /// </summary>
        /// <param name="damageDetails"></param>
        /// <param name="target"></param>
        public virtual void SendDamage(DamageDetails damageDetails, GameObject target, float knockBackLevel, float knockBackDuration, Vector2 knockBackDirection)
        {
            if (target.TryGetComponent(out IDamageable d))
            {
                d.Damage(damageDetails);
                d.KnockBack(knockBackLevel, knockBackDuration, knockBackDirection);
                EntityDelegates.OnDamageSend?.Invoke(this, target, damageDetails);
            }
        }

        /// <summary>
        /// Basic damage function that contains some logic for particles, damaged times and others variables. override to add some features like ScreenShake or others behaviors when an entity is damaged
        /// </summary>
        /// <param name="damageDetails"></param>
        public virtual void Damage(DamageDetails damageDetails)
        {
            if (!IsDamaged && !IsDead && CanBeDamaged)
            {
                IsDamaged = true;

                EntityDelegates.OnDamageReceive?.Invoke(this, damageDetails);

                if (!CanBeDamaged)
                {
                    IsDamaged = false;
                    return;
                }

                if (!IsStunned)
                {
                    lastDamageTime = Time.time;
                    CurrentStunResistance -= damageDetails.stunDamageAmount;
                }

                CurrentHealth -= damageDetails.damageAmount;

                DamageDirectionX = damageDetails.position.x < transform.position.x ? -1 : 1;
                DamageDirectionVector = (damageDetails.position - (Vector2)transform.position).normalized;

                if (CurrentHealth <= 0f)
                {
                    CurrentHealth = 0f;
                    IsDead = true;
                }

                if(CurrentStunResistance <= 0 && !IsStunned && entityData.stunResistance > 0)
                {
                    OnStun();
                }

                if (entityData.faceDamageDirection && !IsStunned && !IsDead)
                {
                    FlipToTarget(damageDetails.position, true);
                }

                if (StateMachine.CurrentState.PlayDamagedAnim && !IsStunned && !IsDead)
                {
                    if(CurrentPlayingAnimation != null)
                    {
                        if (CurrentPlayingAnimation.CanBeStoppedByDamage)
                        {
                            BlockAnim = true;
                            PlayDamagedAnim();
                        }
                        
                    }
                    else
                    {
                        BlockAnim = true;
                        PlayDamagedAnim();
                    }
                }

                if (hitParticles != null)
                {
                    if (hitParticles.Length > 0)
                    {
                        for (int i = 0; i < hitParticles.Length; i++)
                        {
                            ParticleSystem hitParticle = hitParticles[i];
                            hitParticle.Play(true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called by animation events.
        /// </summary>
        public void AnimationTrigger1()
        {
            StateMachine.CurrentState.AnimationTrigger1();
        }

        /// <summary>
        /// Called by animation events.
        /// </summary>
        public void AnimationTrigger2()
        {
            StateMachine.CurrentState.AnimationTrigger2();
        }

        /// <summary>
        /// Called by animation events.
        /// </summary>
        public void AnimationFinish()
        {
            StateMachine.CurrentState.AnimationFinish();
            if (BlockAnim)
            {
                BlockAnim = false;
                PlayAnim(PreviousAnimation);
            }
            else
            {
                StateMachine.CurrentState.AnimationFinish();
            }
        }

        public bool IsFacingPosition(Vector2 position)
        {
            if (entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                if (FacingDirection == 1 && position.x >= transform.position.x || FacingDirection == -1 && position.x <= transform.position.x)
                    return true;
                else
                    return false;
            }
            else
            {
                Vector2 targetDir = (position - (Vector2)transform.position).normalized;

                Debug.Log(Vector2.Angle(transform.right, entityData.rotateTheTransform ?
                    targetDir : prevMovementDirection));

                if (Vector3.Angle(entityData.rotateTheTransform ? transform.right : (Vector3)targetDir, entityData.rotateTheTransform ?
                    targetDir : prevMovementDirection) <= 45)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Function to flip the facing direction and rotate the transform in Y axis. Platformer 2D only.
        /// </summary>
        public virtual void Flip()
        {
            if (entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                FacingDirection *= -1;
                transform.Rotate(0f, 180f, 0f);
            }
        }

        public virtual void FlipToTarget(float radiusSearch = 100f)
        {
            Transform target = GetFirstTargetTransform(radiusSearch);

            Vector2 targetPos = Vector2.zero;

            if (target)
                targetPos = target.position;

            FlipToTarget(targetPos, true);
        }

        public virtual void FlipToTarget(Transform target)
        {
            Vector2 targetPos = target.position;

            FlipToTarget(targetPos, true);
        }

        /// <summary>
        /// This functions flips or rotate the entity to a direction or target position.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="isPos"></param>
        public virtual void FlipToTarget(Vector2 target, bool isPos)
        {
            if (entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                if (isPos)
                {
                    if (target.x > Rb.position.x && FacingDirection == -1 ||
                    target.x < Rb.position.x && FacingDirection == 1)
                        Flip();
                }
                else
                {
                    if (target.x > 0 && FacingDirection == -1 || target.x < 0 && FacingDirection == 1)
                        Flip();
                }
            }
            else
            {
                Vector2 dir;

                if (isPos)
                    dir = (target - Rb.position).normalized;
                else
                    dir = target;

                if (!entityData.rotateTheTransform)
                {
                    if (TryGetAnimatorParam(Anim, prevDirectionXName, out int Hash1))
                        Anim.SetFloat(Hash1, dir.x);
                    if (TryGetAnimatorParam(Anim, prevDirectionYName, out int Hash2))
                        Anim.SetFloat(Hash2, dir.y);
                }
                else
                {
                    LookAt2D(dir);
                }
            }
        }

        /// <summary>
        /// This works just like the lookAt but for 2D.
        /// </summary>
        /// <param name="targetPos"></param>
        public virtual void LookAt2D(Vector2 targetDir, bool smooth = true)
        {
            float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

            if (entityData.gameType == D_Entity.GameType.Platformer2D)
                angle += FacingDirection == 1 ? 0 : 180;

            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            rotation.x = 0;

            if (smooth)
                if (entityData.rotationSpeed > 0)
                {
                    lerpTime = 0f;
                    RotateRigidbody = true;
                    TargetAngle = angle;
                    PreviousAngle = transform.rotation.eulerAngles.z;
                }
                else
                    transform.rotation = rotation;
            else
                transform.rotation = rotation;
        }

        /// <summary>
        /// This function will reset the state of the entity from dead to alive and reset the current health.
        /// </summary>
        public virtual void ResetEntity()
        {
            CurrentHealth = entityData.maxHealth;
            IsDead = false;
            IsStunned = false;
        }
        #endregion

        public virtual void OnDrawGizmos()
        {
            if (entityData.gameType == D_Entity.GameType.Platformer2D)
            {
                Gizmos.DrawLine(wallCheck.position, wallCheck.position + transform.right * entityData.wallCheckDistance);

                Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + -transform.up * entityData.ledgeCheckDistance);

                Vector2 p = ledgeCheck.position;

                p.x -= Mathf.Abs(transform.position.x - p.x) * 2 * transform.right.x;

                Gizmos.DrawRay(p, -transform.up * entityData.ledgeCheckDistance);
            }
            else
            {
                if (prevMovementDirection != Vector2.zero)
                    Gizmos.DrawLine(wallCheck.position, wallCheck.position + (Vector3)prevMovementDirection * entityData.wallCheckDistance);
                else
                    Gizmos.DrawLine(wallCheck.position, wallCheck.position + transform.right * entityData.wallCheckDistance);
            }
        }
    }

}