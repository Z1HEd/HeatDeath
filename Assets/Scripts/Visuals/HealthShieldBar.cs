using UnityEngine;

public class HealthShieldbar : MonoBehaviour
{
    private ShipCoreModule shipCore;
    [SerializeField]
    private Transform healthBar;
    [SerializeField]
    private Transform shieldBar;
    [SerializeField]
    private bool hideWhenFull = true;

    private SpriteRenderer shieldRenderer;
    private Color defaultShieldColor;

    void Start()
    {
        shieldRenderer = shieldBar.GetComponentInChildren<SpriteRenderer>();
        defaultShieldColor = shieldRenderer.color;
        shipCore = transform.parent.GetComponentInChildren<ShipCoreModule>();
        shipCore.OnHPShieldsChanged += UpdateBars;
        UpdateBars();
    }

    public void UpdateBars()
    {
        gameObject.SetActive(!hideWhenFull || !shipCore.IsFull);

        if (!gameObject.activeSelf) return;
        
        healthBar.localScale = new Vector3(Mathf.Clamp01(shipCore.HealthFraction),
                healthBar.localScale.y,healthBar.localScale.z);
        shieldBar.localScale = new Vector3(Mathf.Clamp01(shipCore.ShieldFraction),
                healthBar.localScale.y,healthBar.localScale.z);
        shieldRenderer.color = shipCore.HasHardenedShields
            ? ShipCoreModule.WithAlpha(ShipCoreModule.HardenedShieldColor, defaultShieldColor.a) : defaultShieldColor;
    }
}
