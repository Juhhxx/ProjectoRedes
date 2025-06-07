using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LANGameConnection : NetworkBehaviour
{
    [Header("General")]
    [Space(5)]
    [SerializeField] private GameObject _hostObject;
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;

    [Space(10)]
    [Header("Host")]
    [Space(5)]
    [SerializeField] private GameObject _hostMenu;
    [SerializeField] private TextMeshProUGUI _joinCodeText;
    [SerializeField] private TextMeshProUGUI _playersText;
    [SerializeField] private Button _startBattleButton;

    private int _numberOfClients;

    [Space(10)]
    [Header("Client")]
    [Space(5)]
    [SerializeField] private GameObject _clientMenu;
    [SerializeField] private TMP_InputField _joinCodeInput;
    [SerializeField] private Button _joinBattleButton;
    [SerializeField] private TextMeshProUGUI _messageText;

    private void Start()
    {
        _hostButton.onClick.AddListener(() => StartHosting());
        _joinButton.onClick.AddListener(() => StartClient());

        _joinBattleButton.onClick.AddListener(() => ConnectClient());

        _startBattleButton.onClick.AddListener(() => StartBattle());
    }
    private void Update()
    {
        if (IsServer)
        {
            _numberOfClients = NetworkManager.Singleton.ConnectedClientsList.Count;
            _playersText.text = $"Players Connected : {_numberOfClients}/2";
        }
    }

    public void OpenMenu()
    {
        if (!AccountManager.Instance.IsLoggedIn) return;

        _hostObject.SetActive(true);
    }
    private void StartHosting()
    {
        string adress = ConnectionManager.Instance.StartHosting();

        _joinCodeText.text = adress;
        _mainMenu.SetActive(false);
        _hostMenu.SetActive(true);
    }
    private void StartClient()
    {
        _mainMenu.SetActive(false);
        _clientMenu.SetActive(true);
    }
    private void ConnectClient()
    {
        string adress = _joinCodeInput.text;

        bool result = ConnectionManager.Instance.StartClientLAN(adress);

        if (result) SetMessage("Connection Successful!\nWaiting for Host to Start Battle...");
        else SetMessage("Connection Failed!\nJoin Code might be incorrect.");
    }
    private void SetMessage(string message)
    {
        if (!_messageText.gameObject.activeInHierarchy) _messageText.gameObject.SetActive(true);

        _messageText.text = message;
    }
    private void StartBattle()
    {
        ConnectionManager.Instance.StartBattle();

        _hostMenu.SetActive(false);
        _hostObject.SetActive(false);
    }
}
