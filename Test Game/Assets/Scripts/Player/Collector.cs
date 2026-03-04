using UnityEngine;

public class Collector : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private BoxCollider2D _collider;

    [Header("Settings")]
    [SerializeField] private LayerMask _itemsLayer;
    [SerializeField] private Vector2 _pickUpRange;

    private void Update()
    {
        PickUp();
    }

    private void PickUp()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(_collider.bounds.center, _pickUpRange, 0, _itemsLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<ICollectible>(out ICollectible collectible))
            {
                collectible.Collect(this);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_collider.bounds.center, _pickUpRange);
    }
}
