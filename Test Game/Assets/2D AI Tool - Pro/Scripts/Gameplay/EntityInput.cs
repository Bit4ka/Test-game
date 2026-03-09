using UnityEngine;
using AI2DTool;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MaykerStudio
{
    [DisallowMultipleComponent]
    public class EntityInput : MonoBehaviour
    {
        [BeginGroup("Player detection")]
        [TagSelector]
        public string PlayerTag;

        [Layer]
        public int PlayerLayer;

        [ShowIf(nameof(UsePhysics2D), true)]
        public float DetectionRadius = 0.5f;

        [EndGroup]
        [Tooltip("If checked, the detection mode will use the Physics2D api. If not, it'll use the OnTriggers events, " +
            "make sure the this object layer can collide with the others one.")]
        public bool UsePhysics2D = true;

#if ENABLE_INPUT_SYSTEM
        [NotNull]
        [BeginGroup("Player input")]
        public InputActionAsset InputActions;

        public string InteractActionName = "Interact";

        public InputAction InteractAction { get; private set; }

#else
        [BeginGroup("Player input")]
        [SearchableEnum]
        public KeyCode InteractInput = KeyCode.E;
#endif

        public UnityEvent OnPlayerEnterRange;

        [EndGroup]
        public UnityEvent OnPlayerExitRange;

        public EntityAI EntityAI { get; private set; }

        private bool _playerDetected;

        private LayerMask PlayerMask;

        private Collider2D[] _Players = new Collider2D[5];


        private void OnEnable()
        {
            _playerDetected = false;
#if ENABLE_INPUT_SYSTEM
            InteractAction?.Enable();
#endif
        }

        private void OnDisable()
        {
            OnPlayerExitRange?.Invoke();

#if ENABLE_INPUT_SYSTEM
            InteractAction?.Disable();
# endif
        }

            private void Awake()
        {
            PlayerMask |= 1 << PlayerLayer;

            if (TryGetComponent(out EntityAI entity))
            {
                EntityAI = entity;
            }
            else
            {
                Debug.LogError(this + " requires a EntityAI");
            }

#if ENABLE_INPUT_SYSTEM

            InteractAction = InputActions.FindAction(InteractActionName);

            if (InteractAction == null)
            {
                Debug.LogError(name + " not found '" + InteractActionName + "' on " + InputActions.name);
                return;
            }

            InteractAction.performed += ctx =>
            {
                InvokeInputEvent();
            };
#endif
        }

#if !ENABLE_INPUT_SYSTEM
        private void Update()
        {
            if (Input.GetKeyDown(InteractInput))
            {
                InvokeInputEvent();
            }
        }
#endif

        private void FixedUpdate()
        {
            if (UsePhysics2D)
            {
                Physics2D.OverlapCircleNonAlloc(transform.position, DetectionRadius, _Players, PlayerMask);

                if (_Players[0].gameObject != null)
                {
                    if (!_playerDetected)
                    {
                        if (CheckLayers(_Players))
                        {
                            _playerDetected = true;
                            OnPlayerEnterRange?.Invoke();
                        }
                    }
                    else
                    {
                        if (!CheckLayers(_Players))
                        {
                            _playerDetected = false;
                            OnPlayerExitRange?.Invoke();
                        }
                    }
                }
                else
                {
                    if (_playerDetected)
                    {
                        _playerDetected = false;
                        OnPlayerExitRange?.Invoke();
                    }
                }

                System.Array.Clear(_Players, 0, _Players.Length);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!UsePhysics2D && CheckLayers(collision))
            {
                _playerDetected = true;
                OnPlayerEnterRange?.Invoke();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!UsePhysics2D && CheckLayers(collision))
            {
                _playerDetected = false;
                OnPlayerExitRange?.Invoke();
            }
        }

        public void InvokeInputEvent()
        {
            if (_playerDetected)
            {
#if ENABLE_INPUT_SYSTEM
                EntityAI.InvokeOnPlayerInput(InteractAction);
#else
                EntityAI.InvokeOnPlayerInput(InteractInput);
#endif
            }
        }

        private bool CheckLayers(Collider2D collision)
        {
            return CheckLayers(collision.gameObject);
        }

        private bool CheckLayers(Collider2D[] collisions)
        {
            for (int i = 0; i < collisions.Length; i++)
            {
                Collider2D c = collisions[i];

                if (c == null)
                    break;

                if (CheckLayers(c.gameObject))
                    return true;
            }

            return false;
        }

        private bool CheckLayers(GameObject obj)
        {
            if (PlayerLayer == obj.layer && obj != gameObject)
            {
                if (obj.CompareTag(PlayerTag))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void OnDrawGizmos()
        {
            if (DetectionRadius > 0 && UsePhysics2D)
                Gizmos.DrawWireSphere(transform.position, DetectionRadius);
        }
    }
}