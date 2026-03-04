using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("=== REFERENCJE ===")]
    public GameObject doorNorthPrefab;     // DoorTop prefab
    public GameObject doorSouthPrefab;     // DoorBottom
    public GameObject doorEastPrefab;      // DoorRight
    public GameObject doorWestPrefab;      // DoorLeft
    public GameObject shadowTopPrefab;
    public GameObject shadowBottomPrefab;
    public GameObject shadowLeftPrefab;
    public GameObject shadowRightPrefab;

    [Header("=== KTÓRE DRZWI ===")]
    public bool hasNorthDoor = true;
    public bool hasEastDoor = true;
    public bool hasSouthDoor = false;
    public bool hasWestDoor = true;

    private SpriteRenderer background;

    private void Start()
    {
        background = GetComponent<SpriteRenderer>();  // Bezpośrednio na Room!

        if (background == null) return;

        Bounds bounds = background.bounds;
        Vector2 center = transform.position;
        float offset = 0.05f;  // Dostosuj jeśli nie pasuje

        // === DRZWI ===
        if (hasNorthDoor) SpawnDoor(doorNorthPrefab, Door.Direction.North, center + Vector2.up * (bounds.extents.y - offset));
        if (hasEastDoor) SpawnDoor(doorEastPrefab, Door.Direction.East, center + Vector2.right * (bounds.extents.x - offset));
        if (hasSouthDoor) SpawnDoor(doorSouthPrefab, Door.Direction.South, center + Vector2.down * (bounds.extents.y - offset));
        if (hasWestDoor) SpawnDoor(doorWestPrefab, Door.Direction.West, center + Vector2.left * (bounds.extents.x - offset));

        // === CIENIE (tylko bez drzwi) ===
        if (!hasNorthDoor) SpawnShadow(shadowTopPrefab, center + Vector2.up * bounds.extents.y);
        if (!hasEastDoor) SpawnShadow(shadowRightPrefab, center + Vector2.right * bounds.extents.x);
        if (!hasSouthDoor) SpawnShadow(shadowBottomPrefab, center + Vector2.down * bounds.extents.y);
        if (!hasWestDoor) SpawnShadow(shadowLeftPrefab, center + Vector2.left * bounds.extents.x);
    }

    private void SpawnDoor(GameObject prefab, Door.Direction dir, Vector3 pos)
    {
        if (prefab != null)
        {
            GameObject door = Instantiate(prefab, pos, Quaternion.identity, transform);
            door.GetComponent<Door>().doorDirection = dir;
        }
    }

    private void SpawnShadow(GameObject prefab, Vector3 pos)
    {
        if (prefab != null)
            Instantiate(prefab, pos, Quaternion.identity, transform);
    }
}