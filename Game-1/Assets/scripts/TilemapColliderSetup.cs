using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapColliderSetup : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase waterTile;
    public Vector3Int[] waterTilePositions;

    void Start()
    {
        // Set tiles in the tilemap
        foreach (var pos in waterTilePositions)
        {
            tilemap.SetTile(pos, waterTile);
        }

        // Force update the tilemap to ensure colliders are generated
        tilemap.RefreshAllTiles();
    }
}
