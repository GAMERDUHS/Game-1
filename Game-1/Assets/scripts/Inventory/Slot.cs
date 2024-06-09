using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Image itemImage;  // UI element to display the item sprite
    public Text itemCountText; // UI element to display the item count
    private int _itemCount;    // Count of items in the slot
    private Item currentItem; // Reference to the current item in the slot

    void Start()
    {
        ClearItem(); // Ensure the slot is cleared and text is set to "Blank" initially
    }

    public bool IsEmpty() => currentItem == null;

    public void SetItem(Item item, int count)
    {
        currentItem = item;
        itemImage.sprite = item.itemSprite;
        itemImage.enabled = true;
        ItemCount = count; // Use the public property
    }

    public void ClearItem()
    {
        currentItem = null;
        itemImage.sprite = null;
        itemImage.enabled = false;
        ItemCount = 0; // Use the public property
    }

    public Item GetItem() => currentItem;

    public int ItemCount
    {
        get => _itemCount;
        set
        {
            _itemCount = value;
            UpdateItemCountText();
        }
    }

    private void UpdateItemCountText()
    {
        if (itemCountText != null)
        {
            itemCountText.text = _itemCount > 0 ? _itemCount.ToString() : "";
        }
    }

    public void OnPointerClick(PointerEventData eventData) => OnClick();

    private void OnClick()
    {
        if (!IsEmpty())
        {
            Uimanager.Instance.SelectItem(this);
            Debug.Log("Slot clicked: " + currentItem.itemName);
        }
        else
        {
            Uimanager.Instance.DeselectItem();
            Debug.Log("Slot clicked: empty slot");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsEmpty())
        {
            Uimanager.Instance.StartDragging(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsEmpty() && Uimanager.Instance.IsDragging)
        {
            Uimanager.Instance.Dragging(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsEmpty() && Uimanager.Instance.IsDragging)
        {
            Uimanager.Instance.EndDragging(eventData);
        }
    }
}
