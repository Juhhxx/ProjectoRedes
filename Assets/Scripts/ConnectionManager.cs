using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplayer;
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
    private bool _serverOn = false;
    private bool _matchConnection = false;
    private ISession _matchedSession;

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

            if (IsHost && _matchConnection)
            {
                if (_numberOfClients == 2) StartBattle();
                _matchConnection = false;
            }
        }
    }

    // Relay Connection
    public async void FindMatch()
    {
        if (!AccountManager.Instance.IsLoggedIn) return;

        Debug.Log("Looking for Match");

        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.Log($"Account Sign In Error : {e}");
        }

        _matchConnection = true;

        try
        {
            Debug.Log($"[Network] [Matchmaker Manager] Find Match!");

            var matchmakerOptions = new MatchmakerOptions
            {
                QueueName = "PlayerEXP",
                PlayerProperties = new Dictionary<string, PlayerProperty>() {
                { "EXP", new PlayerProperty(_localPlayer.Player.EXP.ToString())}
            }
            };

            var sessionOptions = new SessionOptions() { MaxPlayers = 2 }.WithRelayNetwork();

            var cancelationSource = new CancellationTokenSource();

            _matchedSession = await MultiplayerService.Instance.MatchmakeSessionAsync(matchmakerOptions, sessionOptions, cancelationSource.Token);

            StoredMatchmakingResults matchmakingResults =  await MatchmakerService.Instance.GetMatchmakingResultsAsync(_matchedSession.Id);

            Debug.Log($"[Network] [Matchmaker Manager] Matchmaking results: {matchmakingResults}");

        }
        catch (Exception e)
        {
            Debug.Log($"[Network] [Matchmaker Manager] Matchmaking failed : {e}!");
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
    public bool StartClientLAN(string adress, ushort port = 7777)
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
