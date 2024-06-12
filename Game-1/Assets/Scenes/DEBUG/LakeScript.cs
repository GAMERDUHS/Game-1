using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    public Tilemap tilemap; // Reference to the tilemap
    public RuleTile lakeTile; // Reference to the RuleTile representing the lake
    public Tile grassTile; // Reference to the Tile representing grass
    public GameObject player; // Reference to the player GameObject

    public int chunkSize = 16; // Size of each chunk
    public int renderDistance = 2; // Render distance in chunks
    public int seed; // Seed for consistent lake generation

    private const int MinLakeTiles = 4; // Minimum number of lake tiles required
    private const float LakeChance = 0.5f; // Chance for a chunk to contain a lake
    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>(); // Dictionary to store loaded chunks

    void Start()
    {
        ManageChunkLoading();
    }

    void Update()
    {
        ManageChunkLoading();
    }

    void ManageChunkLoading()
    {
        // Calculate the player's chunk coordinates
        Vector2Int playerChunkCoords = GetChunkCoordinates(player.transform.position);

        // Iterate through the chunks within the render distance
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2Int chunkCoords = new Vector2Int(playerChunkCoords.x + x, playerChunkCoords.y + y);

                // Check if the chunk is loaded
                if (!loadedChunks.ContainsKey(chunkCoords))
                {
                    // Load the chunk
                    LoadChunk(chunkCoords);
                }
            }
        }

        // Unload chunks that are outside the render distance
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        foreach (var loadedChunk in loadedChunks)
        {
            Vector2Int chunkCoords = loadedChunk.Key;
            if (Vector2Int.Distance(chunkCoords, playerChunkCoords) > renderDistance)
            {
                chunksToUnload.Add(chunkCoords);
            }
        }

        // Unload the chunks
        foreach (Vector2Int chunkCoords in chunksToUnload)
        {
            UnloadChunk(chunkCoords);
        }
    }

    void LoadChunk(Vector2Int chunkCoords)
    {
        // Create a new Chunk instance
        Chunk chunk = new Chunk(chunkCoords.x, chunkCoords.y, chunkSize);

        // Generate the chunk
        chunk.GenerateChunk(this);

        // Add the chunk to the loaded chunks dictionary
        loadedChunks.Add(chunkCoords, chunk);

        // Fill empty tiles with grass
        FillEmptyTilesWithGrass(chunkCoords);
    }

    void UnloadChunk(Vector2Int chunkCoords)
    {
        // Check if the chunk is loaded
        if (loadedChunks.ContainsKey(chunkCoords))
        {
            // Remove the chunk from the loaded chunks dictionary
            loadedChunks.Remove(chunkCoords);

            // Clear the tiles of the chunk from the tilemap
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    tilemap.SetTile(new Vector3Int(chunkCoords.x * chunkSize + x, chunkCoords.y * chunkSize + y, 0), null);
                }
            }
        }
    }

    void FillEmptyTilesWithGrass(Vector2Int chunkCoords)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = new Vector3Int(chunkCoords.x * chunkSize + x, chunkCoords.y * chunkSize + y, 0);
                if (tilemap.GetTile(tilePosition) == null)
                {
                    tilemap.SetTile(tilePosition, grassTile);
                }
            }
        }
    }

    Vector2Int GetChunkCoordinates(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = Mathf.FloorToInt(position.y / chunkSize); // Use position.y for the y-axis
        return new Vector2Int(x, y);
    }

    public void GenerateLake(int chunkX, int chunkY, int lakeWidth, int lakeHeight, int smoothness)
    {
        Random.InitState(seed + chunkX * 1000 + chunkY); // Set the seed based on chunk coordinates

        bool[,] lakeShape;

        do
        {
            // Create a 2D array to represent the lake shape
            lakeShape = new bool[lakeWidth, lakeHeight];

            // Randomly fill the array
            for (int x = 0; x < lakeWidth; x++)
            {
                for (int y = 0; y < lakeHeight; y++)
                {
                    lakeShape[x, y] = Random.value > 0.5f;
                }
            }

            // Smooth the lake shape
            for (int i = 0; i < smoothness; i++)
            {
                lakeShape = SmoothLakeShape(lakeShape, lakeWidth, lakeHeight);
            }

            // Ensure no tile is isolated or has fewer than 2 neighbors
            lakeShape = ValidateLakeShape(lakeShape, lakeWidth, lakeHeight);

        } while (CountLakeTiles(lakeShape, lakeWidth, lakeHeight) < MinLakeTiles);

        // Draw the lake on the tilemap
        for (int x = 0; x < lakeWidth; x++)
        {
            for (int y = 0; y < lakeHeight; y++)
            {
                tilemap.SetTile(new Vector3Int(chunkX * chunkSize + x, chunkY * chunkSize + y, 0), lakeShape[x, y] ? lakeTile : grassTile);
            }
        }
    }

    bool[,] SmoothLakeShape(bool[,] lakeShape, int width, int height)
    {
        bool[,] newShape = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbors = CountNeighbors(lakeShape, x, y, width, height);
                newShape[x, y] = neighbors > 4 || (neighbors == 4 && lakeShape[x, y]);
            }
        }

        return newShape;
    }

    bool[,] ValidateLakeShape(bool[,] lakeShape, int width, int height)
    {
        bool[,] newShape = (bool[,])lakeShape.Clone();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (lakeShape[x, y])
                {
                    int neighbors = CountNeighbors(lakeShape, x, y, width, height);
                    if (neighbors < 2)
                    {
                        newShape[x, y] = false;
                    }
                }
            }
        }

        return RemoveIsolatedTiles(newShape, width, height);
    }

    bool[,] RemoveIsolatedTiles(bool[,] lakeShape, int width, int height)
    {
        bool[,] newShape = (bool[,])lakeShape.Clone();
        bool changedShape;

        do
        {
            changedShape = false;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (lakeShape[x, y])
                    {
                        int neighbors = CountNeighbors(lakeShape, x, y, width, height);
                        if (neighbors < 2)
                        {
                            newShape[x, y] = false;
                            changedShape = true;
                        }
                    }
                }
            }
            lakeShape = (bool[,])newShape.Clone();
        } while (changedShape);

        return newShape;
    }

    int CountNeighbors(bool[,] lakeShape, int x, int y, int width, int height)
    {
        int count = 0;

        for (int nx = x - 1; nx <= x + 1; nx++)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (nx != x || ny != y)
                    {
                        if (lakeShape[nx, ny])
                        {
                            count++;
                        }
                    }
                }
            }
        }

        return count;
    }

    int CountLakeTiles(bool[,] lakeShape, int width, int height)
    {
        int count = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (lakeShape[x, y])
                {
                    count++;
                }
            }
        }

        return count;
    }

    class Chunk
    {
        public int x, y; // Chunk coordinates
        public int size; // Size of the chunk

        public Chunk(int x, int y, int size)
        {
            this.x = x;
            this.y = y;
            this.size = size;
        }

        public void GenerateChunk(WorldGenerator worldGenerator)
        {
            Random.InitState(worldGenerator.seed + x * 1000 + y); // Set the seed based on chunk coordinates

            // Determine if this chunk will contain a lake
            if (Random.value <= LakeChance)
            {
                // Generate a lake for this chunk
                int lakeWidth = Random.Range(size / 2, size);
                int lakeHeight = Random.Range(size / 2, size);
                int smoothness = Random.Range(3, 8);
                worldGenerator.GenerateLake(x, y, lakeWidth, lakeHeight, smoothness);
            }
            else
            {
                // Fill the chunk with grass
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        worldGenerator.tilemap.SetTile(new Vector3Int(x * size + i, y * size + j, 0), worldGenerator.grassTile);
                    }
                }
            }
        }
    }
}
