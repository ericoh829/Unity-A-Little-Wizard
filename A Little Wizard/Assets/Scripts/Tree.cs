using UnityEngine;

public class Tree : MonoBehaviour
{
    public bool isFalling = false; // To prevent multiple falls
    public float fallDelay = 0.5f; // Delay before the next tree falls
    public Vector2 fallDirection; // Direction the tree will fall

    void OnMouseDown() // For testing: detect player clicks
    {
        if (!isFalling)
        {
            SetFallDirection(Vector2.right); // Default direction for now
            TriggerFall();
        }
    }

    public void SetFallDirection(Vector2 direction)
    {
        fallDirection = direction.normalized; // Normalize the direction vector
    }

    public void TriggerFall()
    {
        if (isFalling) return; // Prevent multiple triggers
        isFalling = true;

        // Simulate tree fall animation
        Debug.Log("Tree falling in direction: " + fallDirection);
        StartCoroutine(FallAndTriggerNearbyTrees());
    }

    private System.Collections.IEnumerator FallAndTriggerNearbyTrees()
    {
        // Move tree slightly in the fall direction (placeholder for animation)
        transform.position += (Vector3)(fallDirection * 0.5f);
        yield return new WaitForSeconds(fallDelay);

        // Check for nearby trees to trigger chain reactions
        Collider2D[] nearbyTrees = Physics2D.OverlapCircleAll(transform.position, 1f); // Adjust radius
        foreach (var collider in nearbyTrees)
        {
            Tree nearbyTree = collider.GetComponent<Tree>();
            if (nearbyTree != null && !nearbyTree.isFalling)
            {
                nearbyTree.SetFallDirection(fallDirection);
                nearbyTree.TriggerFall();
            }
        }

        // Destroy or disable tree after falling
        Destroy(gameObject); // Replace with visual feedback later
    }
}
