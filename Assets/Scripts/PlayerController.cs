using Unity.Netcode;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Player _player;
    public Player Player
    {
        get => _player == null ? _playerBase
                               : _player;

        private set => _player = value;
    }
    [SerializeField] private Player _playerBase;
    [SerializeField] private LoginMenu _loginScript;
    [SerializeField] private CreatureEditor _creatureMenus;

    private void Awake()
    {
        _loginScript.SetPlayer(this);
        _creatureMenus.SetPlayer(this);
    }

}
