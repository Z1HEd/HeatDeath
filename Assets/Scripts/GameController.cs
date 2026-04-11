using UnityEngine;
using System;

public class GameController : MonoBehaviour
{
    private static GameController instance;
    
    [SerializeField]
    private int currentLevel = 1;
    [SerializeField]
    private float currentXP = 0f;
    [SerializeField]
    private GameObject gameUIPrefab;
    
    public Action OnXPChanged;
    public Action<int> OnLevelChanged;
    
    public int CurrentLevel => currentLevel;
    public float CurrentXP => currentXP;

    public float XPRequiredForNextLevel => CalculateXPRequiredForLevel(currentLevel + 1);
    public float CurrentLevelProgress => currentXP / XPRequiredForNextLevel;

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
        {
            Instantiate(gameUIPrefab);
        }
    }

    public static GameController Instance => instance;

    public void AddXP(float xpAmount)
    {
        currentXP += xpAmount;
        Debug.Log($"Added {xpAmount} XP. Total: {currentXP}");
        
        ProcessLevelUpsFromCurrentXP();
        OnXPChanged?.Invoke();
    }

    public void Resume()
    {
        Time.timeScale = 1f;
    }

    private float CalculateXPRequiredForLevel(int level)
    {
        return 5f * (level-1);
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

        Debug.Log($"Level Up! Gained {levelsGained} levels. Now level {currentLevel}");
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
