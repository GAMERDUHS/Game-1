using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;
    public int itemID;
    public string description;
    public GameObject prefab; // Prefab to spawn
}
