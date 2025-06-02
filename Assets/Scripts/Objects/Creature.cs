using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;
using System.Collections;

[CreateAssetMenu(fileName = "Creature", menuName = "Scriptable Objects/Creature")]
public class Creature : ScriptableObject
{
    [SerializeField][ShowAssetPreview] private Sprite _frontSprite;
    public Sprite FrontSprite => _frontSprite;
    [SerializeField][ShowAssetPreview] private Sprite _backSprite;
    public Sprite BackSprite => _backSprite;
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public Type Type { get; private set; }
    [field: SerializeField] public List<Attack> Attacks { get; private set; }
    [field: SerializeField] public float BaseHP { get; private set; }
    [field: SerializeField] public int BaseAttack { get; private set; }
    [field: SerializeField] public int BaseDefense { get; private set; }
    [field: SerializeField] public int BaseSpeed { get; private set; }

    public float HP => BaseHP + (_owner?.Level * 2).Value;
    public int Attack => BaseAttack + (_owner?.Level * 2).Value + GetModifierBonus(Stats.Attack);
    public int Defense => BaseDefense + (_owner?.Level * 2).Value + GetModifierBonus(Stats.Defense);
    public int Speed => BaseSpeed + (_owner?.Level * 2).Value + GetModifierBonus(Stats.Speed);
    private int _currentTurn;
    public void SetTurn(int turn) => _currentTurn = turn;

    private List<StatModifier> _statModifiers = new List<StatModifier>();
    public void AddModifier(Attack owner, StatModifier modifier)
    {
        string message = "";
        string animation = "";

        _statModifiers.Add(modifier);
        _dialogue.AddDialogue(owner.GetAttackMessage());

        message += $"{Name}'s {modifier.Stat}";
        animation += $"{modifier.Stat}";

        if (modifier.Amount > 0)
        {
            message += "\nrose !";
            animation += "Up";
        }
        else
        {
            message += "\nfell !";
            animation += "Down";
        }

        _dialogue.AddDialogue(message);
        _anim.SetTrigger(animation);
    }
    public void CheckModifier()
    {
        foreach (StatModifier m in _statModifiers)
        {
            m.TurnPass();
        }

        _statModifiers.RemoveAll(m => m.CheckDone());
    }
    private int GetModifierBonus(Stats stat)
    {
        if (_statModifiers.Count == 0) return 0;

        int bonus = 0;

        foreach (StatModifier m in _statModifiers)
        {
            if (m.Stat == stat) bonus += m.Amount;
        }

        return bonus;
    }

    public event Action OnDamageTaken;

    private Player _owner;
    public Player Owner => _owner;
    private float _currentHP;
    public float CurrentHP => _currentHP;
    private Attack[] _currentAttackSet = new Attack[4];
    public Attack[] CurrentAttackSet => _currentAttackSet;
    private Animator _anim;
    public Animator Animator => _anim;
    public void SetAnimator(Animator anim) => _anim = anim;
    private DialogueManager _dialogue;
    public void SetDialogueManager(DialogueManager dialogue) => _dialogue = dialogue;

    private YieldInstruction _wff = new WaitForEndOfFrame();

    public void SetOwner(Player owner) => _owner = owner;
    public void SetOpponent(Creature opponent)
    {
        foreach (Attack a in _currentAttackSet)
        {
            a.SetTarget(opponent);
        }
    }
    public void AddAttack(int id, Attack attack)
    {
        Attack newAttack = attack.CreateAttack();

        _currentAttackSet[id] = newAttack;
        newAttack.SetAttacker(this);
    }
    public void SetHP(float hp) => _currentHP = hp;

    public (float, float) TakeDamage(Attack attack)
    {
        Debug.Log($"{attack.Attacker.Name} used {attack.Name} on {Name}");
        int rnd = UnityEngine.Random.Range(1, 100);

        if (rnd > attack.Accuracy)
        {
            _dialogue.AddDialogue($"{attack.Attacker.Name} missed...");

            return (0f, 0f);
        }

        float damage = CalculateDamage(attack);

        Debug.Log($"DAMAGE : {damage}");

        attack.Used();

        _dialogue.AddDialogue(attack.GetAttackMessage());
        if (attack.GetEffectiveness(Type) > 1.0f) _dialogue.AddDialogue("It's super effective!!!");
        else if (attack.GetEffectiveness(Type) < 1.0f) _dialogue.AddDialogue("It's not very effective...");

        float recoilDamage = 0f;

        if (attack.HasRecoil)
        {
            recoilDamage = damage / 2;
            _dialogue.AddDialogue($"{attack.Attacker.Name} got damaged by recoil");
        }

        if (damage > 0) attack.Attacker.Animator.SetTrigger("Attack");

        return (damage, recoilDamage);
    }
    private float CalculateDamage(Attack attack)
    {
        float rnd = UnityEngine.Random.Range(217, 255);
        rnd /= 255;

        float damage = (((((2 * attack.Attacker.Owner.Level * attack.CriticalChance()) / 5)
                        * attack.Power * (attack.Attacker.Attack / Defense)) / 50) + 2)
                        * attack.GetSTAB() * attack.GetEffectiveness(Type) * rnd;

        return Mathf.Ceil(damage);
    }
    public IEnumerator ApplyDamage(float damage)
    {
        float objective = _currentHP - damage;

        if (objective < 0f) objective = 0f;

        while (_currentHP > objective)
        {

            _currentHP -= 0.1f;

            OnDamageTaken?.Invoke();

            Debug.Log($"APPLYING DAMAGE");

            yield return new WaitForSeconds(0.005f);
        }
    }

    public Creature CreateCreature(Player owner)
    {
        Creature newC = Instantiate(this);

        newC.SetOwner(owner);
        newC.SetHP(newC.HP);

        // Temporary
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"{Attacks[i].Name} n{i} to {Name} ({name})");
            Debug.Log(i);
            newC.AddAttack(i, Attacks[i]);
        }

        return newC;
    }
}
