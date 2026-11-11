using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class GameUIController : MonoBehaviour
{
    public static GameUIController Instance { get; private set; }

    private const string RarityClassCommon = "upgrade-option-common";
    private const string RarityClassRare = "upgrade-option-rare";
    private const string RarityClassEpic = "upgrade-option-epic";
    private const string RarityClassLegendary = "upgrade-option-legendary";

    [SerializeField] private UIDocument uiDocument;

    private Player player;
    private UpgradeManager upgradeManager;
    private UpgradeDraftService upgradeDraftService;
    private ModuleDraftService<WeaponDefinition> weaponDraftService;
    private ModuleDraftService<ShipModuleDefinition> shipModuleDraftService;
    private ShipDraftService shipDraftService;

    private List<ShipDefinition> currentShipOptions = new List<ShipDefinition>();
    private readonly List<WeaponDefinition> currentWeaponOptions = new List<WeaponDefinition>();
    private readonly List<ShipModuleDefinition> currentShipModuleOptions = new List<ShipModuleDefinition>();
    private List<UpgradeDefinition> currentOptions = new List<UpgradeDefinition>();

    private VisualElement healthFill;
    private VisualElement shieldFill;
    private Label healthText;
    private Label shieldText;
    private VisualElement xpFill;
    private Label xpText;
    private Label levelText;
    private Label waveText;
    private Label pausedText;
    private VisualElement upgradePanel;
    private Label upgradeHeader;
    private Button[] optionButtons;
    public UICooldownDurationOverlay[] weaponButtonOverlays;
    public UICooldownDurationOverlay abilityButtonOverlay;
    private Action[] optionHandlers;

    private int pendingShipDrafts;
    private int pendingUpgradeDrafts;
    private int pendingWeaponDrafts;
    private int pendingShipModuleDrafts;

    private void Awake()
    {
        BindRefferences();
        optionHandlers = new Action[optionButtons.Length];
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionHandlers[i] = () => OnUpgradeOptionClicked(index);
            if (optionButtons[i] != null)
                optionButtons[i].clicked += optionHandlers[i];
        }

        SetUpgradeOverlayVisible(false);

        shipDraftService = new ShipDraftService();
        upgradeDraftService = new UpgradeDraftService();
        weaponDraftService = new ModuleDraftService<WeaponDefinition>("Weapons");
        shipModuleDraftService = new ModuleDraftService<ShipModuleDefinition>("ShipModules");

        GameController.Instance.OnXPChanged += UpdateXPBar;
        GameController.Instance.OnLevelChanged += UpdateLevelText;
        GameController.Instance.OnLevelChanged += StartDraftSequence;
        EnemySpawnController.Instance.OnWaveChanged += UpdateWaveText;

        UpdateXPBar();
        UpdateLevelText(0);

        pendingShipDrafts = 1;
        pendingWeaponDrafts = 1;
        ShowNextDraftOrResume();
    }

    private void BindRefferences()
    {
        Instance = this;

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
        waveText = root.Q<Label>("WaveText");
        pausedText = root.Q<Label>("PausedText");
        upgradePanel = root.Q<VisualElement>("UpgradePanel");
        upgradeHeader = root.Q<Label>("UpgradeHeader");
        root.Q<Button>("PauseButton").clicked += PauseUnpause;
        optionButtons = new[]
        {
            root.Q<Button>("UpgradeOption1"),
            root.Q<Button>("UpgradeOption2"),
            root.Q<Button>("UpgradeOption3")
        };
        weaponButtonOverlays = new[]
        {
            new UICooldownDurationOverlay(root.Q<Button>("Weapon1")),
            new UICooldownDurationOverlay(root.Q<Button>("Weapon2")),
            new UICooldownDurationOverlay(root.Q<Button>("Weapon3")),
            new UICooldownDurationOverlay(root.Q<Button>("Weapon4"))
        };
        abilityButtonOverlay = new UICooldownDurationOverlay(root.Q<Button>("Ability1"));
    }
    public void BindWeaponButton(GameObject weapon, int index){
        weapon.GetComponent<WeaponModule>().updateCooldownOverlay+=
                weaponButtonOverlays[index].SetCooldownFill;
        weaponButtonOverlays[index].button.style.backgroundImage = 
                new StyleBackground(weapon.GetComponent<SpriteRenderer>().sprite.texture);
    }

    private void BindPlayer(GameObject playerObject)
    {
        player = playerObject.GetComponent<Player>();
        player.CoreModule.OnHPShieldsChanged += UpdateBars;
        
        abilityButtonOverlay.button.clicked += player.ability.Activate;
        player.ability.updateCooldown += abilityButtonOverlay.SetCooldownFill;
        player.ability.updateDuration += abilityButtonOverlay.SetDurationFill;
        player.ability.updateIcon += abilityButtonOverlay.SetIcon;
        player.ability.updateActivatable += abilityButtonOverlay.SetClickable;
        abilityButtonOverlay.SetClickable(player.ability.IsActivatable);
    }
    private void PauseUnpause()
    {
        Time.timeScale = Time.timeScale == 0f?1f:0f;
        pausedText.visible = Time.timeScale == 0f;
    }

    private void StartDraftSequence(int levelsGained)
    {
        if (levelsGained <= 0)
            return;

        if (player == null)
        {
            
            return;
        }
        
        QueueDraftPicks(levelsGained);
        ShowNextDraftOrResume();
    }

    private void ShowNextDraftOrResume()
    {
        Time.timeScale = 0f;
        if (pendingShipDrafts > 0)
            ShowShipDraft();
        else if (pendingWeaponDrafts > 0)
            ShowModuleDraft(weaponDraftService, currentWeaponOptions, ref pendingWeaponDrafts, "Choose Weapon", def => def.Description);
        else if (pendingShipModuleDrafts > 0)
            ShowModuleDraft(shipModuleDraftService, currentShipModuleOptions, ref pendingShipModuleDrafts, "Choose Ship Module", def => def.Description);
        else if (pendingUpgradeDrafts > 0)
            ShowUpgradeDraft();
        else
        {
            SetUpgradeOverlayVisible(false);
            Time.timeScale = 1f;
        }
    }

    // Dev shortcut: an extra ship-module draft, shown at once.
    public void QueueShipModuleDraft()
    {
        pendingShipModuleDrafts++;
        ShowNextDraftOrResume();
    }

    private void ShowShipDraft()
    {
        upgradePanel?.AddToClassList("ship-draft-active");
        upgradePanel?.RemoveFromClassList("weapon-draft-active");

        currentShipOptions = shipDraftService.GetDraftOptions(3);

        if (currentShipOptions.Count == 0)
        {
            pendingShipDrafts--;
            ShowNextDraftOrResume();
            return;
        }

        PopulateDraftButtons("Choose Your Ship", currentShipOptions.Count,
            i =>
            {
                ShipDefinition def = currentShipOptions[i];
                ClearRarityClasses(optionButtons[i]);
                optionButtons[i].text = $"{def.DisplayName}\n{def.Description}";
            });
    }

    // Weapon and ship-module drafts. With no candidates, the draft becomes an upgrade draft, so no level is lost.
    private void ShowModuleDraft<T>(ModuleDraftService<T> service, List<T> options, ref int pending,
        string title, Func<T, string> describe) where T : ModuleDefinition
    {
        upgradePanel?.AddToClassList("weapon-draft-active");
        upgradePanel?.RemoveFromClassList("ship-draft-active");

        options.Clear();
        options.AddRange(service.GetDraftOptions(player, 3));

        if (options.Count == 0)
        {
            pending--;
            pendingUpgradeDrafts++;
            ShowNextDraftOrResume();
            return;
        }

        string header = pending > 1 ? $"{title} ({pending} picks remaining)" : title;

        PopulateDraftButtons(header, options.Count,
            i =>
            {
                ClearRarityClasses(optionButtons[i]);
                optionButtons[i].text = $"{options[i].DisplayName}\n{describe(options[i])}";
            });
    }

    private void ShowUpgradeDraft()
    {
        upgradePanel?.RemoveFromClassList("weapon-draft-active");
        upgradePanel?.RemoveFromClassList("ship-draft-active");

        currentOptions = upgradeDraftService.GetDraftOptions(player, 3);

        if (currentOptions.Count == 0)
        {
            pendingUpgradeDrafts = 0;
            SetUpgradeOverlayVisible(false);
            Time.timeScale = 1f;
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
        if (pendingShipDrafts > 0)
        {
            if (index < 0 || index >= currentShipOptions.Count)
                return;

            GameObject playerObject = GameController.Instance.SpawnPlayer(currentShipOptions[index]);
            if (playerObject != null)
            {
                BindPlayer(playerObject);
                UpdateBars();
            }

            pendingShipDrafts--;
        }
        else if (pendingWeaponDrafts > 0)
        {
            if (index < 0 || index >= currentWeaponOptions.Count)
                return;

            player.moduleManager.AddWeapon(currentWeaponOptions[index]);
            pendingWeaponDrafts--;
        }
        else if (pendingShipModuleDrafts > 0)
        {
            if (index < 0 || index >= currentShipModuleOptions.Count)
                return;

            player.moduleManager.AddShipModule(currentShipModuleOptions[index]);
            pendingShipModuleDrafts--;
        }
        else
        {
            if (index < 0 || index >= currentOptions.Count)
                return;

            player.upgradeManager.AddUpgrade(currentOptions[index]);
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
            else if (GameController.Instance.IsShipModuleDraftLevel(level))
                pendingShipModuleDrafts++;
            else
                pendingUpgradeDrafts++;
        }
    }

    // -------------------------------------------------------------------------
    // Bar updates
    // -------------------------------------------------------------------------

    private void UpdateBars()
    {
        UpdateHealthBar();
        UpdateShieldBar();
    }

    private void UpdateHealthBar()
    {

        float currentHealth = player.CoreModule.CurrentHealth;
        float maxHealth = player.CoreModule.CurrentMaxHealth;

        float fillPercent = maxHealth > 0 ? currentHealth / maxHealth : 0;
        healthFill.style.width = Length.Percent(Mathf.Clamp01(fillPercent) * 100f);
        healthText.text = $"{currentHealth:F0}/{maxHealth}";
    }

    private void UpdateShieldBar()
    {
        float currentShields = player.CoreModule.CurrentShields;
        float maxShields = player.CoreModule.CurrentMaxShields;

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
    private void UpdateWaveText(WaveDefinition wave,int waveNumber)
    {
        waveText.text = $"Wave {waveNumber}: {EnemySpawnController.Instance.CurrentSectorName} - {wave.WaveName}";
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public bool IsCursorOverUI()
    {
        var root = uiDocument.rootVisualElement;
        var screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        screenPos.y = Screen.height - screenPos.y; // flip bottom-up -> top-down
        var panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
        var picked = root.panel.Pick(panelPos);
        var hudPanel = root.Q<VisualElement>("HudPanel");
        
        if (picked == null || !picked.visible) return false;
        
        if (!upgradePanel.visible && upgradePanel.Contains(picked))
            return false;
        return true;
    }

    private void SetUpgradeOverlayVisible(bool visible)
    {
        if (upgradePanel == null)
            return;
        upgradePanel.visible = visible;
        pausedText.visible = false;
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