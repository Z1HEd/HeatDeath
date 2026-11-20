using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ScreenBoundary : MonoBehaviour
{
    // A shot that leaves the screen ends.
    private void OnTriggerExit2D(Collider2D other)
    {
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null && projectile.gameObject.activeInHierarchy)
            projectile.Kill();
    }
}