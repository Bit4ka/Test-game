using UnityEngine;

public class Door : MonoBehaviour
{
    public enum Direction { North, East, South, West }  // Kierunek drzwi
    public Direction doorDirection;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {  // Taguj gracza "Player"
            // Tutaj logika zmiany pokoju, np. RoomManager.Instance.MoveToNextRoom(doorDirection);
            Debug.Log("Wejście przez drzwi: " + doorDirection);
        }
    }
}
