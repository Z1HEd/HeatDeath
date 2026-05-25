using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ScreenBoundary : MonoBehaviour
{
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.isTrigger) return;
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Kill();
        }
    }
}