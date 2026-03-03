using UnityEngine;

public class Door : MonoBehaviour
{
    public enum Direction { North, East, South, West }
    public Direction doorDirection;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
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
}