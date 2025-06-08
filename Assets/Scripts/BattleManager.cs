using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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

    public void RegisterAction(string creature, int attackId, int speed)
    {
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

    private void DoAttack(string attackString)
    {
        string[] attackInfo = attackString.Split("|");

        Debug.Log($"{attackInfo[0]} DOING ATTACK {attackInfo[1]}");

        Attack attack = GetAction(attackInfo[0], int.Parse(attackInfo[1]));

        (float damage, float recoil) = attack.DoAttack();

        Debug.Log(damage);

        if (damage > 0)
        {
            StartCoroutine(attack.Target.ApplyDamage(damage));

            if (recoil > 0) StartCoroutine(attack.Attacker.ApplyDamage(recoil));
        }
    }
    private bool CheckWin()
    {
        foreach (Player p in _players)
        {
            if (p.Creature.CurrentHP == 0)
            {
                return true;
            }
        }
        return false;
    }

    public IEnumerator BattleStart()
    {
        Debug.Log("START BATTLE COROUTINE");

        while (!CheckWin())
        {
            Turn++;

            yield return new WaitForPlayerActions(() => _playerActions.Count == _actionsListSize);
            yield return new WaitForSeconds(1);

            OrganizeActions();

            yield return new WaitForEndOfFrame();

            DoAttack(_playerActions[0]);

            _dialogueManager.StartDialogues();

            yield return new WaitForEndOfFrame();
            yield return _wfd;

            DoAttack(_playerActions[1]);

            _dialogueManager.StartDialogues();

            yield return new WaitForEndOfFrame();
            yield return _wfd;

            _ui.SetUpActionScene();
            _playerActions.Clear();
        }

        foreach (Player p in _players)
            if (p.Creature.CurrentHP > 0)
                p.SetEXP(p.EXP + 10);
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

}
