using MaykerStudio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("EntityState/Others/Change Objects Stats")]
    public class E_ChangeObjectsStats : EntityState
    {
        #region Base Vars
        [BeginGroup("Variables")]
        [SerializeField]
        private EntityAnimation StateAnimation;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("The detection radius to get the objects with tag and specified layers. Check the 'CanSeeThroughWalls' if you don't mind the entity to change objects behind walls.")]
        private float objectsDetectionRadius = 1f;

        [SerializeField]
        [Tooltip("Check this if you want the state to just change the stats and play no animation.")]
        private bool NoAnimation;

        [SerializeField]
        [ShowIf(nameof(NoAnimation), false)]
        [Tooltip("Check this if you want the state to change only when the 'AnimationTrigger1' is called.")]
        private bool WaitForTrigger1;

        [SerializeField]
        [ShowIf(nameof(WaitForTrigger1), true)]
        [Tooltip("If checked, the state will change all detected objects at once, if not, it'll change one by one every time the AnimationTrigger1 is called.")]
        private bool ChangeAllAtOnce;

        [SerializeField]
        [TagSelector]
        private string ObjectsTag;

        [SerializeField]
        [EndGroup]
        private LayerMask ObjectsLayers;

        #endregion

        #region Stats Fields

        [SerializeField]
        [BeginGroup("Change")]
        private ObjectStats WhatToChange;

        [SerializeField]
        [ShowIf(nameof(WhatToChange), ObjectStats.Sprite)]
        private Sprite NewSprite;

        [SerializeField]
        [ShowIf(nameof(WhatToChange), ObjectStats.SpriteColor)]
        private Color NewSpriteColor;

        [SerializeField]
        [ShowIf(nameof(WhatToChange), ObjectStats.Material)]
        private Material NewMaterial;

        [SerializeField]
        [ShowIf(nameof(WhatToChange), ObjectStats.PhysicsMaterial)]
        private PhysicsMaterial2D NewPhysicsMaterial2D;

        [SerializeField]
        [ShowIf(nameof(WhatToChange), ObjectStats.RigidBodyType)]
        private RigidBodyType NewRigidBodyType;

        [SerializeField]
        [Layer]
        [ShowIf(nameof(WhatToChange), ObjectStats.Layer)]
        private int NewLayer;

        [EndGroup]
        [SerializeField]
        [TagSelector]
        [ShowIf(nameof(WhatToChange), ObjectStats.Tag)]
        private string NewTag;

        #endregion

        #region Transitions

        [BeginGroup("Transitions")]
        [SerializeField]
        [Disable]
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Header("On Animation Finish || No Animation")]
        [EndGroup]
        private EntityState NextState;

        #endregion

        private int _index = 0;

        private Collider2D[] _results = new Collider2D[10];

        private List<Collider2D> _objectsWithTag = new List<Collider2D>();

        private Dictionary<ObjectStats, System.Func<GameObject, bool>> _changeFunctions = new Dictionary<ObjectStats, System.Func<GameObject, bool>>();

        protected override void Init()
        {
            base.Init();

            _changeFunctions.Clear();

            _changeFunctions.Add(ObjectStats.Sprite, ChangeSprite);
            _changeFunctions.Add(ObjectStats.SpriteColor, ChangeSpriteColor);
            _changeFunctions.Add(ObjectStats.RigidBodyType, ChangeRigidBodyType);
            _changeFunctions.Add(ObjectStats.Layer, ChangeLayer);
            _changeFunctions.Add(ObjectStats.Material, ChangeMaterial);
            _changeFunctions.Add(ObjectStats.PhysicsMaterial, ChangePhysicsMaterial);
            _changeFunctions.Add(ObjectStats.Tag, ChangeTag);
            _changeFunctions.Add(ObjectStats.EnableChilds, ChangeEnableChilds);
        }

        public override void AnimationFinish()
        {
            if (WaitForTrigger1 && !ChangeAllAtOnce)
            {
                if(_index < _objectsWithTag.Count)
                {
                    isAnimationFinished = false;
                }
                else
                {
                    isAnimationFinished = true;
                }
            }
            else
            {
                isAnimationFinished = true;
            }
        }

        public override void AnimationTrigger1()
        {
            if(WaitForTrigger1 && ChangeAllAtOnce)
            {
                if (_objectsWithTag.Count > 0)
                {
                    for (int i = 0; i < _objectsWithTag.Count; i++)
                    {
                        GameObject obj = _objectsWithTag[i].gameObject;

                        _changeFunctions[WhatToChange].Invoke(obj);
                    }
                }
            }
            else if (WaitForTrigger1)
            {
                if(_index < _objectsWithTag.Count)
                {
                    _changeFunctions[WhatToChange].Invoke(_objectsWithTag[_index].gameObject);
                    _index++;
                } 
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

            _index = 0;

            if (!NoAnimation)
            {
                EntityAI.PlayAnim(StateAnimation);
                EntityAI.PlayAudio(StateAnimation.SoundAsset);
            }
            else
            {
                WaitForTrigger1 = false;
            }
            
            NextState = CheckState(NextState);

            GetObjectsClose();

            if(_objectsWithTag.Count > 0)
            {
                if (!WaitForTrigger1)
                {
                    for (int i = 0; i < _objectsWithTag.Count; i++)
                    {
                        GameObject obj = _objectsWithTag[i].gameObject;

                        _changeFunctions[WhatToChange].Invoke(obj);
                    }
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override object GetValue(NodePort port)
        {
            NextState = GetFromPort("NextState", port, NextState);

            return port.Connection?.node;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (isAnimationFinished || _objectsWithTag.Count == 0 || NoAnimation)
            {
                if(NextState != null && !NextState.IsInCooldown)
                {
                    StateMachine.ChangeState(NextState);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
        }

        private void GetObjectsClose()
        {
            Physics2D.OverlapCircleNonAlloc(EntityAI.transform.position, objectsDetectionRadius, _results, ObjectsLayers);

            _objectsWithTag.Clear();

            for (int i = 0; i < _results.Length; i++)
            {
                Collider2D c = _results[i];

                if (c && c.gameObject.CompareTag(ObjectsTag))
                {
                    if (CanSeeThroughWalls)
                    {
                        _objectsWithTag.Add(c);
                    }
                    else
                    {
                        if (!Physics2D.Linecast(EntityAI.transform.position, c.transform.position, EntityAI.entityData.whatIsObstacles))
                        {
                            _objectsWithTag.Add(c);
                        }
                    }
                }
            }
        }

        private enum ObjectStats
        {
            Sprite,
            SpriteColor,
            Material,
            PhysicsMaterial,
            RigidBodyType,
            EnableChilds,
            Tag,
            Layer
        }

        private enum RigidBodyType
        {
            Dynamic,
            Kinematic,
            Static
        }

        private bool ChangeSprite(GameObject obj)
        {
            if(obj.TryGetComponent(out SpriteRenderer renderer))
            {
                renderer.sprite = NewSprite;

                return true;
            }

            return false;
        }

        private bool ChangeSpriteColor(GameObject obj)
        {
            if (obj.TryGetComponent(out SpriteRenderer renderer))
            {
                renderer.color = NewSpriteColor;

                return true;
            }

            return false;
        }

        private bool ChangeMaterial(GameObject obj)
        {
            if (obj.TryGetComponent(out Renderer renderer))
            {
                renderer.material = NewMaterial;

                return true;
            }

            return false;
        }

        private bool ChangePhysicsMaterial(GameObject obj)
        {
            if (obj.TryGetComponent(out Rigidbody2D rb))
            {
                rb.sharedMaterial = NewPhysicsMaterial2D;

                return true;
            }

            return false;
        }

        private bool ChangeRigidBodyType(GameObject obj)
        {
            if (obj.TryGetComponent(out Rigidbody2D rb))
            {
                switch (NewRigidBodyType)
                {
                    case RigidBodyType.Dynamic:
                        rb.bodyType = RigidbodyType2D.Dynamic;
                        break;
                    case RigidBodyType.Kinematic:
                        rb.bodyType = RigidbodyType2D.Kinematic;
                        break;
                    case RigidBodyType.Static:
                        rb.bodyType = RigidbodyType2D.Static;
                        break;
                }
                return true;
            }

            return false;
        }

        private bool ChangeEnableChilds(GameObject obj)
        {
            if(obj.transform.childCount > 0)
            {
                foreach (Transform item in obj.transform)
                {
                    item.gameObject.SetActive(true);
                }

                return true;
            }

            return false;
        }

        private bool ChangeTag(GameObject obj)
        {
            obj.tag = NewTag;

            return false;
        }

        private bool ChangeLayer(GameObject obj)
        {
            obj.layer = NewLayer;

            return false;
        }

        public override void DrawGizmosDebug()
        {
            base.DrawGizmosDebug();

            ExtDebug.DrawEllipse(EntityAI.transform.position, objectsDetectionRadius, objectsDetectionRadius, 32, Color.green);
        }
    }
}