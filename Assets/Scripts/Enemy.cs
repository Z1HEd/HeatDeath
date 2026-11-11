using UnityEngine;

public class Enemy: Ship
{
    [SerializeField]
    protected Player _player;
    [SerializeField]
    private float xpReward = 50f;

    public Player Player {
        get {
            if (_player == null)
                _player = FindAnyObjectByType<Player>();
            return _player; } 
        protected set { _player = value; }
    }

    // Set from run progress by EnemySpawnController. Enemies placed by hand keep 1.
    private float xpMultiplier = 1f;

    // The base reward, before the run-progress multiplier.
    public float XPReward => xpReward;

    public void SetXPMultiplier(float multiplier)
    {
        xpMultiplier = multiplier;
    }

    public override void Die()
    {
        if (IsDead)
            return;

        GameController.Instance.AddXP(xpReward * xpMultiplier);
        base.Die();
    }
}
