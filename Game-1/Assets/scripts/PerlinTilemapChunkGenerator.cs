using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PerlinTilemapChunkGenerator : MonoBehaviour
{
    [System.Serializable]
    public class Structure
    {
        public TileBase[] tiles; // Array of tiles for the structure
        public Vector3Int[] positions; // Relative positions for each tile in the structure
        public float probability; // Probability of the structure spawning
    }

    public Tilemap tilemap;
    public Tilemap structureTilemap; // Tilemap for placing structures
    public TileBase[] tiles; // Array of tiles to use for generation
    public float[] tileThresholds; // Array of thresholds for each tile
    public int chunkSize = 16; // Size of each chunk
    public float scale = 10f; // Perlin noise scale
    public int seed = 42; // Seed for Perlin noise

    public TileBase grassTile; // Tile representing grass
    public TileBase waterTile; // Tile representing water
    public Structure[] structures; // Array of structures to place on grass tiles

    private Dictionary<Vector2Int, TileChunkData> generatedChunks = new Dictionary<Vector2Int, TileChunkData>();
    private Camera mainCamera;
    private Vector2Int lastChunkPos;
    private System.Random random; // Seeded random number generator
    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>(); // Set of occupied tiles

    void Start()
    {
        mainCamera = Camera.main;
        lastChunkPos = GetChunkPosition(mainCamera.transform.position);
        random = new System.Random(seed); // Initialize the seeded random number generator
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
        List<StructureData> structuresInChunk = new List<StructureData>();

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

        generatedChunks[chunkPos] = new TileChunkData { tiles = tilesInChunk, structures = structuresInChunk };
        LoadChunk(chunkPos);

        // Attempt to place structure tiles on grass tiles
        PlaceStructureTilesOnGrass(chunkPos);
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

        TileBase[] tilesInChunk = generatedChunks[chunkPos].tiles;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                tilemap.SetTile(tilePosition, null);
                structureTilemap.SetTile(tilePosition, null); // Clear structure tilemap as well
            }
        }
    }

    void LoadChunk(Vector2Int chunkPos)
    {
        if (!generatedChunks.ContainsKey(chunkPos)) return;

        TileBase[] tilesInChunk = generatedChunks[chunkPos].tiles;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                tilemap.SetTile(tilePosition, tilesInChunk[x + y * chunkSize]);
            }
        }

        // Load structures
        List<StructureData> structuresInChunk = generatedChunks[chunkPos].structures;
        foreach (var structureData in structuresInChunk)
        {
            for (int i = 0; i < structureData.tiles.Length; i++)
            {
                Vector3Int structureTilePosition = structureData.position + structureData.relativePositions[i];
                structureTilemap.SetTile(structureTilePosition, structureData.tiles[i]);
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
            generatedChunks[chunkPos].tiles[localX + localY * chunkSize] = tile;
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
            generatedChunks[chunkPos].tiles[localX + localY * chunkSize] = null;
        }
    }

    void PlaceStructureTilesOnGrass(Vector2Int chunkPos)
    {
        if (!generatedChunks.ContainsKey(chunkPos)) return;

        TileBase[] tilesInChunk = generatedChunks[chunkPos].tiles;
        List<StructureData> structuresInChunk = generatedChunks[chunkPos].structures;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                if (tilesInChunk[x + y * chunkSize] == grassTile && !IsTileOccupied(tilePosition))
                {
                    // Use seeded random to decide if a structure should be placed
                    float structureRoll = (float)random.NextDouble();
                    foreach (var structure in structures)
                    {
                        if (structureRoll < structure.probability)
                        {
                            bool canPlace = true;

                            // Check if any of the structure tiles would overlap with occupied tiles
                            foreach (var relativePosition in structure.positions)
                            {
                                Vector3Int structureTilePosition = tilePosition + relativePosition;
                                if (IsTileOccupied(structureTilePosition))
                                {
                                    canPlace = false;
                                    break;
                                }
                            }

                            if (canPlace)
                            {
                                // Place structure tiles and mark them as occupied
                                StructureData structureData = new StructureData
                                {
                                    tiles = structure.tiles,
                                    position = tilePosition,
                                    relativePositions = structure.positions
                                };

                                structuresInChunk.Add(structureData);

                                for (int i = 0; i < structure.tiles.Length; i++)
                                {
                                    Vector3Int structureTilePosition = tilePosition + structure.positions[i];
                                    structureTilemap.SetTile(structureTilePosition, structure.tiles[i]);
                                    occupiedTiles.Add(structureTilePosition);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    bool IsTileOccupied(Vector3Int tilePosition)
    {
        if (occupiedTiles.Contains(tilePosition))
        {
            return true;
        }

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPosition = new Vector3Int(tilePosition.x + x, tilePosition.y + y, tilePosition.z);
                if (occupiedTiles.Contains(checkPosition))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Data class to store chunk data
    private class TileChunkData
    {
        public TileBase[] tiles;
        public List<StructureData> structures;
    }

    // Data class to store structure data
    private class StructureData
    {
        public TileBase[] tiles;
        public Vector3Int position;
        public Vector3Int[] relativePositions;
    }
}
