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
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
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

            // If we are in a matched connection, there is no UI for starting a
            // so we need to start it automatically once we have 2 players conected
            
            if (IsHost && _matchConnection)
            {
                if (_numberOfClients == 2) StartBattle();
                _matchConnection = false;
            }
        }
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"Successful Login for Player {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.Log($"Account Sign In Error : {e}");
        }
    }

    // Relay Connection with Matchmaking
    public async void FindMatch()
    {
        if (!AccountManager.Instance.IsLoggedIn) return;

        Debug.Log("Looking for Match");

        LoadingScreenActivator.Instance.ToogleScreen(true);

        await InitializeUnityServices();

        _matchConnection = true;

        try
        {
            Debug.Log($"[Network] [Matchmaker Manager] Find Match!");

            // Create matchmaker options to specify the correct Queue to use and the player parameters
            var matchmakerOptions = new MatchmakerOptions
            {
                QueueName = "PlayerEXP",
                PlayerProperties = new Dictionary<string, PlayerProperty>() {
                { "EXP", new PlayerProperty(_localPlayer.Player.EXP.ToString())}
            }
            };

            // Create session options to specify number of players and Relay usage
            var sessionOptions = new SessionOptions() { MaxPlayers = 2 }.WithRelayNetwork();

            // Create a cancellation source for 
            var cancellationSource = new CancellationTokenSource();

            // Ask the Multiplayer Services to Matchmake a Session based on the previous parameters
            _matchedSession = await MultiplayerService.Instance.MatchmakeSessionAsync(matchmakerOptions, sessionOptions, cancellationSource.Token);

            // Store the results of the Matchmaked Session
            StoredMatchmakingResults matchmakingResults = await MatchmakerService.Instance.GetMatchmakingResultsAsync(_matchedSession.Id);

            Debug.Log($"[Network] [Matchmaker Manager] Matchmaking results: {matchmakingResults}");

        }
        catch (Exception e)
        {
            Debug.Log($"[Network] [Matchmaker Manager] Matchmaking failed : {e}!");
        }
        finally
        {
            LoadingScreenActivator.Instance.ToogleScreen(false);
        }
    }

    // Private Connection
    public async Task<string> StartPrivateHosting()
    {
        LoadingScreenActivator.Instance.ToogleScreen(true);

        await InitializeUnityServices();

        // Get Relay Allocation
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Create RelayServerData
        RelayServerData relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        // Set UnityTransport Server Data
        transport.SetRelayServerData(relayData);

        // Get the Join Code
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        LoadingScreenActivator.Instance.ToogleScreen(false);

        // If the Server started, return join code, if not, return null
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    // Client code
    public async Task<bool> StartPrivateClient(string joinCode)
    {
        await InitializeUnityServices();

        // Get join allocation from Relay
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Create RelayServerData
        RelayServerData relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

        // Set UnityTransport Server Data
        transport.SetRelayServerData(relayData);

        // Return the result of the StartClient method
        return NetworkManager.Singleton.StartClient();
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} connected!");
    }
    private void OnClientDisconnected(ulong clientId)
    {
        ToogleMainMenuClientRpc(true);
        if (IsServer) Destroy(_serverBattle?.gameObject);
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
