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

    private void Awake()
    {
        p1.SetUpPlayer();
        p2.SetUpPlayer();

        p1.Creature.SetOpponent(p2.Creature);
        p2.Creature.SetOpponent(p1.Creature);

        OnTurnPassed += () => p1.Creature.SetTurn(Turn);
        OnTurnPassed += () => p1.Creature.CheckModifier();

        OnTurnPassed += () => p2.Creature.SetTurn(Turn);
        OnTurnPassed += () => p2.Creature.CheckModifier();
    }
    private void Start()
    {
        _playerActions = new List<Attack>();
        _dialogueManager.SetUpDialogueManager();
        _ui.SetUpUI(this);

        OnTurnPassed += () => Debug.Log($"TURN {Turn}");

        _wfd = new WaitForDialogueEnd(_dialogueManager);

        StartCoroutine(Test());
    }
    private void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                RegisterAction(p2.Creature.CurrentAttackSet[i]);
            }
        }
    }

    public void RegisterAction(Attack attack)
    {
        _playerActions.Add(attack);
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
            yield return new WaitForPlayerActions(() => _playerActions.Count == 1);

            _dialogueManager.StartDialogues($"Waiting for {p2.Name}'s action...");

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
            Turn++;
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
