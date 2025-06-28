using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

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
        _hostButton.onClick.AddListener(async () => await StartHosting());
        _joinButton.onClick.AddListener(() => StartClient());

        _joinBattleButton.onClick.AddListener(async () => await ConnectClient());

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
    private async Task StartHosting()
    {
        string joinCode = await ConnectionManager.Instance.StartPrivateHosting();

        if (joinCode != null)
        {
            _joinCodeText.text = joinCode;
            _mainMenu.SetActive(false);
            _hostMenu.SetActive(true);
        }
        else
        {
            Debug.Log("Error Hosting Game");
        }
    }
    private void StartClient()
    {
        _mainMenu.SetActive(false);
        _clientMenu.SetActive(true);
    }
    private async Task ConnectClient()
    {
        string joinCode = _joinCodeInput.text;

        if (joinCode.IsNullOrEmpty())
        {
            SetMessage("No Join Code given.");
            return;
        }

        bool result = await ConnectionManager.Instance.StartPrivateClient(joinCode);

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
        bool result = ConnectionManager.Instance.StartBattle();

        if (result) TurnOffUIClientRpc();
    }
    [ClientRpc]
    private void TurnOffUIClientRpc()
    {
        _hostMenu.SetActive(false);
        _clientMenu.SetActive(false);
        _mainMenu.SetActive(true);
        _hostObject.SetActive(false);
    }
}
