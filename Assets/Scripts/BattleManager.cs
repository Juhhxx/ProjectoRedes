using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BattleManager : NetworkBehaviour
{
    [SerializeField] private List<Player> _players;
    [SerializeField][Range(1, 2)] private int _actionsListSize;

    [SerializeField] private GameObject _mainMenuUI;
    [SerializeField] private UpdateUI _ui;
    [SerializeField] private DialogueManager _dialogueManager;
    private List<Attack> _playerActions;
    public event Action OnTurnPassed;
    private int _turn;
    public int Turn
    {
        get => _turn;

        private set
        {
            OnTurnPassed?.Invoke();
            _turn = value;
        }
    }

    private WaitForDialogueEnd _wfd;

    public void SetUp()
    {
        Debug.LogWarning("AAAAAAAAA");

        SetPlayers();

        _playerActions = new List<Attack>();

        OnTurnPassed += () => Debug.Log($"TURN {Turn}");

        _wfd = new WaitForDialogueEnd(_dialogueManager);

        SetUpPlayersUIClientRpc();

        StartCoroutine(Test());
    }

    [ClientRpc]
    private void SetUpPlayersUIClientRpc()
    {

        _dialogueManager.SetUpDialogueManager();
        _ui.SetUpUI();

        _mainMenuUI.SetActive(false);
        gameObject.SetActive(true);
    }

    private void SetPlayers()
    {
        for (int i = 0; i < 2; i++)
        {
            int otherIdx = i == 0 ? 1 : 0;

            _players[i].Creature.SetOpponent(_players[otherIdx].Creature);

            OnTurnPassed += () => _players[i].Creature.SetTurn(Turn);
            OnTurnPassed += () => _players[i].Creature.CheckModifier();
        }
    }
    
    public void AddPlayer(Player player)
    {
        if (_players.Count < 2) _players.Add(player);
    }

    [ServerRpc]
    public void RegisterActionServerRpc(string creature, int attackId)
    {
        Attack attack = GetAction(creature, attackId);

        if (attack.CurrenPP == 0) return;

        _playerActions.Add(attack);
    }
    private Attack GetAction(string creature, int attack)
    {
        foreach (Player p in _players)
        {
            if (p.Creature.Name == creature)
            {
                return p.Creature.CurrentAttackSet[attack];
            }
        }
        return null;
    }

    private void DoAttack(Attack attack)
    {
        (float damage, float recoil) = attack.DoAttack();

        Debug.Log(damage);

        if (damage > 0)
        {
            StartCoroutine(attack.Target.ApplyDamage(damage));

            if (recoil > 0) StartCoroutine(attack.Attacker.ApplyDamage(recoil));
        }
    }

    private IEnumerator Test()
    {
        while (true)
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
    }
    public IEnumerator BattleStart()
    {
        yield return new WaitForPlayerActions(() => _playerActions.Count == _actionsListSize);

        OrganizeActions();

        _dialogueManager.StartDialogues();
    }
    private void OrganizeActions()
    {
        if (_playerActions[0].Attacker.Speed < _playerActions[1].Attacker.Speed)
        {
            Attack tmp = _playerActions[0];

            _playerActions[0] = _playerActions[1];
            _playerActions[1] = tmp;
        }
    }

}
