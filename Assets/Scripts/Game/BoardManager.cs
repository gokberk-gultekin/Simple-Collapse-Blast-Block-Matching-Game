using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Prefab & Sprites")] // Inspector label
    public GameObject blockPrefab;
    public BlockSpriteColorSet[] blockSpriteColorSet;

    private Camera mainCamera; // Cache camera for optimization

    // Serialized struct shown in the Inspector
    [System.Serializable]
    public struct BlockSpriteColorSet
    {
        public Sprite iconDef;
        public Sprite iconFirst;
        public Sprite iconSecond;
        public Sprite iconThird;
    }

    // Grid configuration values
    private int cols;
    private int rows;
    private int colorCount;

    // Game state arrays
    private Block[,] board;
    private bool[,] visited; // Array for tracking visited cells during match detection

    // Object pooling for optimization
    private Queue<Block> blockPool = new Queue<Block>();
    private const int INITIAL_POOL_SIZE = 20; // Initial pool size for pre-allocation

    // Threshold values for icon swapping
    private int thresholdA;
    private int thresholdB;
    private int thresholdC;

    // Reusable containers to prevent runtime allocations
    private List<Block> tempMatchList = new List<Block>();
    private List<Block> tempBlockList = new List<Block>();
    private Queue<(int, int)> bfsQueue = new Queue<(int, int)>();


    void Awake()
    {
        mainCamera = Camera.main;
    }

    void OnEnable()
    {
        InputHandler.OnBlockClicked += HandleMatch;
    }

    void OnDisable()
    {
        InputHandler.OnBlockClicked -= HandleMatch;
    }

    void Start()
    {
        InitializeSettings();
        InitializePool();
        GenerateGrid();
        UpdateAllIcons();
        CenterCamera();
    }

    void InitializeSettings()
    {
        cols = MainMenu.cols;
        rows = MainMenu.rows;
        colorCount = MainMenu.colors;

        if (colorCount == 1)
        {
            thresholdA = 4;
            thresholdB = 7;
            thresholdC = 9;
        }

        else
        {
            // Dynamic icon swap thresholds based on board size and color density
            float density = (float)(cols * rows) / colorCount; // Average number of blocks per color
            thresholdA = 5;
            thresholdB = 5 + (int)(density * 0.1f);
            thresholdC = 6 + (int)(density * 0.2f);
        }

        // Allocate reusable arrays once
        board = new Block[cols, rows];
        visited = new bool[cols, rows];
    }

    void InitializePool()
    {
        // Pre-fill the obj pool to avoid runtime instantiation for optimization
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            GameObject obj = Instantiate(blockPrefab);
            obj.SetActive(false);
            obj.transform.parent = transform;
            blockPool.Enqueue(obj.GetComponent<Block>());
        }
    }

    void GenerateGrid()
    {
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                CreateBlock(x, y);
            }
        }

        // Handle potential initial deadlock situations
        if (IsDeadlocked())
        {
            ShuffleBoard();
        }
    }

    void UpdateAllIcons()
    {
        ClearVisited();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Block b = board[x, y];
                if (b == null || visited[x, y])
                {
                    continue;
                }
                // Find all connected blocks of the same color starting from this block
                tempMatchList.Clear();
                FindMatches(b, tempMatchList);

                // Update block icons based on the size of the matched group
                UpdateIconForGroup(tempMatchList);
            }
        }
    }

    void CenterCamera()
    {
        // Calculate grid center in world space
        float xCenter = (cols - 1) / 2f;
        float yCenter = (rows - 1) / 2f;

        // Position the camera at the grid center
        mainCamera.transform.position = new Vector3(xCenter, yCenter, -10f);

        // Screen aspect ratio for proper scaling
        float aspectRatio = (float)Screen.width / Screen.height;
        
        // Extra space around the grid to prevent blocks from touching screen edges
        float padding = 0.25f; 

        // Required camera size to fit the grid
        float verticalSize = (rows / 2f) + padding;
        float horizontalSize = ((cols / 2f) + padding) / aspectRatio;

        // Ensure the entire grid is visible
        mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    // Public API to handle a match
    public void HandleMatch(Block startBlock)
    {
        if (startBlock == null)
        {
            return;
        }

        tempMatchList.Clear(); // Clear reusable list before use
        ClearVisited(); // Reset visited grid

        FindMatches(startBlock, tempMatchList);
        if (tempMatchList.Count >= 2)
        {
            // Return blocks to pool instead of destroying
            foreach (Block b in tempMatchList)
            {
                board[b.col, b.row] = null;
                ReturnBlockToPool(b);
            }

            RefillBoard();
        }
    }

    /** Object pool management*/
    Block GetBlockFromPool() 
    { 
        if (blockPool.Count > 0)
        {
            Block block = blockPool.Dequeue();
            block.gameObject.SetActive(true);
            return block;
        }
        
        // Creates a new obj if the pool is empty
        GameObject newObj = Instantiate(blockPrefab);
        newObj.transform.SetParent(transform);

        return newObj.GetComponent<Block>();
    }

    void ReturnBlockToPool(Block block)
    {
        block.gameObject.SetActive(false);
        blockPool.Enqueue(block);
    }

    void CreateBlock(int x, int y)
    {
        int randColor = Random.Range(0, colorCount);
        
        Block block = GetBlockFromPool();

        block.transform.position = new Vector3(x, y, 0);

        Sprite initSprite = blockSpriteColorSet[randColor].iconDef; // Initial sprite (default state)
        block.Init(y, x, randColor, initSprite);

        board[x, y] = block;
    }

    void RefillBoard()
    {
        ApplyGravity(); // Let blocks fall into empty cells
        
        SpawnNewBlocks();

        // Update block icon sprites according to the group size
        UpdateAllIcons(); 

        if (IsDeadlocked())
        {
            ShuffleBoard();
        }
    }

    void ApplyGravity()
    {
        for (int x = 0; x < cols; x++) 
        {
            int nextRow = 0;

            for (int y = 0; y < rows; y++) 
            {
                Block current = board[x, y];
                if (current == null)
                {
                    continue;
                }

                if (y != nextRow)
                {
                    // Move the block down to the next empty row
                    board[x, nextRow] = current;
                    board[x, y] = null;

                    // Update block metadata
                    current.row = nextRow;
                    current.col = x;

                    // Update visual position
                    current.transform.position = new Vector3(x, nextRow, 0);
                }

                // Move pointer to the next empty row
                nextRow++;
            }
        }
    }

    void SpawnNewBlocks()
    {
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (board[x, y] == null) 
                { 
                    CreateBlock(x, y);
                }
            }
        }
    }

    // Detect deadlock situation
    bool IsDeadlocked() 
    {
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Block current = board[x, y];
                if (current == null)
                {
                    continue;
                }

                // Check right neighbor for possible match
                if (x < cols - 1)
                {
                    Block right = board[x + 1, y];
                    if (right != null && right.colorId == current.colorId) 
                    {
                        return false;
                    }
                }

                // Check upper neighbor for possible match
                if (y < rows - 1)
                {
                    Block up = board[x, y + 1];
                    if (up != null && up.colorId == current.colorId) 
                    { 
                        return false; 
                    }
                }
            }
        }
        
        return true;
    }

    void ShuffleBoard()
    {
        const int MAX_ATTEMPTS = 10;
        int attempt = 0;
        float startTime = Time.realtimeSinceStartup;
        const float TIMEOUT = 2f;

        while (attempt < MAX_ATTEMPTS && IsDeadlocked())
        {
            if (Time.realtimeSinceStartup - startTime > TIMEOUT)
            {
                Debug.LogWarning("Shuffle timeout! Unplayable grid values.");
                break; // Safety exit to avoid infinite loops
            }


            PerformShuffle();
            attempt++;
        }

        // Ensure a playable board in extreme edge cases
        // 2x2 grid with 6 colors for example
        if (IsDeadlocked())
        {
            ForceValidBoard();
        }

        UpdateAllIcons();
    }

    void PerformShuffle()
    {
        tempBlockList.Clear();
        
        // Gather all blocks 
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++) 
            { 
                if (board[x, y] != null)
                {
                    tempBlockList.Add(board[x, y]);
                }
            }
        }

        // Randomly shuffle color IDs using Fisher-Yates algorithm
        // See: https://www.geeksforgeeks.org/dsa/shuffle-a-given-array-using-fisher-yates-shuffle-algorithm/
        int[] shuffledColors = new int[tempBlockList.Count];
        for (int i = 0;  i < tempBlockList.Count; i++)
        {
            shuffledColors[i] = tempBlockList[i].colorId;
        }

        for (int i = shuffledColors.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = shuffledColors[i];
            shuffledColors[i] = shuffledColors[j];
            shuffledColors[j] = temp;
        }

        // Assign shuffled colors back to blocks and update their sprites
        int idx = 0;
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Block b = board[x, y];
                if (b != null)
                {
                    int newColor = shuffledColors[idx];
                    b.colorId = newColor; // Update logic
                    b.SpriteRenderer.sprite = blockSpriteColorSet[newColor].iconDef; // Update visuals
                    idx++;
                }
            }
        }
    }

    void ForceValidBoard()
    {
        // Horizontal match
        if (cols >= 2 && board[0, 0] != null && board[1, 0] != null)
        {
            int safeColor = board[0, 0].colorId;
            board[1, 0].colorId = safeColor;
            board[1, 0].SpriteRenderer.sprite = blockSpriteColorSet[safeColor].iconDef;
            if (!IsDeadlocked()) 
            {
                return;
            }
        }

        // Vertical match
        if (rows >= 2 && board[0, 0] != null && board[0, 1] != null)
        {
            int safeColor = board[0, 0].colorId;
            board[0, 1].colorId = safeColor;
            board[0, 1].SpriteRenderer.sprite = blockSpriteColorSet[safeColor].iconDef;
            if (!IsDeadlocked()) 
            { 
                return; 
            }
        }

        // Emergency: set all blocks to same color
        int emergencyColor = 0;
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Block b = board[x, y];
                if (b != null)
                {
                    b.colorId = emergencyColor;
                    b.SpriteRenderer.sprite = blockSpriteColorSet[emergencyColor].iconDef;
                }
            }
        }
    }

    // Find matches using a non-recursive Flood Fill algorithm
    // see https://www.geeksforgeeks.org/dsa/flood-fill-algorithm/
    void FindMatches(Block startBlock, List<Block> matchedBlocks)
    {
        if (startBlock == null)
        {
            return;
        }

        int startX = startBlock.col;
        int startY = startBlock.row;
        int targetColor = startBlock.colorId;

        bfsQueue.Clear(); // Mark start as visited
        bfsQueue.Enqueue((startX, startY)); // Add start block to matches

        visited[startX, startY] = true;
        matchedBlocks.Add(board[startX, startY]);

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (bfsQueue.Count > 0)
        {
            var (x, y) = bfsQueue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                // Check bounds, visited, existence, and color match
                if (nx >= 0 && nx < cols &&
                    ny >= 0 && ny < rows &&
                    !visited[nx, ny] &&
                    board[nx, ny] != null &&
                    board[nx, ny].colorId == targetColor)
                { 
                    visited[nx, ny] = true; // Mark neighbor visited
                    matchedBlocks.Add(board[nx, ny]); // Add to match list
                    bfsQueue.Enqueue((nx, ny)); // Continue BFS
                }
            }
        }
    }

    void UpdateIconForGroup(List<Block> group)
    {
        if (group.Count == 0)
        {
            return;
        }

        // Determine group size and color
        int size = group.Count;
        int colorId = group[0].colorId;
        
        // Get sprite to reflect the group size visually
        Sprite sprite = GetSpriteForGroupSize(size, colorId);

        // Update each block's sprite using the cached SpriteRenderer
        foreach (Block block in group)
        {
            if (block != null)
            {
                block.SpriteRenderer.sprite = sprite;
            }
        }
    }

    Sprite GetSpriteForGroupSize(int size, int colorId)
    {
        if (size > thresholdC) 
        { 
            return blockSpriteColorSet[colorId].iconThird;        
        }
        else if (size > thresholdB)
        {
            return blockSpriteColorSet[colorId].iconSecond;
        }
        else if (size >= thresholdA)
        {
            return blockSpriteColorSet[colorId].iconFirst;
        }
        else
        {
            return blockSpriteColorSet[colorId].iconDef;
        }
    }

    void ClearVisited()
    {
        // Reset all visited flags to false without allocating a new array
        System.Array.Clear(visited, 0, visited.Length);
    }
}