using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ScreenBoundary : MonoBehaviour
{
    void OnTriggerExit2D(Collider2D other)
    {
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            Destroy(other.gameObject);
        }
    }
}