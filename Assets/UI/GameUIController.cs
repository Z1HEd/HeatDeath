using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class GameUIController : MonoBehaviour
{
    private const string RarityClassCommon = "upgrade-option-common";
    private const string RarityClassRare = "upgrade-option-rare";
    private const string RarityClassEpic = "upgrade-option-epic";
    private const string RarityClassLegendary = "upgrade-option-legendary";

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private UpgradeDatabase upgradeDatabase;
    
    private ShipCoreModule playerCoreModule;
    private Player player;
    private UpgradeManager upgradeManager;
    private UpgradeDraftService draftService;

    private VisualElement healthFill;
    private VisualElement shieldFill;
    private Label healthText;
    private Label shieldText;
    private VisualElement xpFill;
    private Label xpText;
    private Label levelText;
    private VisualElement upgradeOverlay;
    private Label upgradeHeader;
    private Button[] optionButtons;
    private Action[] optionHandlers;

    private int pendingDraftPicks;
    private List<UpgradeDefinition> currentOptions = new List<UpgradeDefinition>();

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
        upgradeOverlay = root.Q<VisualElement>("UpgradeOverlay");
        upgradeHeader = root.Q<Label>("UpgradeHeader");
        optionButtons = new[]
        {
            root.Q<Button>("UpgradeOption1"),
            root.Q<Button>("UpgradeOption2"),
            root.Q<Button>("UpgradeOption3")
        };
        optionHandlers = new Action[optionButtons.Length];
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionHandlers[i] = () => OnUpgradeOptionClicked(index);
            if (optionButtons[i] != null)
                optionButtons[i].clicked += optionHandlers[i];
        }

        SetUpgradeOverlayVisible(false);

        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;

        player = playerObject.GetComponent<Player>();
        playerCoreModule = playerObject.GetComponent<ShipCoreModule>();
        upgradeManager = playerObject.GetComponent<UpgradeManager>();

        if (upgradeDatabase != null)
            draftService = new UpgradeDraftService(upgradeDatabase);

        if (playerCoreModule == null) return;
        
        playerCoreModule.OnHPShieldsChanged += UpdateBars;
        GameController.Instance.OnXPChanged += UpdateXPBar;
        GameController.Instance.OnLevelChanged += UpdateLevelText;
        GameController.Instance.OnLevelChanged += StartDraftSequence;
        
        UpdateBars();
        UpdateXPBar();
        UpdateLevelText(0);
    }

    private void OnDisable()
    {
        if (playerCoreModule != null)
        {
            playerCoreModule.OnHPShieldsChanged -= UpdateBars;
        }

        if (optionButtons != null && optionHandlers != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null && optionHandlers[i] != null)
                    optionButtons[i].clicked -= optionHandlers[i];
            }
        }

        if (GameController.Instance != null)
        {
            GameController.Instance.OnXPChanged -= UpdateXPBar;
            GameController.Instance.OnLevelChanged -= UpdateLevelText;
            GameController.Instance.OnLevelChanged -= StartDraftSequence;
        }
    }

    private void UpdateBars()
    {
        UpdateHealthBar();
        UpdateShieldBar();
    }

    private void UpdateHealthBar()
    {
        float currentHealth = playerCoreModule.CurrentHealth;
        float maxHealth = playerCoreModule.CurrentMaxHealth;

        float fillPercent = maxHealth > 0 ? currentHealth / maxHealth : 0;
        healthFill.style.width = Length.Percent(Mathf.Clamp01(fillPercent) * 100f);
        healthText.text = $"{currentHealth:F0}/{maxHealth}";
    }

    private void UpdateShieldBar()
    {
        float currentShields = playerCoreModule.CurrentShields;
        float maxShields = playerCoreModule.CurrentMaxShields;

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

    private void UpdateLevelText(int levelsIncrease)
    {
        levelText.text = $"Level {GameController.Instance.CurrentLevel}";
    }

    private void StartDraftSequence(int levelsGained)
    {
        if (levelsGained <= 0)
            return;

        if (player == null || upgradeManager == null || draftService == null)
        {
            GameController.Instance.Resume();
            return;
        }

        pendingDraftPicks = levelsGained;
        ShowNextDraftOrResume();
    }

    private void ShowNextDraftOrResume()
    {
        if (pendingDraftPicks <= 0)
        {
            SetUpgradeOverlayVisible(false);
            GameController.Instance.Resume();
            return;
        }

        currentOptions = draftService.BuildDraftOptions(player, 3);
        if (currentOptions.Count == 0)
        {
            pendingDraftPicks = 0;
            SetUpgradeOverlayVisible(false);
            GameController.Instance.Resume();
            return;
        }

        SetUpgradeOverlayVisible(true);
        if (upgradeHeader != null)
            upgradeHeader.text = pendingDraftPicks > 1 ? $"Choose Upgrade ({pendingDraftPicks} picks remaining)" : "Choose Upgrade";

        for (int i = 0; i < optionButtons.Length; i++)
        {
            Button button = optionButtons[i];
            if (button == null)
                continue;

            if (i < currentOptions.Count)
            {
                UpgradeDefinition option = currentOptions[i];
                button.style.display = DisplayStyle.Flex;
                button.SetEnabled(true);
                ApplyRarityClass(button, option.Rarity);
                button.text = $"[{option.Rarity}] {option.DisplayName}\n{option.Description}";
            }
            else
            {
                ClearRarityClasses(button);
                button.style.display = DisplayStyle.None;
                button.SetEnabled(false);
            }
        }
    }

    private void OnUpgradeOptionClicked(int index)
    {
        if (index < 0 || index >= currentOptions.Count)
            return;

        UpgradeDefinition selected = currentOptions[index];
        upgradeManager.AddUpgrade(selected);

        pendingDraftPicks--;
        ShowNextDraftOrResume();
    }

    private void SetUpgradeOverlayVisible(bool visible)
    {
        if (upgradeOverlay == null)
            return;

        upgradeOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ApplyRarityClass(Button button, UpgradeRarity rarity)
    {
        ClearRarityClasses(button);

        switch (rarity)
        {
            case UpgradeRarity.Rare:
                button.AddToClassList(RarityClassRare);
                break;
            case UpgradeRarity.Epic:
                button.AddToClassList(RarityClassEpic);
                break;
            case UpgradeRarity.Legendary:
                button.AddToClassList(RarityClassLegendary);
                break;
            default:
                button.AddToClassList(RarityClassCommon);
                break;
        }
    }

    private void ClearRarityClasses(Button button)
    {
        button.RemoveFromClassList(RarityClassCommon);
        button.RemoveFromClassList(RarityClassRare);
        button.RemoveFromClassList(RarityClassEpic);
        button.RemoveFromClassList(RarityClassLegendary);
    }
}
