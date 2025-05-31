using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Expandable][SerializeField] private Player p1, p2;
    public Player P1 => p1;
    public Player P2 => p2;
    [SerializeField][Range(1, 2)] private int _actionsListSize;

    [SerializeField] private UpdateUI _ui;
    [SerializeField] private DialogueManager _dialogueManager;
    private List<Attack> _playerActions;

    private WaitForDialogueEnd _wfd;

    private void Awake()
    {
        p1.SetUpPlayer();
        p2.SetUpPlayer();
    }
    private void Start()
    {
        _playerActions = new List<Attack>();
        _dialogueManager.SetUpDialogueManager();
        _ui.SetUpUI(this);

        _wfd = new WaitForDialogueEnd(_dialogueManager);
    }

    public void RegisterAction(Attack attack)
    {
        _playerActions.Add(attack);

        _dialogueManager.StartDialogues($"Waiting for {p2.Name}'s action...");

        StartCoroutine(Test());
    }
    private void DoAttack(Attack attack, Creature defender)
    {
        Debug.Log($"ATTACK {attack.Name} from {attack.Attacker.Name}");

        float damage = defender.TakeDamage(attack, _dialogueManager);

        Debug.Log(damage);

        if (damage > 0) StartCoroutine(defender.ApplyDamage(damage));
    }

    private IEnumerator Test()
    {
        yield return new WaitForSeconds(5f);

        DoAttack(_playerActions[0], p2.Creature);

        _dialogueManager.StartDialogues();

        yield return _wfd;

        yield return new WaitForKeyDown("Submit");

        _ui.SetUpActionScene();
        _playerActions.Clear();
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
