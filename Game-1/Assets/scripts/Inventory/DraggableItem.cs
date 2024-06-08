using UnityEngine;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour
{
    public Image itemImage; // UI element to display the item sprite

    void Start()
    {
        SetImage(null); // Hide the image initially
    }

    public void SetImage(Sprite sprite)
    {
        itemImage.sprite = sprite;
        itemImage.enabled = sprite != null; // Enable the image only if there is a sprite
    }
}
