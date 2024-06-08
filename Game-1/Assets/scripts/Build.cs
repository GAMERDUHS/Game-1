using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class Build : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase tileToPlace;

    public static event Action<Vector3Int, TileBase> OnTilePlaced;
    public static event Action<Vector3Int> OnTileRemoved;

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
        OnTilePlaced?.Invoke(gridPos, tileToPlace);
    }

    void RemoveTile(Vector3Int gridPos)
    {
        tilemap.SetTile(gridPos, null);
        OnTileRemoved?.Invoke(gridPos);
    }
}
