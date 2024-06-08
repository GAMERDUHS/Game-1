using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class TilemapInteraction : MonoBehaviour
{
    public Tilemap tilemap;
    public Camera mainCamera;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button clicked
        {
            // Check if the click is over a UI element
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = tilemap.WorldToCell(mouseWorldPos);

            Debug.Log("Tilemap clicked at position: " + coordinate);

            if (Uimanager.Instance.selectedSlot != null && Uimanager.Instance.selectedSlot.GetItem().itemID == 0)
            {
                TileBase clickedTile = tilemap.GetTile(coordinate);
                if (clickedTile != null)
                {
                    tilemap.SetTile(coordinate, null);
                    Debug.Log("Tile destroyed at position: " + coordinate);
                }
                else
                {
                    Debug.Log("No tile at position: " + coordinate);
                }
            }
        }
    }
}
