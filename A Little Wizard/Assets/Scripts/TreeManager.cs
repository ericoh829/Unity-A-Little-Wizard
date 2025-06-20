using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TreeManager : MonoBehaviour
{
    public Tilemap treeTilemap;        // Reference to the Tree Tilemap
    public Tilemap groundTilemap;      // Reference to the Ground Tilemap
    public TilemapCollider2D treeTilemapCollider; // Reference to Tilemap Collider 2D
    public int treeHealth = 3;         // Default health for each tree

    private Dictionary<Vector3Int, int> treeHealthMap = new Dictionary<Vector3Int, int>();

    void Start()
    {
        InitializeTrees();
    }

    // Initialize all tree tiles with default health
    void InitializeTrees()
    {
        foreach (Vector3Int position in treeTilemap.cellBounds.allPositionsWithin)
        {
            if (treeTilemap.HasTile(position))
            {
                treeHealthMap[position] = treeHealth; // Set health for each tree tile
            }
        }
    }

    // Chop the tree at the clicked position
    public void ChopTree(Vector3 worldPosition)
    {
        Vector3Int tilePosition = treeTilemap.WorldToCell(worldPosition);

        if (treeTilemap.HasTile(tilePosition))
        {
            if (treeHealthMap.ContainsKey(tilePosition))
            {
                treeHealthMap[tilePosition]--; // Reduce tree health
                Debug.Log($"Tree at {tilePosition} has {treeHealthMap[tilePosition]} health left.");

                if (treeHealthMap[tilePosition] <= 0)
                {
                    ReplaceWithClosestGroundTile(tilePosition);
                    Debug.Log($"Tree at {tilePosition} chopped down!");
                }
            }
        }
    }

    // Replace the chopped tree with the closest ground tile
    void ReplaceWithClosestGroundTile(Vector3Int treePosition)
    {
        Vector3Int closestGroundTile = FindClosestGroundTile(treePosition);

        if (closestGroundTile != null)
        {
            TileBase groundTile = groundTilemap.GetTile(closestGroundTile);
            if (groundTile != null)
            {
                // Replace the tree tile with the ground tile
                treeTilemap.SetTile(treePosition, groundTile);

                // Force the Tilemap Collider to refresh
                RefreshTilemapCollider();
            }
        }
    }

    // Find the closest ground tile to the given position
    Vector3Int FindClosestGroundTile(Vector3Int treePosition)
    {
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),   // Right
            new Vector3Int(-1, 0, 0),  // Left
            new Vector3Int(0, 1, 0),   // Up
            new Vector3Int(0, -1, 0),  // Down
            new Vector3Int(1, 1, 0),   // Top-right
            new Vector3Int(-1, 1, 0),  // Top-left
            new Vector3Int(1, -1, 0),  // Bottom-right
            new Vector3Int(-1, -1, 0)  // Bottom-left
        };

        foreach (Vector3Int dir in directions)
        {
            Vector3Int neighborPosition = treePosition + dir;
            if (groundTilemap.HasTile(neighborPosition))
            {
                return neighborPosition; // Return the first ground tile found
            }
        }

        Debug.LogWarning("No ground tile found near chopped tree.");
        return treePosition; // Fallback: return the same position
    }

    // Force the Tilemap Collider to refresh
    void RefreshTilemapCollider()
    {
        if (treeTilemapCollider != null)
        {
            treeTilemapCollider.enabled = false;
            treeTilemapCollider.enabled = true; // Disable and re-enable to refresh colliders
        }
    }
}
