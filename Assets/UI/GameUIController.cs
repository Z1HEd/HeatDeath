using UnityEngine;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    
    private Ship playerShip;
    private VisualElement healthFill;
    private VisualElement shieldFill;
    private Label healthText;
    private Label shieldText;
    private VisualElement xpFill;
    private Label xpText;
    private Label levelText;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;
        healthFill = root.Q<VisualElement>("HealthFill");
        shieldFill = root.Q<VisualElement>("ShieldFill");
        healthText = root.Q<Label>("HealthText");
        shieldText = root.Q<Label>("ShieldText");
        xpFill = root.Q<VisualElement>("XPFill");
        xpText = root.Q<Label>("XPText");
        levelText = root.Q<Label>("LevelText");

        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;
        
        playerShip = playerObject.GetComponent<Ship>();
        if (playerShip == null) return;
        
        playerShip.OnHPShieldsChanged += UpdateBars;
        GameController.Instance.OnXPChanged += UpdateXPBar;
        GameController.Instance.OnLevelChanged += UpdateLevelText;
        
        UpdateBars();
        UpdateXPBar();
        UpdateLevelText();
    }

    private void OnDisable()
    {
        if (playerShip != null)
        {
            playerShip.OnHPShieldsChanged -= UpdateBars;
        }
        if (GameController.Instance != null)
        {
            GameController.Instance.OnXPChanged -= UpdateXPBar;
            GameController.Instance.OnLevelChanged -= UpdateLevelText;
        }
    }

    private void UpdateBars()
    {
        UpdateHealthBar();
        UpdateShieldBar();
    }

    private void UpdateHealthBar()
    {
        var characteristics = playerShip.CurrentCharacteristics;
        float currentHealth = playerShip.CurrentHealth;
        float maxHealth = characteristics.maxHealth;

        float fillPercent = maxHealth > 0 ? currentHealth / maxHealth : 0;
        healthFill.style.width = Length.Percent(Mathf.Clamp01(fillPercent) * 100f);
        healthText.text = $"{currentHealth:F0}/{maxHealth}";
    }

    private void UpdateShieldBar()
    {
        var characteristics = playerShip.CurrentCharacteristics;
        float currentShields = playerShip.CurrentShields;
        float maxShields = characteristics.maxShields;

        float fillPercent = maxShields > 0 ? currentShields / maxShields : 0;
        shieldFill.style.width = Length.Percent(Mathf.Clamp01(fillPercent) * 100f);
        shieldText.text = $"{currentShields:F0}/{maxShields}";
    }

    private void UpdateXPBar()
    {
        float currentXP = GameController.Instance.CurrentXP;
        float xpRequired = GameController.Instance.XPRequiredForNextLevel;

        float fillPercent = xpRequired > 0 ? currentXP / xpRequired : 0;
        xpFill.style.width = Length.Percent(Mathf.Clamp01(fillPercent) * 100f);
        xpText.text = $"{currentXP:F0}/{xpRequired:F0}";
    }

    private void UpdateLevelText()
    {
        levelText.text = $"Level {GameController.Instance.CurrentLevel}";
    }
}
