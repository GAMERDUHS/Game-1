using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Uimanager : MonoBehaviour
{
    public static Uimanager Instance; // Singleton instance

    public Slot[] slots;               // Array of slots
    public ItemDatabase itemDatabase;  // Reference to the item database
    public Button spawnButton;         // The button to press to spawn the item
    public GameObject draggableItemPrefab; // Prefab for the draggable item
    public Image selectedItemImage;    // UI element to display the currently selected item
    public Transform player;           // Reference to the player transform
    public Vector3 spawnOffset = new Vector3(0, 0.5f, 0); // Offset for spawning prefab

    private Slot _selectedSlot;        // Private field for currently selected slot
    private DraggableItem draggableItem; // Instance of the draggable item
    private GameObject currentSpawnedPrefab; // Reference to the currently spawned prefab

    public static Uimanager UIManagerInstance { get; private set; } // Rename the property to avoid conflict

    private GameObject _currentSpawnedPrefab;

    public GameObject CurrentSpawnedPrefab
    {
        get { return _currentSpawnedPrefab; }
        set { _currentSpawnedPrefab = value; }
    }

    public Slot selectedSlot // Public property to access the selected slot
    {
        get => _selectedSlot;
        set
        {
            _selectedSlot = value;
            UpdateSelectedItemImage();
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
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

        // Initialize all slots to be empty
        foreach (var slot in slots)
        {
            slot.ClearItem();
        }

        if (selectedItemImage != null)
        {
            selectedItemImage.enabled = false;
        }
    }

    void SpawnItem()
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("Slots array is not initialized or empty!");
            return;
        }

        // Example: Spawn an item with ID 1
        Item itemToSpawn = itemDatabase.GetItemByID(1);

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
        SpawnPrefabAsChildOfPlayer(slot.GetItem());
    }

    public void DeselectItem()
    {
        selectedSlot = null;
        Debug.Log("Deselected item");

        // Destroy the currently spawned prefab
        if (currentSpawnedPrefab != null)
        {
            Destroy(currentSpawnedPrefab);
            currentSpawnedPrefab = null;
        }
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

    private void UpdateSelectedItemImage()
    {
        if (selectedSlot != null && selectedSlot.GetItem() != null)
        {
            selectedItemImage.sprite = selectedSlot.GetItem().itemSprite;
            selectedItemImage.enabled = true;
        }
        else
        {
            selectedItemImage.enabled = false;
        }
    }

    private void SpawnPrefabAsChildOfPlayer(Item item)
    {
        if (item.prefab != null && player != null)
        {
            // Destroy the currently spawned prefab if it exists
            if (currentSpawnedPrefab != null)
            {
                Destroy(currentSpawnedPrefab);
            }

            Vector3 spawnPosition = player.position + spawnOffset;
            currentSpawnedPrefab = Instantiate(item.prefab, spawnPosition, Quaternion.identity, player);

            // Add a BoxCollider2D if it doesn't have one
            if (currentSpawnedPrefab.GetComponent<BoxCollider2D>() == null)
            {
                currentSpawnedPrefab.AddComponent<BoxCollider2D>();
            }

            Debug.Log($"Spawned prefab: {item.itemName} at position {spawnPosition} as a child of {player.name}.");
        }
        else
        {
            Debug.LogWarning("Item prefab or player is null");
        }
    }
}
