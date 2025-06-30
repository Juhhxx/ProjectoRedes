using TMPro;
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
        _profileText.text = $"{Player.Name} Lv. {Player.Level} ({Player.EXP:0000})";
    }

    private void IncreaseScore()
    {
        if (!AccountManager.Instance.IsLoggedIn) return;
        _player.SetEXP(_player.EXP + 100);
        UpdatePlayer();
    }
    private void DecreaseScore()
    {
        if (!AccountManager.Instance.IsLoggedIn) return;
        _player.SetEXP(_player.EXP - 100);
        UpdatePlayer();
    }

    private void Awake()
    {
        _loginScript.SetPlayer(this);
        _creatureMenus.SetPlayer(this);
        _accountManager.SetPlayer(this);
        _profileText.text = "Not Signed In";
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.P)) IncreaseScore();
            if (Input.GetKeyDown(KeyCode.O)) DecreaseScore();
        }
    }

}
