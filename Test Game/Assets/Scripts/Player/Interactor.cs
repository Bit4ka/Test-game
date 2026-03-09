using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [Header("Player Interaction")]
    [SerializeField] private Vector2 _interactionRange;
    [SerializeField] private Vector3 _rangeOffset;
    [SerializeField] private LayerMask _interactionLayer;
    [SerializeField] private GameObject _interactionIcon;

    private void Update()
    {
        InteractionIcon();
    }

    #region Interaction

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position + _rangeOffset, _interactionRange, 0, _interactionLayer);

            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent<IInteractable>(out IInteractable interactable))
                {
                    if (interactable.CanInteract(gameObject))
                        interactable.Interact(gameObject);
                }
            }

        }
    }

    private void InteractionIcon()
    {
        bool showIcon = false;
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position + _rangeOffset, _interactionRange, 0, _interactionLayer);

        if (hits.Length == 0) showIcon = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                if (interactable.CanInteract(gameObject))
                {
                    showIcon = true;
                    break;
                }
            }
        }

        _interactionIcon.SetActive(showIcon);
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + _rangeOffset, _interactionRange);
    }
}
