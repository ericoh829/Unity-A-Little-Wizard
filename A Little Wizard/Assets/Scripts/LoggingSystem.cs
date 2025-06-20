using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LoggingSystem : MonoBehaviour
{
    public Tilemap treeTilemap;              // Tilemap containing tree tiles
    public Transform walkableGrid;           // Parent Grid for walkable tiles
    public TileBase markedTreeTile;          // Tile to visually highlight marked trees

    private Dictionary<Vector3Int, Vector3Int> markedTrees = new Dictionary<Vector3Int, Vector3Int>(); // Tree positions -> fall direction
    private Dictionary<Vector3Int, int> treeHeights = new Dictionary<Vector3Int, int>(); // Tree positions -> tree heights
    private PlayerTileMovement playerMovement; // Reference to PlayerTileMovement script
    private bool isChainActive = false;      // Prevent simultaneous chain reactions

    void Start()
    {
        playerMovement = GetComponent<PlayerTileMovement>();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) // Detect touch or mouse click
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPosition.z = 0;

            Vector3Int treePosition = treeTilemap.WorldToCell(worldPosition);

            if (treeTilemap.HasTile(treePosition))
            {
                if (markedTrees.ContainsKey(treePosition) && !isChainActive)
                {
                    StartCoroutine(HoldToStartChainCoroutine(treePosition));
                }
                else
                {
                    DetectSwipeAndMarkTree(treePosition);
                }
            }
        }
    }

    void DetectSwipeAndMarkTree(Vector3Int treePosition)
    {
        StartCoroutine(WaitForSwipeDirection(treePosition));
    }

    IEnumerator WaitForSwipeDirection(Vector3Int treePosition)
    {
        Vector3 swipeStart = Input.mousePosition;

        while (Input.GetMouseButton(0)) // Wait for swipe to end
        {
            yield return null;
        }

        Vector3 swipeEnd = Input.mousePosition;
        Vector3 swipeDirection = swipeEnd - swipeStart;
        Vector3Int direction = GetSwipeDirection(swipeDirection);

        if (direction != Vector3Int.zero)
        {
            MovePlayerAndMarkTreeToClosestTile(treePosition, direction);
        }
    }

    void MovePlayerAndMarkTreeToClosestTile(Vector3Int treePosition, Vector3Int direction)
    {
        Vector3Int[] possibleTiles = GetFiveClosestTiles(treePosition, direction);
        Vector3Int closestTile = FindClosestWalkableTile(possibleTiles);

        if (closestTile != Vector3Int.zero)
        {
            Vector3 worldTarget = treeTilemap.GetCellCenterWorld(closestTile);
            playerMovement.MoveToPosition(worldTarget);
            StartCoroutine(WaitForPlayerAndMarkTree(treePosition, direction, closestTile));
        }
        else
        {
            Debug.Log("No suitable walkable tile found near the tree.");
        }
    }

    IEnumerator WaitForPlayerAndMarkTree(Vector3Int treePosition, Vector3Int direction, Vector3Int targetTile)
    {
        while (playerMovement.IsMoving)
        {
            yield return null;
        }

        Vector3Int currentPlayerTile = WorldToCell(transform.position);
        if (currentPlayerTile == targetTile)
        {
            MarkTree(treePosition, direction);
        }
    }

    void MarkTree(Vector3Int treePosition, Vector3Int direction)
    {
        if (!markedTrees.ContainsKey(treePosition))
        {
            markedTrees.Add(treePosition, direction);
            int height = DetermineTreeHeight(treePosition);
            treeTilemap.SetTile(treePosition, markedTreeTile); 
            treeHeights[treePosition] = height;
            Debug.Log($"Tree at {treePosition} marked to fall in direction {direction} with height {height}");
        }
    }

    int DetermineTreeHeight(Vector3Int treePosition)
    {
        Tile treeTile = treeTilemap.GetTile<Tile>(treePosition);
        if (treeTile == null) return 0;

        Sprite treeSprite = treeTile.sprite;
        if (treeSprite == null) return 0; 

        float pixelsPerUnit = treeSprite.pixelsPerUnit; // Get pixels per unit of the sprite
        float pixelHeight = treeSprite.rect.height;

        Debug.Log($"pixelsPerUnit: {pixelsPerUnit}, pixelHeight: {pixelHeight}");
        return Mathf.CeilToInt(pixelHeight / pixelsPerUnit);
    }

    IEnumerator TriggerTreeFallChain(Vector3Int startTree)
    {
        isChainActive = true;
        Queue<Vector3Int> treeQueue = new Queue<Vector3Int>();
        treeQueue.Enqueue(startTree);

        while (treeQueue.Count > 0)
        {
            Vector3Int currentTree = treeQueue.Dequeue();
            if (!markedTrees.ContainsKey(currentTree)) continue;

            int treeHeight = treeHeights.ContainsKey(currentTree) ? treeHeights[currentTree] : 1;
            Vector3Int fallDirection = markedTrees[currentTree];

            treeTilemap.SetTile(currentTree, null);
            markedTrees.Remove(currentTree);
            Debug.Log($"Tree at {currentTree} fell.");

            for (int i = 1; i < treeHeight; i++)
            {
                Vector3Int affectedTile = currentTree + fallDirection * i;
                if (markedTrees.ContainsKey(affectedTile))
                {
                    treeQueue.Enqueue(affectedTile);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        isChainActive = false;
        Debug.Log("Chain reaction completed.");
    }

    IEnumerator HoldToStartChainCoroutine(Vector3Int treePosition)
    {
        Debug.Log("Holding to start tree fall chain...");
        yield return new WaitForSeconds(2f);

        if (markedTrees.ContainsKey(treePosition) && !isChainActive)
        {
            StartCoroutine(TriggerTreeFallChain(treePosition));
        }
    }

    Vector3Int GetSwipeDirection(Vector3 swipe)
    {
        if (swipe.magnitude < 50) return Vector3Int.zero;
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        {
            return swipe.x > 0 ? Vector3Int.right : Vector3Int.left;
        }
        else
        {
            return swipe.y > 0 ? Vector3Int.up : Vector3Int.down;
        }
    }

    Vector3Int[] GetFiveClosestTiles(Vector3Int treePosition, Vector3Int direction)
    {
        Vector3Int[] tiles = new Vector3Int[5];
        tiles[0] = treePosition + direction;
        tiles[1] = treePosition + new Vector3Int(direction.y, direction.x, 0);
        tiles[2] = tiles[1] + direction;
        tiles[3] = treePosition + new Vector3Int(-direction.y, -direction.x, 0);
        tiles[4] = tiles[3] + direction;
        return tiles;
    }

    Vector3Int FindClosestWalkableTile(Vector3Int[] candidateTiles)
    {
        float minDistance = float.MaxValue;
        Vector3Int closestTile = Vector3Int.zero;

        foreach (Vector3Int tile in candidateTiles)
        {
            if (playerMovement.IsTileWalkable(tile))
            {
                float distance = playerMovement.GetDistanceBetweenTiles(WorldToCell(transform.position), tile);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTile = tile;
                }
            }
        }
        return closestTile;
    }

    Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return treeTilemap.WorldToCell(worldPosition);
    }
}
