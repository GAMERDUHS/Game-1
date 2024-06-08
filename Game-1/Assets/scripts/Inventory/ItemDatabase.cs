using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public Item[] items;

    public Item GetItemByID(int id)
    {
        foreach (var item in items)
        {
            if (item.itemID == id)
                return item;
        }
        return null;
    }
}
