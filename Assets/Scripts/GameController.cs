using UnityEngine;
using System;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    private static GameController instance;
    
    [SerializeField]
    private int currentLevel = 1;
    [SerializeField]
    private float currentXP = 0f;
    private List<int> weaponDraftLevels = new List<int> { 1, 5, 10, 20 };
    [SerializeField]
    private GameObject gameUIPrefab;
    [SerializeField]
    private Transform playerSpawnPoint;
    
    public Action OnXPChanged;
    public Action<int> OnLevelChanged;
    
    public int CurrentLevel => currentLevel;
    public float CurrentXP => currentXP;

    public float XPRequiredForNextLevel => CalculateXPRequiredForLevel(currentLevel + 1);
    public float CurrentLevelProgress => currentXP / XPRequiredForNextLevel;

    public bool IsWeaponDraftLevel()
    {
        return IsWeaponDraftLevel(currentLevel);
    }

    public bool IsWeaponDraftLevel(int level)
    {
        if (level <= 0 || weaponDraftLevels == null || weaponDraftLevels.Count == 0)
            return false;
        return weaponDraftLevels.IndexOf(level) != -1;
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

    public static GameController Instance => instance;

    public GameObject SpawnPlayer(ShipDefinition shipDefinition)
    {
        if (shipDefinition == null || shipDefinition.ShipPrefab == null)
        {
            Debug.LogWarning("SpawnPlayer: ShipDefinition or its prefab is null.");
            return null;
        }

        Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        Quaternion spawnRot = playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity;

        return Instantiate(shipDefinition.ShipPrefab, spawnPos, spawnRot);
    }

    public void AddXP(float xpAmount)
    {
        currentXP += xpAmount;
        
        ProcessLevelUpsFromCurrentXP();
        OnXPChanged?.Invoke();
    }

    public void Resume()
    {
        Time.timeScale = 1f;
    }

    private float CalculateXPRequiredForLevel(int level)
    {
        return 5f * (level - 1);
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

        Time.timeScale = 0f;
        OnLevelChanged?.Invoke(levelsGained);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        Time.timeScale = 1f;
    }
}