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

    public float XPReward => xpReward;

    public override void Die()
    {
        if (IsDead)
            return;

        GameController.Instance.AddXP(xpReward);
        base.Die();
    }
}
