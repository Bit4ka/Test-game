using UnityEngine;
using AI2DTool;
using UnityEngine.Events;

namespace MaykerStudio
{
    public class EntityInteractable : MonoBehaviour
    {
        [BeginGroup]
        [Tooltip("If any entity on this layer enters the trigger's collider the events will be raised.")]
        public LayerMask EntityLayers;

        [ShowIf(nameof(UseEntityTag), true)]
        [TagSelector]
        public string EntityTag;

        [ShowIf(nameof(UsePhysics2D), true)]
        public float DetectionRadius = 0.5f;

        [Tooltip("If you want this object to interact only with entities with a specific tag")]
        public bool UseEntityTag;

        [Tooltip("If you want this object to interact only with entities with a specific tag")]
        public bool UsePhysics2D;

        [Space(10)]
        public UnityEvent OnEntityEnter;

        [EndGroup]
        public UnityEvent OnEntityExit;

        private EntityAI _cachedEntity;

        private Collider2D[] _entities = new Collider2D[1];

        private bool _entityDetected;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (CheckLayers(collision))
            {
                OnEntityEnter?.Invoke();
                if(_cachedEntity != null)
                    _cachedEntity.InvokeOnObjectClose(gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (CheckLayers(collision))
                OnEntityExit?.Invoke();
        }

        private void FixedUpdate()
        {
            if (UsePhysics2D)
            {
                Physics2D.OverlapCircleNonAlloc(transform.position, DetectionRadius, _entities, EntityLayers);

                if (_entities[0] != null)
                {
                    if (!_entityDetected && CheckLayers(_entities[0]))
                    {
                        _entityDetected = true;
                        OnEntityEnter?.Invoke();
                    }
                    else if (_entityDetected && !CheckLayers(_entities[0]))
                    {
                        _entityDetected = false;
                        OnEntityExit?.Invoke();
                    }
                }
                else
                {
                    if (_entityDetected)
                    {
                        _entityDetected = false;
                        OnEntityExit?.Invoke();
                    }
                }
            }
        }

        private bool CheckLayers(Collider2D collision)
        {
            if (UseEntityTag && !collision.gameObject.CompareTag(EntityTag))
                return false;

            if (EntityLayers == (EntityLayers | (1 << collision.gameObject.layer)))
            { 
                if (_cachedEntity == null || _cachedEntity.gameObject != collision.gameObject)
                {
                    if (collision.gameObject.TryGetComponent(out EntityAI entityAI))
                    {
                        _cachedEntity = entityAI;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;

            }
            else
            {
                return false;
            }
        }
    }
}