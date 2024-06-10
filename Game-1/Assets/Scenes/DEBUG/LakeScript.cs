using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PerlinNoiseLakeGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase lakeTile;
    public TileBase grassTile;
    public float perlinScale = 0.1f;
    public float threshold = 0.5f;
    public Vector2Int chunkSize = new Vector2Int(10, 10);
    public int renderDistance = 3;
    public int seed = 0;
    public int minLakeTiles = 4;
    public int minLakeWidth = 2;
    public int minLakeHeight = 2;

    private Vector2Int currentChunkPosition = Vector2Int.zero;
    private Dictionary<Vector2Int, bool> generatedChunks = new Dictionary<Vector2Int, bool>();
    private List<Vector2Int> loadedChunks = new List<Vector2Int>();
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        GenerateChunksAroundCamera();
    }

    void Update()
    {
        Vector2Int newChunkPosition = GetCameraChunkPosition();
        if (newChunkPosition != currentChunkPosition)
        {
            currentChunkPosition = newChunkPosition;
            GenerateChunksAroundCamera();
        }
    }

    Vector2Int GetCameraChunkPosition()
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        return new Vector2Int(
            Mathf.FloorToInt(cameraPosition.x / chunkSize.x),
            Mathf.FloorToInt(cameraPosition.y / chunkSize.y)
        );
    }

    void GenerateChunksAroundCamera()
    {
        Vector2Int cameraChunkPosition = GetCameraChunkPosition();
        List<Vector2Int> newLoadedChunks = new List<Vector2Int>();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2Int chunkPosition = new Vector2Int(
                    cameraChunkPosition.x + x,
                    cameraChunkPosition.y + y
                );

                if (!generatedChunks.ContainsKey(chunkPosition))
                {
                    GenerateChunk(chunkPosition);
                    generatedChunks[chunkPosition] = true;
                }
                newLoadedChunks.Add(chunkPosition);
            }
        }

        UnloadDistantChunks(newLoadedChunks);
        loadedChunks = newLoadedChunks;
    }

    void UnloadDistantChunks(List<Vector2Int> newLoadedChunks)
    {
        foreach (var chunk in loadedChunks)
        {
            if (!newLoadedChunks.Contains(chunk))
            {
                UnloadChunk(chunk);
            }
        }
    }

    void UnloadChunk(Vector2Int chunkPosition)
    {
        Vector2Int start = new Vector2Int(
            chunkPosition.x * chunkSize.x,
            chunkPosition.y * chunkSize.y
        );

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                Vector3Int pos = new Vector3Int(start.x + x, start.y + y, 0);
                tilemap.SetTile(pos, null);
            }
        }

        generatedChunks.Remove(chunkPosition);
    }

    void GenerateChunk(Vector2Int chunkPosition)
    {
        Vector2Int start = new Vector2Int(
            chunkPosition.x * chunkSize.x,
            chunkPosition.y * chunkSize.y
        );

        float[,] noiseMap = GeneratePerlinNoiseMap(chunkSize.x, chunkSize.y, start.x, start.y, perlinScale);
        bool[,] lakeMap = GenerateLakeMap(noiseMap);

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                Vector3Int pos = new Vector3Int(start.x + x, start.y + y, 0);
                if (lakeMap[x, y])
                {
                    tilemap.SetTile(pos, lakeTile);
                }
                else
                {
                    tilemap.SetTile(pos, grassTile);
                }
            }
        }

        FixSingleTilesAndEnforceMinimumSize(start);
    }

    float[,] GeneratePerlinNoiseMap(int width, int height, int offsetX, int offsetY, float scale)
    {
        float[,] noiseMap = new float[width, height];
        System.Random prng = new System.Random(seed);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float sampleX = (x + offsetX) * scale + prng.Next(-10000, 10000);
                float sampleY = (y + offsetY) * scale + prng.Next(-10000, 10000);
                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = noiseValue;
            }
        }

        return noiseMap;
    }

    bool[,] GenerateLakeMap(float[,] noiseMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);
        bool[,] lakeMap = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                lakeMap[x, y] = noiseMap[x, y] > threshold;
            }
        }

        return lakeMap;
    }

    void FixSingleTilesAndEnforceMinimumSize(Vector2Int start)
    {
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                Vector3Int pos = new Vector3Int(start.x + x, start.y + y, 0);
                TileBase tile = tilemap.GetTile(pos);

                if (tile == lakeTile)
                {
                    int connectingTiles = GetConnectingTilesCount(pos);
                    if (connectingTiles < 2)
                    {
                        tilemap.SetTile(pos, grassTile);
                        UpdateSurroundingTiles(pos);
                    }
                    else
                    {
                        bool isValidLake = CheckMinimumSize(pos);
                        if (!isValidLake)
                        {
                            tilemap.SetTile(pos, grassTile);
                            UpdateSurroundingTiles(pos);
                        }
                    }
                }
            }
        }
    }

    int GetConnectingTilesCount(Vector3Int pos)
    {
        int count = 0;
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
        };

        foreach (var direction in directions)
        {
            if (tilemap.GetTile(pos + direction) == lakeTile)
            {
                count++;
            }
        }

        return count;
    }

    void UpdateSurroundingTiles(Vector3Int pos)
    {
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
        };

        foreach (var direction in directions)
        {
            Vector3Int neighborPos = pos + direction;
            TileBase neighborTile = tilemap.GetTile(neighborPos);

            if (neighborTile == lakeTile)
            {
                int connectingTiles = GetConnectingTilesCount(neighborPos);
                if (connectingTiles < 2)
                {
                    tilemap.SetTile(neighborPos, grassTile);
                    UpdateSurroundingTiles(neighborPos);
                }
                else
                {
                    bool isValidLake = CheckMinimumSize(neighborPos);
                    if (!isValidLake)
                    {
                        tilemap.SetTile(neighborPos, grassTile);
                        UpdateSurroundingTiles(neighborPos);
                    }
                }
            }
        }
    }

    bool CheckMinimumSize(Vector3Int pos)
    {
        int width = 1;
        int height = 1;

        int x = pos.x;
        while (tilemap.GetTile(new Vector3Int(x + 1, pos.y, 0)) == lakeTile)
        {
            width++;
            x++;
        }

        x = pos.x;
        while (tilemap.GetTile(new Vector3Int(x - 1, pos.y, 0)) == lakeTile)
        {
            width++;
            x--;
        }

        int y = pos.y;
        while (tilemap.GetTile(new Vector3Int(pos.x, y + 1, 0)) == lakeTile)
        {
            height++;
            y++;
        }

        y = pos.y;
        while (tilemap.GetTile(new Vector3Int(pos.x, y - 1, 0)) == lakeTile)
        {
            height++;
            y--;
        }

        return width >= minLakeWidth && height >= minLakeHeight;
    }
}