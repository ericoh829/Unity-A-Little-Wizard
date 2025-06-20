using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerTileMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform walkableGrid;
    public Transform obstaclesGrid;
    public GameObject markerEffectPrefab;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Queue<Vector3> pathQueue;
    private GameObject currentMarkerEffect;

    private GestureDetection gestureDetector;

    void Start()
    {
        targetPosition = transform.position;
        gestureDetector = gameObject.AddComponent<GestureDetection>();
    }

    void Update()
    {
        HandleGestureInput();
        MovePlayer();
    }

    void HandleGestureInput()
    {
        var (touchType, touchLocation) = gestureDetector.GetTouchInput();

        if (touchType == GestureDetection.TouchType.Tap)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(touchLocation);
            worldPosition.z = 0;

            Vector3Int clickedTile = WorldToCell(worldPosition);
            Vector3Int currentTile = WorldToCell(transform.position);

            if (IsTileWalkable(clickedTile) && !IsTileObstacle(clickedTile))
            {
                if (isMoving)
                {
                    InterruptMovementToNewTile(worldPosition);
                }
                else
                {
                    StartPathMovement(currentTile, clickedTile);
                }
            }
        }
        else if (touchType == GestureDetection.TouchType.SwipeUp)
        {
            //Debug.Log("Swipe Up detected: Custom behavior can be added.");
        }
        else if (touchType == GestureDetection.TouchType.Hold)
        {
            //Debug.Log("Hold detected: Custom hold action here.");
        }
    }

    void StartPathMovement(Vector3Int startTile, Vector3Int endTile)
    {
        List<Vector3Int> path = AStarPathfinding(startTile, endTile);
        pathQueue = new Queue<Vector3>();

        foreach (Vector3Int cell in path)
        {
            pathQueue.Enqueue(CellToWorld(cell));
        }

        isMoving = pathQueue.Count > 0;
        PlaceMarkerAtTarget(CellToWorld(endTile));
    }

    void MovePlayer()
    {
        if (!isMoving || pathQueue == null || pathQueue.Count == 0) return;

        Vector3 nextPosition = pathQueue.Peek();
        transform.position = Vector3.MoveTowards(transform.position, nextPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, nextPosition) < 0.01f)
        {
            transform.position = nextPosition;
            pathQueue.Dequeue();
        }

        if (pathQueue.Count == 0)
        {
            isMoving = false;
            RemoveMarkerEffect();
        }
    }

    // Convert world position to tile position
    Vector3Int WorldToCell(Vector3 position)
    {
        foreach (Transform child in walkableGrid)
        {
            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null)
                return tilemap.WorldToCell(position);
        }
        return Vector3Int.zero;
    }

    // Convert tile position to world position
    Vector3 CellToWorld(Vector3Int cellPosition)
    {
        foreach (Transform child in walkableGrid)
        {
            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null)
                return tilemap.GetCellCenterWorld(cellPosition);
        }
        return Vector3.zero;
    }

    // Check if a tile is walkable (exists on any Tilemap under WalkableGrid)
    public bool IsTileWalkable(Vector3Int position)
    {
        foreach (Transform child in walkableGrid)
        {
            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null && tilemap.HasTile(position) && !IsTileObstacle(position))
            {
                return true; // Tile found on a walkable Tilemap
            }
        }
        return false;
    }

    // Check if a tile exists on any Tilemap under ObstaclesGrid
    bool IsTileObstacle(Vector3Int position)
    {
        foreach (Transform child in obstaclesGrid)
        {
            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null && tilemap.HasTile(position))
            {
                return true; // Tile found in an obstacle Tilemap
            }
        }
        return false;
    }

    // A* Pathfinding Algorithm
    public List<Vector3Int> AStarPathfinding(Vector3Int start, Vector3Int goal)
    {
        List<Vector3Int> openSet = new List<Vector3Int> { start };
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        // Use float to allow decimal costs
        Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float> { [start] = 0f };
        Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Vector3Int current = GetTileWithLowestFScore(openSet, fScore);

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector3Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor) || !IsTileWalkable(neighbor) || IsTileObstacle(neighbor))
                    continue;

                // Determine if the move is straight or diagonal
                bool isDiagonal = (neighbor.x != current.x && neighbor.y != current.y);
                float moveCost = isDiagonal ? 1.4f : 1f;

                float tentativeGScore = gScore[current] + moveCost;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    // Heuristic returns int, cast to float
                    fScore[neighbor] = gScore[neighbor] + (float)Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return new List<Vector3Int>(); // Return empty path if no path found
    }

    // Get valid neighbors for pathfinding
    List<Vector3Int> GetNeighbors(Vector3Int tile)
    {
        Vector3Int[] straightDirs = {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
        };

        Vector3Int[] diagonalDirs = {
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
        };

        List<Vector3Int> neighbors = new List<Vector3Int>();

        // Add straight neighbors
        foreach (var dir in straightDirs)
        {
            Vector3Int neighbor = tile + dir;
            if (IsTileWalkable(neighbor) && !IsTileObstacle(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        // Add diagonal neighbors only if both adjacent straight tiles are walkable
        foreach (var dir in diagonalDirs)
        {
            Vector3Int neighbor = tile + dir;
            Vector3Int adjacent1 = tile + new Vector3Int(dir.x, 0, 0); // Horizontal
            Vector3Int adjacent2 = tile + new Vector3Int(0, dir.y, 0); // Vertical

            if (IsTileWalkable(neighbor) && !IsTileObstacle(neighbor) &&
                IsTileWalkable(adjacent1) && !IsTileObstacle(adjacent1) &&
                IsTileWalkable(adjacent2) && !IsTileObstacle(adjacent2))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    // Heuristic function (Manhattan Distance)
    int Heuristic(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Reconstruct the path
    List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> path = new List<Vector3Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    // Get tile with the lowest fScore
    Vector3Int GetTileWithLowestFScore(List<Vector3Int> openSet, Dictionary<Vector3Int, float> fScore)
    {
        Vector3Int lowest = openSet[0];
        float lowestScore = fScore[lowest];
        foreach (Vector3Int tile in openSet)
        {
            if (fScore[tile] < lowestScore)
            {
                lowest = tile;
                lowestScore = fScore[tile];
            }
        }
        return lowest;
    }

    public void MoveToPosition(Vector3 worldPosition)
    {
        Vector3Int targetTile = WorldToCell(worldPosition); // Convert world position to tile
        Vector3Int currentTile = WorldToCell(transform.position);

        // Validate the target tile
        if (IsTileWalkable(targetTile) && !IsTileObstacle(targetTile))
        {
            List<Vector3Int> path = AStarPathfinding(currentTile, targetTile);

            pathQueue = new Queue<Vector3>();
            foreach (Vector3Int cell in path)
            {
                pathQueue.Enqueue(CellToWorld(cell));
            }

            isMoving = pathQueue.Count > 0;

            // If we found a path, place the marker effect at the target
            if (path.Count > 0)
            {
                PlaceMarkerAtTarget(CellToWorld(targetTile));
            }
        }
        else
        {
            Debug.Log("Target tile is not walkable or blocked by an obstacle.");
        }
    }

    public bool IsMoving
    {
        get { return isMoving; }
    }

    // If new movement input was made while the character is not at a tile center,
    // the character will skip the first tile in the new route and start from the second tile.
    public void InterruptMovementToNewTile(Vector3 worldPosition)
    {
        // Remove the old marker since we are changing the route
        RemoveMarkerEffect();

        MoveToPosition(worldPosition);

        // If the character is currently not on the center of a tile, skip the first tile in the new path
        if (!IsAtTileCenter() && pathQueue != null && pathQueue.Count > 1)
        {
            pathQueue.Dequeue();
        }
    }

    // Checks if the character is currently exactly at a tile center
    private bool IsAtTileCenter()
    {
        Vector3Int currentCell = WorldToCell(transform.position);
        Vector3 cellCenter = CellToWorld(currentCell);

        // If the character is very close to cell center, consider them at the tile center
        return Vector3.Distance(transform.position, cellCenter) < 0.01f;
    }

    // Place the marker effect at the given world position
    private void PlaceMarkerAtTarget(Vector3 position)
    {
        // Remove old marker if any
        RemoveMarkerEffect();

        if (markerEffectPrefab != null)
        {
            currentMarkerEffect = Instantiate(markerEffectPrefab, position, Quaternion.identity);
        }
    }

    // Remove the currently placed marker effect, if any
    private void RemoveMarkerEffect()
    {
        if (currentMarkerEffect != null)
        {
            Destroy(currentMarkerEffect);
            currentMarkerEffect = null;
        }
    }

    public float GetDistanceBetweenTiles(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> path = AStarPathfinding(start, end);
        if (path.Count == 0)
        {
            return -1f; // No valid path found
        }

        float totalCost = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            Vector3Int prev = path[i - 1];
            Vector3Int current = path[i];

            // Determine if the move is diagonal or straight
            bool isDiagonal = (current.x != prev.x && current.y != prev.y);

            // Add cost accordingly
            totalCost += isDiagonal ? 1.4f : 1f;
        }

        return totalCost;
    }
}
