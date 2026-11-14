using UnityEngine;
using System;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    private static GameController instance;

    [SerializeField]
    private int currentLevel = 0;
    [SerializeField]
    private float currentXP = 0f;

    [Header("Level curve")]
    [Tooltip("Scrap needed to go from level 1 to 2.")]
    [SerializeField] private float baseXPPerLevel = 20f;
    [Tooltip("How much more scrap each following level needs. The cost is rounded.")]
    [SerializeField] private float xpIncreasePerLevel = 1.2f;

    [Header("Module drafts")]
    [Tooltip("Levels that give a weapon draft instead of an upgrade draft. The first weapon is drafted at run start.")]
    [SerializeField] private List<int> weaponDraftLevels = new List<int> { 10, 30, 45 };
    [Tooltip("Levels that give a ship module draft instead of an upgrade draft.")]
    [SerializeField] private List<int> shipModuleDraftLevels = new List<int> { 5, 20, 35, 50 };

    [Space]
    [SerializeField]
    private GameObject gameUIPrefab;
    [SerializeField]
    private Transform playerSpawnPoint;
    
    public Action OnXPChanged;
    public Action<int> OnLevelChanged;
    
    public int CurrentLevel => currentLevel;
    public float CurrentXP => currentXP;

    public float XPRequiredForNextLevel =>
        Mathf.Round(baseXPPerLevel + xpIncreasePerLevel * (Mathf.Max(currentLevel, 1) - 1));
    public float CurrentLevelProgress => currentXP / XPRequiredForNextLevel;

    // Null until the ship draft spawns it.
    public Player Player => player;
    private Player player;

    public static GameController Instance => instance;

    public bool IsWeaponDraftLevel()
    {
        return IsWeaponDraftLevel(currentLevel);
    }

    public bool IsWeaponDraftLevel(int level)
    {
        return IsDraftLevel(weaponDraftLevels, level);
    }

    public bool IsShipModuleDraftLevel(int level)
    {
        return IsDraftLevel(shipModuleDraftLevels, level);
    }

    private static bool IsDraftLevel(List<int> draftLevels, int level)
    {
        return level > 0 && draftLevels != null && draftLevels.Contains(level);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        if (gameUIPrefab != null)
            Instantiate(gameUIPrefab);
    }

    public GameObject SpawnPlayer(ShipDefinition shipDefinition)
    {
        if (shipDefinition == null || shipDefinition.ShipPrefab == null)
        {
            Debug.LogWarning("SpawnPlayer: ShipDefinition or its prefab is null.");
            return null;
        }

        Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        Quaternion spawnRot = playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity;

        GameObject playerObject = Instantiate(shipDefinition.ShipPrefab, spawnPos, spawnRot);
        player = playerObject.GetComponent<Player>();
        return playerObject;
    }

    // The player's scrap gain multiplies all scrap. Without a player it counts as 1.
    public void AddXP(float xpAmount)
    {
        if (player != null && player.CoreModule != null)
            xpAmount *= player.CoreModule.XPGain;

        currentXP += xpAmount;
        
        ProcessLevelUpsFromCurrentXP();
        OnXPChanged?.Invoke();
    }

    // Dev shortcut: levels without scrap, so scrap gain can't distort the count.
    public void AddLevels(int levels)
    {
        if (levels <= 0)
            return;

        currentLevel += levels;
        RaiseLevelChanged(levels);
        OnXPChanged?.Invoke();
    }

    private void RaiseLevelChanged(int levelsGained)
    {
        // Level-scaled effects (StatModifierEffect.perLevel) grow with each level.
        if (player != null)
            player.effectManager.MarkDirty();
        OnLevelChanged?.Invoke(levelsGained);
    }

    private void ProcessLevelUpsFromCurrentXP()
    {
        int levelsGained = 0;

        while (currentXP >= XPRequiredForNextLevel)
        {
            currentXP -= XPRequiredForNextLevel;
            currentLevel++;
            levelsGained++;
        }
        
        if (levelsGained <= 0)
            return;

        RaiseLevelChanged(levelsGained);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

    }
}