
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : NetworkBehaviour
{
    [SerializeField] private List<Player> _players;
    [SerializeField] private Player _playerBase;
    private List<PlayerData> _playerDataAdd = new List<PlayerData>();
    private List<PlayerData> _playerDatas = new List<PlayerData>();
    [SerializeField][Range(1, 2)] private int _actionsListSize;

    [SerializeField] private UpdateUI _ui;
    [SerializeField] private DialogueManager _dialogueManager;
    private List<string> _playerActions;
    private int _playerDoneReading;
    private bool _hasWinner;
    private ulong _winnerID;
    private string _winnerName;

    public event Action OnTurnPassed;
    private int _turn;
    public int Turn
    {
        get => _turn;

        private set
        {
            _turn = value;
            OnTurnPassed?.Invoke();
        }
    }

    private WaitForDialogueEnd _wfd;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        if (_playerDataAdd.Count > 0)
        {
            foreach (PlayerData p in _playerDataAdd) AddPlayerDatasClientRpc(p);

            SetPlayersClientRpc();
        }
        else
        {
            Debug.LogWarning("PLAYERS DATA NOT SYNCHORINZED");
        }
    }

    private void SetUp()
    {
        if (!IsServer) return;

        Debug.LogWarning("BBBBBBBB");

        _playerActions = new List<string>();

        OnTurnPassed += () => Debug.Log($"TURN {Turn}");

        _wfd = new WaitForDialogueEnd(_dialogueManager);

        Debug.LogWarning($"PLAYER 1 ID : {_players[0].ID}");
        Debug.LogWarning($"PLAYER 2 ID : {_players[1].ID}");

        InitializeUIClientRpc();

        Debug.LogWarning("BBBBBBBB");
        StartCoroutine(BattleStart());
    }

    [ClientRpc]
    private void InitializeUIClientRpc()
    {
        foreach (Player p in _players)
        {
            if (p.ID == NetworkManager.Singleton.LocalClientId)
            {
                _dialogueManager.SetUpDialogueManager();
                _ui.SetUpUI(p);
                break;
            }
        }
    }

    public void AddPlayers(List<PlayerData> players)
    {
        if (players.Count == 2)
            foreach (PlayerData p in players) _playerDataAdd.Add(p);

    }
    [ClientRpc]
    private void AddPlayerDatasClientRpc(PlayerData p)
    {
        _playerDatas.Add(p);
    }

    [ClientRpc]
    private void SetPlayersClientRpc()
    {
        _players.Clear();

        for (int i = 0; i < 2; i++)
        {
            int icapture = i;

            Player newP = _playerBase.CreatePlayer(_playerDatas[i].Name);

            newP.SetEXP(_playerDatas[i].EXP);
            newP.SetId(_playerDatas[i].ID);
            newP.LoadCreature(_playerDatas[i].Creature, _playerDatas[i].Move1,
                                                        _playerDatas[i].Move2,
                                                        _playerDatas[i].Move3,
                                                        _playerDatas[i].Move4);

            _players.Add(newP);
            Debug.Log($"Add player {newP.Name} in {NetworkManager.Singleton.LocalClientId}, idP {newP.ID}");

            OnTurnPassed += () => _players[icapture].Creature.SetTurn(Turn);
            OnTurnPassed += () => _players[icapture].Creature.CheckModifier();
        }
        for (int i = 0; i < 2; i++)
        {
            int otherIdx = i == 0 ? 1 : 0;
            _players[i].Creature.SetOpponent(_players[otherIdx].Creature);
        }

        SetUp();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterActionServerRpc(string creature, int attackId, int speed)
    {
        Debug.Log($"ACTION REGISTRED FOR {creature}");

        string attack = creature + "|" + attackId + "|" + speed;

        _playerActions.Add(attack);
    }
    private Attack GetAction(string creature, int attack)
    {
        foreach (Player p in _players)
        {
            if (p.Creature.Name == creature)
            {
                Debug.Log($"{creature}, long {p.Creature.CurrentAttackSet.Count}, id{attack}");
                return p.Creature.CurrentAttackSet[attack];
            }
        }
        return null;
    }

    [ClientRpc]
    private void DoAttackClientRpc(string attackString)
    {
        string[] attackInfo = attackString.Split("|");

        Debug.Log($"{attackInfo[0]} DOING ATTACK {attackInfo[1]}");

        Attack attack = GetAction(attackInfo[0], int.Parse(attackInfo[1]));

        (float damage, float recoil) = attack.DoAttack();

        Debug.Log(damage);

        if (attack.Target.CurrentHP - damage <= 0)
        {
            _hasWinner = true;
            _winnerID = attack.Attacker.Owner.ID;
            _winnerName = attack.Attacker.Owner.Name;
        }

        if (damage > 0)
        {
            StartCoroutine(attack.Target.ApplyDamage(damage));

            if (recoil > 0) StartCoroutine(attack.Attacker.ApplyDamage(recoil));
        }

        Debug.Log($"CHECK OPPONENT HP : {attack.Target.CurrentHP}");
    }

    [ClientRpc]
    private void UpdateDialogueClientRpc()
    {
        _dialogueManager.StartDialogues();
    }

    [ClientRpc]
    private void ClearDialogueClientRpc()
    {
        _dialogueManager.ClearDialogues();
    }

    [ClientRpc]
    private void UpdateUIClientRpc()
    {
        _ui.SetUpActionScene();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RegisterDoneReadingServerRpc() => _playerDoneReading++;

    public IEnumerator BattleStart()
    {
        Debug.Log("START BATTLE COROUTINE");

        while (!_hasWinner)
        {
            UpdateUIClientRpc();

            Debug.Log($"HAS WINNER : {_hasWinner}");
            Turn++;

            yield return new WaitForPlayerActions(() => _playerActions.Count == _actionsListSize);
            yield return new WaitForSeconds(1);

            OrganizeActions();

            yield return new WaitForEndOfFrame();

            DoAttackClientRpc(_playerActions[0]);

            UpdateDialogueClientRpc();

            yield return new WaitForEndOfFrame();
            yield return _wfd;

            DoAttackClientRpc(_playerActions[1]);

            UpdateDialogueClientRpc();

            yield return new WaitForEndOfFrame();
            yield return _wfd;

            // RegisterDoneReadingServerRpc();

            // yield return new WaitUntil(() => _playerDoneReading == 2);

            _playerDoneReading = 0;
            _playerActions.Clear();
            ClearDialogueClientRpc(); // Clear Dialogues list for all clients to prevent de-synchronization in the tests
        }

        FinnishBattleClientRpc(_winnerID, _winnerName);
    }
    private void OrganizeActions()
    {
        if (int.Parse(_playerActions[0].Split("|")[2]) < int.Parse(_playerActions[1].Split("|")[2]))
        {
            string tmp = _playerActions[0];

            _playerActions[0] = _playerActions[1];
            _playerActions[1] = tmp;
        }
    }

    [ClientRpc]
    private void FinnishBattleClientRpc(ulong winnerID, string winnerName)
    {
        _dialogueManager.StartDialogues($"{winnerName} WON!");

        if (winnerID == NetworkManager.Singleton.LocalClientId)
        {
            PlayerController p = FindAnyObjectByType<PlayerController>(0);
            p.Player.SetEXP(p.Player.EXP + 50);
            AccountManager.Instance.SavePlayerData(
                new Dictionary<string, string>()
                {
                    { "EXP", p.Player.EXP.ToString() }
                }
            );
        }

        StartCoroutine(NerworkShutdown());
    }
    private IEnumerator NerworkShutdown()
    {
        yield return new WaitForSeconds(2);

        ConnectionManager.Instance.ToogleMainMenu(true);
        NetworkManager.Singleton.Shutdown();
    }

}
