using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Linq;
using Unity.VisualScripting;
using Unity.Netcode.Transports.UTP;

public class LANGameConnection : NetworkBehaviour
{
    [Header("General")]
    [Space(5)]
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
    private bool _serverOn;

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

        NetworkManager.Singleton.OnServerStarted += () => _serverOn = true;
        NetworkManager.Singleton.OnServerStopped += (bool i) => _serverOn = false;
    }
    private void Update()
    {
        if (_serverOn)
        {
            if (IsServer)
            {
                _numberOfClients = NetworkManager.Singleton.ConnectedClientsList.Count;
                _playersText.text = $"Players Connected : {_numberOfClients}/2";
                Debug.Log($"Players Connected : {_numberOfClients}/2");
            }
        }
        
    }

    private void StartHosting()
    {
        string adress = GetLocalIPv4();

        UnityTransport transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();

        transport.SetConnectionData(adress, 7777);

        NetworkManager.Singleton.StartHost();

        _joinCodeText.text = adress;
        _mainMenu.SetActive(false);
        _hostMenu.SetActive(true);
    }

    private void StartClient()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += (ulong i) =>
        SetMessage("Connection Failed!\nJoin Code might be incorrect.");

        NetworkManager.Singleton.OnClientConnectedCallback += (ulong i) =>
        SetMessage("Connection Successful!\nWaiting for Host to Start Battle!");

        _mainMenu.SetActive(false);
        _clientMenu.SetActive(true);
    }
    private void ConnectClient()
    {
        string adress = _joinCodeInput.text;
        UnityTransport transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();

        transport.SetConnectionData(adress, 7777);

        NetworkManager.Singleton.StartClient();
    }

    private void SetMessage(string message)
    {
        if (!_messageText.gameObject.activeInHierarchy) _messageText.gameObject.SetActive(true);

        _messageText.text = message;
    }
    
    // Get Local IP Adress
    private string GetLocalIPv4()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
        .AddressList.First(
        f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        .ToString();
    }
}
