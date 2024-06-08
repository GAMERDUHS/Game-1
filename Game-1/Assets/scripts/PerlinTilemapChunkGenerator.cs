using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class PerlinTilemapChunkGenerator : MonoBehaviour
{
    [System.Serializable]
    public class Structure
    {
        public TileBase[] tiles;
        public Vector3Int[] positions;
        public float probability;
    }

    public Tilemap tilemap;
    public Tilemap structureTilemap;
    public Tilemap waterTilemap;
    public TileBase[] tiles;
    public float[] tileThresholds;
    public int chunkSize = 16;
    public float scale = 10f;
    public int seed = 42;

    public TileBase grassTile;
    public TileBase waterTile;
    public Structure[] structures;
    public GameObject emptyGameObjectPrefab;
    public Transform player;
    
    private HashSet<Vector3Int> destroyedStructures = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, StructureData> structurePositions = new Dictionary<Vector3Int, StructureData>();
    private Dictionary<Vector2Int, TileChunkData> generatedChunks = new Dictionary<Vector2Int, TileChunkData>();
    private Dictionary<Vector2Int, List<GameObject>> chunkEmptyObjects = new Dictionary<Vector2Int, List<GameObject>>();
    private Camera mainCamera;
    private Vector2Int lastChunkPos;
    private System.Random random;

    void Start()
    {
        mainCamera = Camera.main;
        lastChunkPos = GetChunkPosition(mainCamera.transform.position);
        random = new System.Random(seed);
        LoadDestroyedStructures();
        LoadStructures();
        LoadChunksAround(lastChunkPos);

        Build.OnTilePlaced += HandleTilePlaced;
        Build.OnTileRemoved += HandleTileRemoved;
    }

    void OnDestroy()
    {
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
        chunkEmptyObjects[chunkPos] = new List<GameObject>();
        LoadChunk(chunkPos);

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
                structureTilemap.SetTile(tilePosition, null);
                waterTilemap.SetTile(tilePosition, null);
            }
        }

        if (chunkEmptyObjects.ContainsKey(chunkPos))
        {
            foreach (GameObject emptyObject in chunkEmptyObjects[chunkPos])
            {
                Destroy(emptyObject);
            }
            chunkEmptyObjects.Remove(chunkPos);
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
                TileBase tile = tilesInChunk[x + y * chunkSize];
                if (tile == waterTile)
                {
                    waterTilemap.SetTile(tilePosition, tile);
                }
                else
                {
                    tilemap.SetTile(tilePosition, tile);
                }
            }
        }

        List<StructureData> structuresInChunk = generatedChunks[chunkPos].structures;
        foreach (var structureData in structuresInChunk)
        {
            for (int i = 0; i < structureData.tiles.Length; i++)
            {
                Vector3Int structureTilePosition = structureData.position + structureData.relativePositions[i];
                structureTilemap.SetTile(structureTilePosition, structureData.tiles[i]);
            }

            PlaceEmptyGameObjectAtCenter(structureData, chunkPos);
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

            if (structurePositions.TryGetValue(gridPos, out StructureData structureData))
            {
                RemoveEntireStructure(structureData);
            }
            destroyedStructures.Add(gridPos);  // Add to destroyed structures set
        }
    }

    void RemoveEntireStructure(StructureData structureData)
    {
        foreach (var tile in structureData.relativePositions)
        {
            Vector3Int structureTilePosition = structureData.position + tile;
            structureTilemap.SetTile(structureTilePosition, null);
            destroyedStructures.Add(structureTilePosition);  // Add each tile position to destroyed structures set
        }
        structurePositions.Remove(structureData.position);
    }

    void PlaceStructureTilesOnGrass(Vector2Int chunkPos)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + y, 0);

                if (tilemap.GetTile(tilePosition) == grassTile && !destroyedStructures.Contains(tilePosition))
                {
                    foreach (var structure in structures)
                    {
                        if (random.NextDouble() < structure.probability)
                        {
                            bool canPlaceStructure = true;

                            foreach (var relativePos in structure.positions)
                            {
                                Vector3Int structureTilePosition = tilePosition + relativePos;
                                if (tilemap.GetTile(structureTilePosition) != grassTile || structureTilemap.GetTile(structureTilePosition) != null || destroyedStructures.Contains(structureTilePosition))
                                {
                                    canPlaceStructure = false;
                                    break;
                                }
                            }

                            if (canPlaceStructure)
                            {
                                foreach (var relativePos in structure.positions)
                                {
                                    Vector3Int structureTilePosition = tilePosition + relativePos;
                                    structureTilemap.SetTile(structureTilePosition, structure.tiles[System.Array.IndexOf(structure.positions, relativePos)]);
                                }

                                StructureData structureData = new StructureData
                                {
                                    position = tilePosition,
                                    tiles = structure.tiles,
                                    relativePositions = structure.positions
                                };
                                structurePositions[tilePosition] = structureData;
                                generatedChunks[chunkPos].structures.Add(structureData);

                                if (!chunkEmptyObjects.ContainsKey(chunkPos))
                                {
                                    chunkEmptyObjects[chunkPos] = new List<GameObject>();
                                }

                                PlaceEmptyGameObjectAtCenter(structureData, chunkPos);

                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    void PlaceEmptyGameObjectAtCenter(StructureData structureData, Vector2Int chunkPos)
    {
        Vector3Int centerPosition = structureData.position + new Vector3Int(structureData.relativePositions.Length / 2, structureData.relativePositions.Length / 2, 0);
        GameObject emptyGameObject = Instantiate(emptyGameObjectPrefab, centerPosition, Quaternion.identity);

        if (!chunkEmptyObjects.ContainsKey(chunkPos))
        {
            chunkEmptyObjects[chunkPos] = new List<GameObject>();
        }

        chunkEmptyObjects[chunkPos].Add(emptyGameObject);
    }

    void SaveDestroyedStructures()
    {
        PlayerPrefs.SetString("DestroyedStructures", string.Join(";", destroyedStructures.Select(pos => $"{pos.x},{pos.y},{pos.z}").ToArray()));
        PlayerPrefs.Save();
    }

    void LoadDestroyedStructures()
    {
        destroyedStructures.Clear();
        string data = PlayerPrefs.GetString("DestroyedStructures", "");
        if (!string.IsNullOrEmpty(data))
        {
            foreach (var pos in data.Split(';'))
            {
                var coords = pos.Split(',');
                if (coords.Length == 3)
                {
                    destroyedStructures.Add(new Vector3Int(int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2])));
                }
            }
        }
    }

    void SaveStructures()
    {
        PlayerPrefs.SetString("Structures", JsonUtility.ToJson(new SerializableDictionary<Vector3Int, StructureData>(structurePositions)));
        PlayerPrefs.Save();
    }

    void LoadStructures()
    {
        structurePositions.Clear();
        string data = PlayerPrefs.GetString("Structures", "");
        if (!string.IsNullOrEmpty(data))
        {
            SerializableDictionary<Vector3Int, StructureData> loadedStructures = JsonUtility.FromJson<SerializableDictionary<Vector3Int, StructureData>>(data);
            structurePositions = new Dictionary<Vector3Int, StructureData>(loadedStructures);
        }
    }

    void OnApplicationQuit()
    {
        SaveDestroyedStructures();
        SaveStructures();
    }

    void Awake()
    {
        LoadDestroyedStructures();
        LoadStructures();
    }

    [System.Serializable]
    public class TileChunkData
    {
        public TileBase[] tiles;
        public List<StructureData> structures;
    }

    [System.Serializable]
    public class StructureData
    {
        public Vector3Int position;
        public TileBase[] tiles;
        public Vector3Int[] relativePositions;
    }

    [System.Serializable]
    private class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        public SerializableDictionary() : base() { }

        public SerializableDictionary(Dictionary<TKey, TValue> dictionary) : base(dictionary) { }

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
                throw new System.Exception("There are unequal amounts of keys and values after deserialization.");

            for (int i = 0; i < keys.Count; i++)
                this.Add(keys[i], values[i]);
        }
    }
}
