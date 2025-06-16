using TMPro;
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
    [SerializeField] private AccountManager _accountManager;
    [SerializeField] private TextMeshProUGUI _profileText;

    public void SetUpPlayer(Player player)
    {
        _player = player;
        Debug.Log($"Set Up {Player.Name}");
        _profileText.text = $"{Player.Name} Lv. {Player.Level} ({Player.EXP:0000})";
    }

    public void UpdatePlayer()
    {
        _creatureMenus.UpdateShowcase();
    }

    private void Awake()
    {
        _loginScript.SetPlayer(this);
        _creatureMenus.SetPlayer(this);
        _accountManager.SetPlayer(this);
        _profileText.text = "Not Signed In";
    }

}
