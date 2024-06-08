using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Uimanager : MonoBehaviour
{
    public static Uimanager Instance; // Singleton instance

    public Slot[] slots;              // Array of slots
    public ItemDatabase itemDatabase; // Reference to the item database
    public Button spawnButton;        // The button to press to spawn the item
    public GameObject draggableItemPrefab; // Prefab for the draggable item
    private Slot selectedSlot;        // Currently selected slot
    private DraggableItem draggableItem; // Instance of the draggable item

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (spawnButton == null)
        {
            Debug.LogError("Spawn button is not assigned in the Inspector!");
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogError("Item database is not assigned in the Inspector!");
            return;
        }

        spawnButton.onClick.AddListener(SpawnItem);

        // Instantiate the draggable item and hide it initially
        GameObject draggableItemObject = Instantiate(draggableItemPrefab, transform);
        draggableItem = draggableItemObject.GetComponent<DraggableItem>();
        draggableItem.SetImage(null); // Hide the image initially
        draggableItem.gameObject.SetActive(false); // Ensure the draggable item is inactive initially

        // Initialize all slots to be empty and text to "Blank"
        foreach (var slot in slots)
        {
            slot.ClearItem();
        }
    }

    void SpawnItem()
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("Slots array is not initialized or empty!");
            return;
        }

        // Example: Spawn an item with ID 0
        Item itemToSpawn = itemDatabase.GetItemByID(0);

        if (itemToSpawn == null)
        {
            Debug.LogError("Item with the specified ID not found in the database!");
            return;
        }

        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
            {
                slot.SetItem(itemToSpawn, 1); // Set the item with count 1
                break; // Exit the loop once the item is placed
            }
        }
    }

    public void SelectItem(Slot slot)
    {
        selectedSlot = slot;
        // Perform any additional logic for selecting an item
        Debug.Log("Selected item: " + slot.GetItem().itemName);
    }

    public bool IsDragging { get; private set; }

    public void StartDragging(Slot slot)
    {
        IsDragging = true;
        draggableItem.SetImage(slot.GetItem().itemSprite);
        draggableItem.transform.position = Input.mousePosition;
        draggableItem.gameObject.SetActive(true); // Show the draggable item
        selectedSlot = slot;
    }

    public void Dragging(PointerEventData eventData)
    {
        draggableItem.transform.position = Input.mousePosition;
    }

    public void EndDragging(PointerEventData eventData)
    {
        IsDragging = false;
        draggableItem.SetImage(null); // Hide the draggable item
        draggableItem.gameObject.SetActive(false); // Hide the draggable item

        Slot targetSlot = GetSlotUnderPointer(eventData);
        if (targetSlot != null && targetSlot != selectedSlot)
        {
            Item draggedItem = selectedSlot.GetItem();
            int itemCount = selectedSlot.ItemCount; // Use the public property
            selectedSlot.ClearItem();
            targetSlot.SetItem(draggedItem, itemCount);
        }
    }

    private Slot GetSlotUnderPointer(PointerEventData eventData)
    {
        foreach (var slot in slots)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(slot.GetComponent<RectTransform>(), eventData.position))
            {
                return slot;
            }
        }
        return null;
    }
}
