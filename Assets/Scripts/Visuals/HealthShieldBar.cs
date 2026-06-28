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

    void Start()
    {
        shipCore = transform.parent.GetComponentInChildren<ShipCoreModule>();
        shipCore.OnHPShieldsChanged += UpdateBars;
        UpdateBars();
    }

    public void UpdateBars()
    {
        gameObject.SetActive(!hideWhenFull ||
                shipCore.CurrentHealth != shipCore.CurrentMaxHealth || 
                shipCore.CurrentShields != shipCore.CurrentMaxShields);

        if (!gameObject.activeSelf) return;
        
        healthBar.localScale = new Vector3(Mathf.Clamp(shipCore.CurrentHealth/shipCore.CurrentMaxHealth,0f,1f),
                healthBar.localScale.y,healthBar.localScale.z);
        shieldBar.localScale = new Vector3(Mathf.Clamp(shipCore.CurrentShields/shipCore.CurrentMaxShields,0f,1f),
                healthBar.localScale.y,healthBar.localScale.z);
    }
}
