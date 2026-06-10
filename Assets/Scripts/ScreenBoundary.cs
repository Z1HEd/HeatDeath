using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ScreenBoundary : MonoBehaviour
{
    void OnTriggerExit2D(Collider2D other)
    {
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null && !proj.isDead && proj.gameObject.activeInHierarchy)
        {
            proj.StartCoroutine(proj.Kill());
        }
    }
}