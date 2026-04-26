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
    
    private ShipCoreModule playerCoreModule;
    private Player player;
    private UpgradeManager upgradeManager;
    private UpgradeDraftService upgradeDraftService;
    private WeaponDraftService weaponDraftService;

    private List<WeaponDefinition> currentWeaponOptions = new List<WeaponDefinition>();

    private VisualElement healthFill;
    private VisualElement shieldFill;
    private Label healthText;
    private Label shieldText;
    private VisualElement xpFill;
    private Label xpText;
    private Label levelText;
    private VisualElement upgradeOverlay;
    private VisualElement upgradePanel;
    private Label upgradeHeader;
    private Button[] optionButtons;
    private Action[] optionHandlers;

    private int pendingUpgradeDrafts;
    private int pendingWeaponDrafts;
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
        upgradePanel = root.Q<VisualElement>("UpgradePanel");
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
        playerCoreModule = player != null ? player.CoreModule : null;
        upgradeManager = playerObject.GetComponent<UpgradeManager>();

        upgradeDraftService = new UpgradeDraftService();
        weaponDraftService = new WeaponDraftService();

        if (playerCoreModule == null) return;
        
        playerCoreModule.OnHPShieldsChanged += UpdateBars;
        GameController.Instance.OnXPChanged += UpdateXPBar;
        GameController.Instance.OnLevelChanged += UpdateLevelText;
        GameController.Instance.OnLevelChanged += StartDraftSequence;
        
        UpdateBars();
        UpdateXPBar();
        UpdateLevelText(0);

        if (GameController.Instance.IsWeaponDraftLevel())
        {
            Time.timeScale = 0f;
            pendingWeaponDrafts++;
            ShowNextDraftOrResume();
        }
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

        if (player == null)
        {
            GameController.Instance.Resume();
            return;
        }

        QueueDraftPicks(levelsGained);
        ShowNextDraftOrResume();
    }

    private void ShowNextDraftOrResume()
    {
        if (pendingUpgradeDrafts <= 0 && pendingWeaponDrafts <= 0)
        {
            SetUpgradeOverlayVisible(false);
            GameController.Instance.Resume();
            return;
        }

        if (pendingWeaponDrafts > 0)
            ShowWeaponDraft();
        else
            ShowUpgradeDraft();
    }

    private void ShowWeaponDraft()
    {
        upgradePanel?.AddToClassList("weapon-draft-active");
        currentWeaponOptions = weaponDraftService.GetDraftOptions(player, 3);

        if (currentWeaponOptions.Count == 0)
        {
            pendingWeaponDrafts--;
            ShowNextDraftOrResume();
            return;
        }

        string header = pendingWeaponDrafts > 1
            ? $"Choose Weapon ({pendingWeaponDrafts} picks remaining)"
            : "Choose Weapon";

        PopulateDraftButtons(header, currentWeaponOptions.Count,
            i =>
            {
                WeaponDefinition def = currentWeaponOptions[i];
                ClearRarityClasses(optionButtons[i]);
                optionButtons[i].text = $"{def.DisplayName}\n{def.Description}";
            });
    }

    private void ShowUpgradeDraft()
    {
        upgradePanel?.RemoveFromClassList("weapon-draft-active");
        currentOptions = upgradeDraftService.GetDraftOptions(player, 3);
        if (currentOptions.Count == 0)
        {
            pendingUpgradeDrafts = 0;
            SetUpgradeOverlayVisible(false);
            GameController.Instance.Resume();
            return;
        }

        string header = pendingUpgradeDrafts > 1
            ? $"Choose Upgrade ({pendingUpgradeDrafts} picks remaining)"
            : "Choose Upgrade";

        PopulateDraftButtons(header, currentOptions.Count,
            i =>
            {
                UpgradeDefinition opt = currentOptions[i];
                ApplyRarityClass(optionButtons[i], opt.Rarity);
                optionButtons[i].text = $"[{opt.Rarity}] {opt.DisplayName}\n{opt.Description}";
            });
    }

    private void PopulateDraftButtons(string header, int activeCount, System.Action<int> configureButton)
    {
        SetUpgradeOverlayVisible(true);
        if (upgradeHeader != null)
            upgradeHeader.text = header;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            Button button = optionButtons[i];
            if (button == null)
                continue;

            if (i < activeCount)
            {
                button.style.display = DisplayStyle.Flex;
                button.SetEnabled(true);
                configureButton(i);
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
        if (pendingWeaponDrafts > 0)
        {
            if (index < 0 || index >= currentWeaponOptions.Count)
                return;

            player.moduleManager.AddWeapon(currentWeaponOptions[index]);
            pendingWeaponDrafts--;
        }
        else
        {
            if (index < 0 || index >= currentOptions.Count)
                return;

            upgradeManager.AddUpgrade(currentOptions[index]);
            pendingUpgradeDrafts--;
        }

        ShowNextDraftOrResume();
    }

    private void QueueDraftPicks(int levelsGained)
    {
        int currentLevel = GameController.Instance.CurrentLevel;
        int firstGainedLevel = currentLevel - levelsGained + 1;

        for (int level = firstGainedLevel; level <= currentLevel; level++)
        {
            if (GameController.Instance.IsWeaponDraftLevel(level))
                pendingWeaponDrafts++;
            else
                pendingUpgradeDrafts++;
        }
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
