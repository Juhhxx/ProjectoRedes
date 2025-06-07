using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Linq;
using System;
using System.Collections.Generic;
public class ConnectionManager : NetworkBehaviour
{
    [Header("General")]
    [Space(5)]
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private Player _playerBase;
    [SerializeField] private GameObject _battleScreen;
    private BattleManager _serverBattle;
    private List<Player> _players;

    private int _numberOfClients;
    private bool _serverOn;

    public static ConnectionManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;        
    }
    private void Start()
    {
        _players = new List<Player>();

        NetworkManager.Singleton.OnServerStarted += () => _serverOn = true;
        NetworkManager.Singleton.OnServerStopped += (bool i) => _serverOn = false;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
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
        }
    }

    public string StartHosting()
    {
        string adress = GetLocalIPv4();

        UnityTransport transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();

        transport.SetConnectionData(adress, 7777);

        NetworkManager.Singleton.StartHost();

        return adress;
    }

    public bool StartClientLAN(string code)
    {
        string adress = code;

        UnityTransport transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();

        transport.SetConnectionData(adress, 7777);

        bool result = NetworkManager.Singleton.StartClient();

        return result;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        _serverBattle ??= _battleScreen.GetComponent<BattleManager>();

        Debug.Log($"Player {clientId} connected!");
    }

    public void StartBattle()
    {
        if (_numberOfClients != 2) return;

        GetPlayerData();

        foreach (Player p in _players)
            _serverBattle.AddPlayer(p);

        _serverBattle.SetUp();
    }

    private void GetPlayerData()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

            PlayerData pd = playerObject.GetComponent<PlayerNetwork>().Player;

            Player newP = _playerBase.CreatePlayer(pd.Name);

            newP.SetEXP(pd.EXP);
            newP.LoadCreature(pd.Creature, pd.Move1, pd.Move2, pd.Move3, pd.Move4);

            _players.Add(newP);
        }
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
