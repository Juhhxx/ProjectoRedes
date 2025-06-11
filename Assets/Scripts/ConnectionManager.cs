using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Linq;
using System;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Matchmaker;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;



#if UNITY_SERVER
using Unity.Services.Multiplay;
#endif

public class ConnectionManager : NetworkBehaviour
{
    [Header("General")]
    [Space(5)]
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _battleScreen;
    [SerializeField] private PlayerController _localPlayer;
    private BattleManager _serverBattle;
    private List<PlayerData> _players;

    // Server Variables
    private int _numberOfClients;
    private bool _serverOn;
    
#if UNITY_SERVER
    private string _serveripv4Adress = "0.0.0.0";
    private ushort _serverPort = 7777;

    private IMultiplayService _multiplayService;
    private string _allocationId;
    private MultiplayEventCallbacks _serverCallbacks;
#endif

    // Client Variables
    private CreateTicketResponse _createTicketResponse;
    private float _pollTicketTimer;
    private float _pollTicketTimerMax = 1.1f;

    public static ConnectionManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        _players = new List<PlayerData>();

        NetworkManager.Singleton.OnServerStarted += () => _serverOn = true;
        NetworkManager.Singleton.OnServerStopped += (bool i) => _serverOn = false;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        _pollTicketTimer = _pollTicketTimerMax;

#if UNITY_SERVER
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && (i + 1 < args.Length))
            {
                _serverPort = (ushort)int.Parse(args[i + 1]);
            }
        }
        
        await StartServerServices();
        StartServer();
#endif

    }
    private void Update()
    {
        if (_serverOn)
        {
            Debug.Log("SERVER IS ON");
            if (IsServer)
            {
                _numberOfClients = NetworkManager.Singleton.ConnectedClientsList.Count;
                Debug.Log($"Players Connected : {_numberOfClients}/2");
            }

            // If we aren't on a Host (which has a real life user) start battle 
            // when 2 clients are connected

            if (IsServer && !IsHost)
            {
                if (_numberOfClients == 2) StartBattle();
            }
        }

        if (_createTicketResponse != null)
        {
            _pollTicketTimer -= Time.deltaTime;
            if (_pollTicketTimer <= 0f)
            {
                _pollTicketTimer = _pollTicketTimerMax;

                PollMatchmakerTicket();
            }
        }
    }

#if UNITY_SERVER
    //  Online Connection
    private void StartServer()
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            Debug.Log("CREATING SERVER");

             UnityTransport transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();

            transport.SetConnectionData(_serveripv4Adress, _serverPort, "0.0.0.0");

            NetworkManager.Singleton.StartServer();
        }
    }

    private async Task StartServerServices()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        try
        {
            _multiplayService = MultiplayService.Instance;
            await _multiplayService.StartServerQueryHandlerAsync(2, "n/a", "n/a", "0", "n/a");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Something went wrong while setting up SQP Service : {e.Message}");
        }

        try
        {
            var matchmakerPayload = await GetMatchmakerPayLoad(20000);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Something went wrong while setting up Allocation and Backfill : {e.Message}");
        }
        
    }

    private async Task<MatchmakingResults> GetMatchmakerPayLoad(int timeout)
    {
        var matchmakerPayLoadTask = SubscribeAndAwaitMatchmakerAllocation();

        if (await Task.WhenAny(matchmakerPayLoadTask, Task.Delay(timeout)) == matchmakerPayLoadTask)
        {
            return matchmakerPayLoadTask.Result;
        }

        return null;
    }
    private async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if (_multiplayService == null) return null;

        _allocationId = null;
        _serverCallbacks = new MultiplayEventCallbacks();
        _serverCallbacks.Allocate += OnMultiplayAllocation;

        _allocationId = await AwaitAllocationId();

        var mmPayload = await GetMatchmakerAllocationPayloadAsync();

        return mmPayload;
    }

    private async Task<string> AwaitAllocationId()
    {
        var serverConfig = _multiplayService.ServerConfig;

        Debug.Log(
            $"Awaiting Allocation. Server Config Is:\n" +
            $"ServerID : {serverConfig.ServerId}\n" +
            $"AllocationID : {serverConfig.AllocationId}\n" +
            $"Port : {serverConfig.Port}\n" +
            $"QPort : {serverConfig.QueryPort}\n" +
            $"Logs : {serverConfig.ServerLogDirectory}\n"
        );

        while (string.IsNullOrEmpty(_allocationId))
        {
            var configId = serverConfig.AllocationId;

            if (!string.IsNullOrEmpty(configId) && string.IsNullOrEmpty(_allocationId))
            {
                _allocationId = configId;
                _serverPort = serverConfig.Port;
                break;
            }

            await Task.Delay(100);
        }

        return _allocationId;
    }
    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        if (string.IsNullOrEmpty(allocation.AllocationId)) return;

        Debug.Log($"Set Allocation : {allocation.AllocationId}");
        _allocationId = allocation.AllocationId;
    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        try
        {
            var payloadAllocation =
                await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

            var modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);

            Debug.Log($"Got Match Allocation : \n {modelAsJson}");

            return payloadAllocation;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Something went wrong while trying to get matchmaking payload : {e.Message}");
        }

        return null;
    }

#endif
    public async void FindMatch()
    {
        if (!AccountManager.Instance.IsLoggedIn) return;

        Debug.Log("Looking for Match");

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        _createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(new List<Unity.Services.Matchmaker.Models.Player>
        {
            new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId,
            new Dictionary<string, object>() {
                { "EXP", _localPlayer.Player.EXP }
            })
        }, new CreateTicketOptions { QueueName = "EXP" });
    }
    private async void PollMatchmakerTicket()
    {
        TicketStatusResponse ticketStatus = await MatchmakerService.Instance
                                        .GetTicketAsync(_createTicketResponse.Id);

        if (ticketStatus == null)
        {
            //No updates yet
            return;
        }

        if (ticketStatus.Type == typeof(MultiplayAssignment))
        {
            MultiplayAssignment multiplayA = ticketStatus.Value as MultiplayAssignment;

            Debug.Log($"Current Status : {multiplayA.Status}");

            switch (multiplayA.Status)
            {
                case MultiplayAssignment.StatusOptions.Found:
                    _createTicketResponse = null;
                    _pollTicketTimer = _pollTicketTimerMax;

                    string ipv4Adress = multiplayA.Ip;
                    ushort port = (ushort)multiplayA.Port;

                    Debug.Log($"Found : {multiplayA.Ip} : {multiplayA.Port}");

                    UnityTransport transport = NetworkManager.Singleton
                                                .GetComponent<UnityTransport>();

                    transport.SetConnectionData(ipv4Adress, port);

                    StartClient(ipv4Adress, port);
                    break;

                case MultiplayAssignment.StatusOptions.InProgress:
                    Debug.Log("Awaiting Ticket...");
                    break;

                case MultiplayAssignment.StatusOptions.Failed:
                    _createTicketResponse = null;
                    _pollTicketTimer = _pollTicketTimerMax;
                    Debug.Log("Failed Conneting to Server");
                    break;

                case MultiplayAssignment.StatusOptions.Timeout:
                    _createTicketResponse = null;
                    _pollTicketTimer = _pollTicketTimerMax;
                    Debug.Log("Failed Conneting to Server : Timeout!");
                    break;
            }
        }
    }

    // LAN Connection
    public string StartHosting()
    {
        string adress = GetLocalIPv4();

        UnityTransport transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();

        transport.SetConnectionData(adress, 7777);

        NetworkManager.Singleton.StartHost();

        return adress;
    }
    // Get Local IP Adress
    private string GetLocalIPv4()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
        .AddressList.First(
        f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        .ToString();
    }

    // Client code
    public bool StartClient(string adress, ushort port = 7777)
    {
        UnityTransport transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();

        transport.SetConnectionData(adress, port);

        bool result = NetworkManager.Singleton.StartClient();

        return result;
    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} connected!");
    }
    private void OnClientDisconnected(ulong clientId)
    {
        ToogleMainMenuClientRpc(true);
    }

    // Start Battle
    public bool StartBattle()
    {
        if (_numberOfClients != 2) return false;

        GetPlayerData();

        GameObject battle = Instantiate(_battleScreen);
        NetworkObject netBattle = battle.GetComponent<NetworkObject>();

        _serverBattle = battle.GetComponent<BattleManager>();

        _serverBattle.AddPlayers(_players);

        netBattle.Spawn();

        ToogleMainMenuClientRpc(false);
        return true;
    }

    private void GetPlayerData()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

            PlayerData pd = playerObject.GetComponent<PlayerNetwork>().Player;

            _players.Add(pd);
        }
    }

    [ClientRpc]
    private void ToogleMainMenuClientRpc(bool onoff)
    {
        _mainMenu.SetActive(onoff);
    }

}
