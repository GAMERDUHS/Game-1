using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PerlinTilemapChunkGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase[] tiles; // Array of tiles to use for generation
    public float[] tileThresholds; // Array of thresholds for each tile
    public int chunkSize = 16; // Size of each chunk
    public float scale = 10f; // Perlin noise scale
    public int seed = 42; // Seed for Perlin noise

    private Dictionary<Vector2Int, TileBase[]> generatedChunks = new Dictionary<Vector2Int, TileBase[]>();
    private Camera mainCamera;
    private Vector2Int lastChunkPos;

    void Start()
    {
        mainCamera = Camera.main;
        lastChunkPos = GetChunkPosition(mainCamera.transform.position);
        LoadChunksAround(lastChunkPos);
        
        // Register for tile placement/removal events
        Build.OnTilePlaced += HandleTilePlaced;
        Build.OnTileRemoved += HandleTileRemoved;
    }

    void OnDestroy()
    {
        // Unregister for tile placement/removal events
        Build.OnTilePlaced -= HandleTilePlaced;
        Build.OnTileRemoved -= HandleTileRemoved;
    }

    void Update()
    {
        Vector2Int currentChunkPos = GetChunkPosition(mainCamera.transform.position);

        if (currentChunkPos != lastChunkPos)
        {
            UnloadChunksAround(lastChunkPos);
            LoadChunksAround(currentChunkPos);
            lastChunkPos = currentChunkPos;
        }
    }

    Vector2Int GetChunkPosition(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / chunkSize),
            Mathf.FloorToInt(position.y / chunkSize)
        );
    }

    void LoadChunksAround(Vector2Int chunkPos)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int pos = new Vector2Int(chunkPos.x + x, chunkPos.y + y);
                if (!generatedChunks.ContainsKey(pos))
                {
                    GenerateChunk(pos);
                }
                else
                {
                    LoadChunk(pos);
                }
            }
        }
    }

    void UnloadChunksAround(Vector2Int chunkPos)
    {
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();

        foreach (var kvp in generatedChunks)
        {
            Vector2Int pos = kvp.Key;
            if (Mathf.Abs(pos.x - chunkPos.x) > 1 || Mathf.Abs(pos.y - chunkPos.y) > 1)
            {
                chunksToUnload.Add(pos);
            }
        }

        foreach (var pos in chunksToUnload)
        {
            UnloadChunk(pos);
        }
    }

    void GenerateChunk(Vector2Int chunkPos)
    {
        TileBase[] tilesInChunk = new TileBase[chunkSize * chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                float perlinValue = Mathf.PerlinNoise(
                    (chunkPos.x * chunkSize + x + seed) / scale,
                    (chunkPos.y * chunkSize + y + seed) / scale
                );

                TileBase selectedTile = GetTileFromPerlinValue(perlinValue);
                tilesInChunk[x + y * chunkSize] = selectedTile;
            }
        }

        generatedChunks[chunkPos] = tilesInChunk;
        LoadChunk(chunkPos);
    }

    TileBase GetTileFromPerlinValue(float perlinValue)
    {
        for (int i = 0; i < tileThresholds.Length; i++)
        {
            if (perlinValue <= tileThresholds[i])
            {
                return tiles[i];
            }
        }

        // Fallback in case of rounding errors or if the perlinValue is above all thresholds
        return tiles[tiles.Length - 1];
    }

    void UnloadChunk(Vector2Int chunkPos)
    {
        if (!generatedChunks.ContainsKey(chunkPos)) return;

        TileBase[] tilesInChunk = generatedChunks[chunkPos];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                tilemap.SetTile(tilePosition, null);
            }
        }
    }

    void LoadChunk(Vector2Int chunkPos)
    {
        if (!generatedChunks.ContainsKey(chunkPos)) return;

        TileBase[] tilesInChunk = generatedChunks[chunkPos];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                tilemap.SetTile(tilePosition, tilesInChunk[x + y * chunkSize]);
            }
        }
    }

    void HandleTilePlaced(Vector3Int gridPos, TileBase tile)
    {
        Vector2Int chunkPos = GetChunkPosition((Vector3)gridPos);
        int localX = gridPos.x % chunkSize;
        int localY = gridPos.y % chunkSize;

        if (localX < 0) localX += chunkSize;
        if (localY < 0) localY += chunkSize;

        if (generatedChunks.ContainsKey(chunkPos))
        {
            generatedChunks[chunkPos][localX + localY * chunkSize] = tile;
        }
    }

    void HandleTileRemoved(Vector3Int gridPos)
    {
        Vector2Int chunkPos = GetChunkPosition((Vector3)gridPos);
        int localX = gridPos.x % chunkSize;
        int localY = gridPos.y % chunkSize;

        if (localX < 0) localX += chunkSize;
        if (localY < 0) localY += chunkSize;

        if (generatedChunks.ContainsKey(chunkPos))
        {
            generatedChunks[chunkPos][localX + localY * chunkSize] = null;
        }
    }
}
