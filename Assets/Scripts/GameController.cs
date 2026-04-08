using UnityEngine;
using System;

public class GameController : MonoBehaviour
{
    private static GameController instance;
    
    [SerializeField]
    private int currentLevel = 1;
    [SerializeField]
    private float currentXP = 0f;
    
    public Action OnXPChanged;
    public Action OnLevelChanged;
    
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
        DontDestroyOnLoad(gameObject);
    }

    public static GameController Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("GameController");
                instance = go.AddComponent<GameController>();
            }
            return instance;
        }
    }

    public void AddXP(float xpAmount)
    {
        currentXP += xpAmount;
        Debug.Log($"Added {xpAmount} XP. Total: {currentXP}");
        OnXPChanged?.Invoke();

        while (currentXP >= XPRequiredForNextLevel)
        {
            currentXP -= XPRequiredForNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        Debug.Log($"Level Up! Now level {currentLevel}");
        OnLevelChanged?.Invoke();
        OnXPChanged?.Invoke();
    }

    private float CalculateXPRequiredForLevel(int level)
    {
        return 10f * level;
    }
}
