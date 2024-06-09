using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PerlinTilemapChunkGenerator : MonoBehaviour
{
    public static PerlinTilemapChunkGenerator Instance { get; private set; }

    public Tilemap tilemap;
    public Tilemap waterTilemap;
    public TileBase[] tiles;
    public float[] tileThresholds;
    public int chunkSize = 16;
    public float scale = 10f;
    public int seed = 42;

    public GameObject[] structurePrefabs; // Array of structure prefabs
    public float[] structureSpawnChances; // Array of spawn chances for each structure
    public TileBase grassTile; // The tile considered as grass

    private Dictionary<Vector2Int, TileChunkData> generatedChunks = new Dictionary<Vector2Int, TileChunkData>();
    private Camera mainCamera;
    private Vector2Int lastChunkPos;
    private List<GameObject> activeStructures = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        lastChunkPos = GetChunkPosition(mainCamera.transform.position);
        LoadChunksAround(lastChunkPos);
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

        CleanupStructures(chunkPos);
    }

    void GenerateChunk(Vector2Int chunkPos)
    {
        TileBase[] tilesInChunk = new TileBase[chunkSize * chunkSize];
        bool[] occupiedTiles = new bool[chunkSize * chunkSize];
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

                // Attempt to spawn a structure only on grass tiles
                if (selectedTile == grassTile)
                {
                    for (int i = 0; i < structurePrefabs.Length; i++)
                    {
                        if (Random.value < structureSpawnChances[i])
                        {
                            Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                            StructureData structureData = ScriptableObject.CreateInstance<StructureData>();
                            structureData.position = tilePosition;
                            structureData.structureIndex = i;
                            structuresInChunk.Add(structureData);
                            occupiedTiles[x + y * chunkSize] = true;
                            break; // Spawn only one structure per tile
                        }
                    }
                }
            }
        }

        generatedChunks[chunkPos] = new TileChunkData { tiles = tilesInChunk, occupiedTiles = occupiedTiles, structures = structuresInChunk };
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

        return tiles[tiles.Length - 1];
    }

    void UnloadChunk(Vector2Int chunkPos)
    {
        if (!generatedChunks.ContainsKey(chunkPos)) return;

        TileBase[] tilesInChunk = generatedChunks[chunkPos].tiles;
        List<StructureData> structuresInChunk = generatedChunks[chunkPos].structures;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                tilemap.SetTile(tilePosition, null);
                waterTilemap.SetTile(tilePosition, null);
            }
        }

        // Destroy structures and remove from active structures list
        foreach (StructureData structureData in structuresInChunk)
        {
            Vector3 worldPosition = tilemap.CellToWorld(structureData.position) + tilemap.cellSize / 2;
            worldPosition.y -= 0.5f; // Subtract 0.5 from the y position
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.1f);
            foreach (Collider2D collider in colliders)
            {
                if (collider.CompareTag("Structure"))
                {
                    Destroy(collider.gameObject);
                    activeStructures.Remove(collider.gameObject);
                }
            }
        }
    }

    void LoadChunk(Vector2Int chunkPos)
    {
        if (!generatedChunks.ContainsKey(chunkPos)) return;

        TileBase[] tilesInChunk = generatedChunks[chunkPos].tiles;
        List<StructureData> structuresInChunk = generatedChunks[chunkPos].structures;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);
                TileBase tile = tilesInChunk[x + y * chunkSize];
                if (tile == tiles[tiles.Length - 1]) // Assuming last tile is water
                {
                    waterTilemap.SetTile(tilePosition, tile);
                }
                else
                {
                    tilemap.SetTile(tilePosition, tile);
                }
            }
        }

        // Spawn structures
        foreach (StructureData structureData in structuresInChunk)
        {
            activeStructures.Add(SpawnStructure(structureData.position, structureData.structureIndex));
        }
    }

    GameObject SpawnStructure(Vector3Int tilePosition, int structureIndex)
    {
        GameObject structurePrefab = structurePrefabs[structureIndex];
        Vector3 adjustedPosition = tilemap.CellToWorld(tilePosition) + tilemap.cellSize / 2;
        adjustedPosition.y -= 0.5f; // Subtract 0.5 from the y position
        GameObject structure = Instantiate(structurePrefab, adjustedPosition, Quaternion.identity);
        structure.tag = "Structure"; // Tag the structure for easier identification during unloading
        return structure;
    }

    void CleanupStructures(Vector2Int centerChunkPos)
    {
        int minX = (centerChunkPos.x - 1) * chunkSize;
        int maxX = (centerChunkPos.x + 2) * chunkSize;
        int minY = (centerChunkPos.y - 1) * chunkSize;
        int maxY = (centerChunkPos.y + 2) * chunkSize;

        // Destroy all active structures that are not in currently loaded chunks
        for (int i = activeStructures.Count - 1; i >= 0; i--)
        {
            GameObject structure = activeStructures[i];
            Vector3 position = structure.transform.position;
            Vector2Int structureChunkPos = GetChunkPosition(position);

            // Adjust position.y by adding 0.5 to account for previous subtraction
            position.y += 0.5f;

            if (position.x < minX || position.x >= maxX || position.y < minY || position.y >= maxY)
            {
                Destroy(structure);
                activeStructures.RemoveAt(i);
            }
        }
    }




    [System.Serializable]
    public class TileChunkData
    {
        public TileBase[] tiles;
        public bool[] occupiedTiles;
        public List<StructureData> structures;
    }

    [System.Serializable]
    public class StructureData : ScriptableObject
    {
        public Vector3Int position;
        public int structureIndex;
    }
}

