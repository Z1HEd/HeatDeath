using UnityEngine;

public class Enemy: Ship
{
    [SerializeField]
    protected Player _player;
    public Player Player {
        get {
            if (_player == null)
                _player = FindAnyObjectByType<Player>();
            return _player; } 
        protected set { _player = value; }
        }
    

}
