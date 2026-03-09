using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Combat/Hit Scan Shoot")]
    public class E_HitScan : EntityState
    {
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation PreparingAnimation;

        [SerializeField]
        private EntityAnimation ShootAnimation;

        [SerializeField]
        private GameObject SpawnOnHitObstacles;

        [SerializeField]
        private GameObject SpawnOnHitTarget;

        [Min(0f)]
        [SerializeField]
        private int attackDamage = 10;

        [Min(0)]
        [SerializeField]
        private int stunAmount = 10;

        [Min(0f)]
        [SerializeField]
        private int knockBackLevel = 4;

        [Range(0f, 1f)]
        [SerializeField]
        private float knockBackDuration = 0.2f;

        [Min(1)]
        [SerializeField]
        private int MaxShots = 1;

        [SerializeField]
        private float preparingDuration = 3f;

        [SerializeField]
        private bool UseLineRenderer;

        [SerializeField]
        [ShowIf(nameof(UseLineRenderer), true)]
        private Gradient PreparingGrad;

        [EndGroup]
        [SerializeField]
        [ShowIf(nameof(UseLineRenderer), true)]
        private Gradient ShootingGrad;

        [EndGroup]
        [Header("If max shots")]
        [BeginGroup("Transitions")]
        [SerializeField]
        [Output(connectionType = ConnectionType.Override)]
        [Disable]
        private EntityState NextState1;

        public bool isPreparing { get; private set; }

        public bool isShooting { get; private set; }

        private bool hitted;

        private int Shoots;

        private float preparingCounter, lerpRay;

        private LineRenderer lineRenderer;

        private Transform target;

        private RaycastHit2D raycastToTarget;

        private PhysicsMaterial2D originalMat;

        public override void AnimationTrigger1()
        {
            base.AnimationTrigger1();

            if (UseLineRenderer)
            {
                lineRenderer.enabled = true;
                lineRenderer.colorGradient = ShootingGrad;
            }

            if (!raycastToTarget)
            {
                Vector2 knockBackDirection = (target.position - EntityAI.transform.position).normalized;

                EntityAI.SendDamage(damageDetails, target.gameObject, knockBackLevel, knockBackDuration, knockBackDirection);
            }
        }

        public override void AnimationFinish()
        {
            base.AnimationFinish();

            Shoots++;
            preparingCounter = 0f;
            isShooting = false;

            if (Shoots >= MaxShots)
            {
                isPreparing = false;
            }
            else
            {
                isPreparing = true;

                EntityAI.PlayAnim(PreparingAnimation);
                EntityAI.PlayAudio(PreparingAnimation.SoundAsset);
            }
        }

        public override void DoChecks()
        {
            base.DoChecks();

            if (UseAggroFill && target)
            {
                if(Physics2D.Linecast(EntityAI.targetCheck.position, target.position, EntityAI.entityData.whatIsObstacles))
                {
                    EntityAI.DecreaseAggro();
                }
                else
                {
                    EntityAI.IncreaseAggro(FillDuration, target);
                }
            }
        }

        public override void Enter()
        {
            base.Enter();

            originalMat = EntityAI.Rb.sharedMaterial;

            NextState1 = CheckState(NextState1);

            EntityAI.SeeThroughWalls = true;
            target = EntityAI.GetFirstTargetTransform();
            EntityAI.SeeThroughWalls = CanSeeThroughWalls;

            if (!lineRenderer && UseLineRenderer)
            {
                lineRenderer = EntityAI.gameObject.GetComponentInChildren<LineRenderer>();
                if (!lineRenderer)
                    Debug.LogError("No line renderer found on " + EntityAI.name);
                lineRenderer.enabled = true;
            }

            preparingCounter = 0f;

            isPreparing = true;
            isShooting = false;

            EntityAI.PlayAnim(PreparingAnimation);
            EntityAI.PlayAudio(PreparingAnimation.SoundAsset);

            EntityAI.Rb.sharedMaterial = null;

            damageDetails.damageAmount = attackDamage;
            damageDetails.stunDamageAmount = stunAmount;
			
			EntityAI.SetVelocityX(0f);
        }

        public override void Exit()
        {
            base.Exit();

            if (UseLineRenderer)
                lineRenderer.enabled = false;

            Shoots = 0;

            EntityAI.Rb.sharedMaterial = originalMat;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (isPreparing)
            {
                hitted = false;

                preparingCounter += Time.deltaTime;

                if(preparingCounter >= preparingDuration)
                {
                    isPreparing = false;
                }
            }
            else if (!isShooting)
            {
                lerpRay = 0f;
                isShooting = true;
                isAnimationFinished = false;
                EntityAI.PlayAnim(ShootAnimation);
                EntityAI.PlayAudio(ShootAnimation.SoundAsset);
            }

            if(Shoots >= MaxShots)
            {
                if(NextState1 != null)
                {
                    StateMachine.ChangeState(NextState1);
                }
            }

            if (target.position.x > EntityAI.Rb.position.x + 0.5f && EntityAI.FacingDirection == -1)
                EntityAI.Flip();
            else if(target.position.x < EntityAI.Rb.position.x - 0.5f && EntityAI.FacingDirection == 1)
                EntityAI.Flip();

            if (UseLineRenderer)
            {
                if (isPreparing)
                {
                    lineRenderer.enabled = true;
                    lineRenderer.colorGradient = PreparingGrad;
                }
                else if(!isShooting)
                {
                    lineRenderer.enabled = false;
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
    
            if (!isShooting)
                raycastToTarget = Physics2D.Linecast(EntityAI.attackCheck.position, target.position, EntityAI.entityData.whatIsObstacles);

            if (UseLineRenderer && (isPreparing || isShooting))
            {
                if (isShooting)
                {
                    lerpRay += Time.deltaTime / 0.1f;
                    if (raycastToTarget)
                    {
                        lineRenderer.SetPosition(0, Vector3.Lerp(EntityAI.attackCheck.position, raycastToTarget.point, lerpRay));

                        if(SpawnOnHitObstacles && !hitted)
                        {
                            hitted = true;
                            Instantiate(SpawnOnHitObstacles, raycastToTarget.point, SpawnOnHitObstacles.transform.rotation);
                        }
                    }
                    else
                    {
                        lineRenderer.SetPosition(0, Vector3.Lerp(EntityAI.attackCheck.position, target.position, lerpRay));

                        if (SpawnOnHitTarget && !hitted)
                        {
                            hitted = true;
                            Instantiate(SpawnOnHitTarget, target.position, SpawnOnHitObstacles.transform.rotation);
                        }
                    }
                }
                else
                    lineRenderer.SetPosition(0, EntityAI.attackCheck.position);

                if (raycastToTarget)
                    lineRenderer.SetPosition(1, raycastToTarget.point);
                else
                    lineRenderer.SetPosition(1, target.position);
            }
        }

        public override object GetValue(NodePort port)
        {
            NextState1 = GetFromPort("NextState1", port, NextState1);

            return port.Connection?.node;
        }
    }
}