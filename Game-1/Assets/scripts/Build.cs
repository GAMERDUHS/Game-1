using UnityEngine;
using UnityEngine.Tilemaps;

public class Build : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase tileToPlace;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mousePos);

            PlaceTile(gridPos);
        }

        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mousePos);

            RemoveTile(gridPos);
        }
    }

    void PlaceTile(Vector3Int gridPos)
    {
        tilemap.SetTile(gridPos, tileToPlace);
    }

    void RemoveTile(Vector3Int gridPos)
    {
        tilemap.SetTile(gridPos, null);
    }
}
