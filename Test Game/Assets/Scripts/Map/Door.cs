using Unity.Cinemachine;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public enum Direction { North, East, South, West }
    public Direction doorDirection;

    private Animator animator;

    [SerializeField] private float _interactionRange = 2;
    [SerializeField] private Transform _placeToGo;
    [SerializeField] PolygonCollider2D _mapBoundry;
    private CinemachineConfiner2D _confiner;

    void Start()
    {
        animator = GetComponent<Animator>();
        _confiner = FindAnyObjectByType<CinemachineConfiner2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetTrigger("Open");
            // Tutaj później: RoomManager.MoveToNextRoom(doorDirection);
            Debug.Log("Otwieram drzwi: " + doorDirection);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetTrigger("Close");
            Debug.Log("Zamykam drzwi: " + doorDirection);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (_placeToGo == null)
        {
            Debug.LogWarning("Place to go not set");
            return;
        }

        if (_mapBoundry == null)
        {
            Debug.LogWarning("Map boundry not set");
            return;
        }

        _confiner.BoundingShape2D = _mapBoundry;
        interactor.transform.position = _placeToGo.position;
        
    }

    public bool CanInteract(GameObject interactor)
    {
        float distance = Vector2.Distance(interactor.transform.position, transform.position);
        return distance < _interactionRange;
    }
}