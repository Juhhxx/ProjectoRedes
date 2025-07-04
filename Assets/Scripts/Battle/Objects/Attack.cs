using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class Attack : ScriptableObject
{
    [field: SerializeField] public string Name;
    [field: SerializeField] public AttackType AttackType;
    private bool IsStatModifier => AttackType != AttackType.Physical;
    [HideIf("IsStatModifier")][field: SerializeField] public int Power = 0;
    [HideIf("IsStatModifier")][field: SerializeField] public int Accuracy = 100;
    [HideIf("IsStatModifier")][field: SerializeField] public int CritRate = 10;
    [HideIf("IsStatModifier")][field: SerializeField] public bool HasRecoil;
    [ShowIf("IsStatModifier")][field: SerializeField] public List<Stats> Stat;
    [ShowIf("IsStatModifier")][field: SerializeField] public float AmountPercent;
    [ShowIf("IsStatModifier")][field: SerializeField] public bool Randomize;
    [ShowIf("Randomize")][field: SerializeField] public float RandomChance;
    [ShowIf("IsStatModifier")][field: SerializeField] public int TurnDuration;
    [field: SerializeField] public int PP;
    [field: SerializeField] public Type Type;
    [field: SerializeField] public int LevelUnlocked;
    [TextArea][field: SerializeField] public string Description;

    private Creature _attacker;
    public Creature Attacker => _attacker;
    private Creature _target;
    public Creature Target => _target;
    private int _timesUsed;
    public int CurrenPP => PP - _timesUsed;
    private System.Random attackRandom = new System.Random(2);

    public void SetAttacker(Creature creature)
    {
        _attacker = creature;
    }
    public void SetTarget(Creature creature)
    {
        _target = creature;
    }
    public void ResetUsedTimes() => _timesUsed = 0;
    public void Used() => _timesUsed++;

    public (float, float) DoAttack()
    {
        switch (AttackType)
        {
            case AttackType.Physical:
                return DoDamage();

            case AttackType.StatBooster:
                AddModifier(false);
                break;
            
            case AttackType.StatNerfer:
                AddModifier(true);
                break;
        }

        return (0f, 0f);
    }
    private (float, float) DoDamage()
    {
        Debug.Log($"ATTACK {Name} from {Attacker.Name}");

        (float, float) damage = Target.TakeDamage(this);

        return damage;
    }
    private void AddModifier(bool negative)
    {
        float rand = 0;

        // Do randomize per modifier and not per stat so multi stat modifiers 
        // are equal
        if (Randomize)
        {
            rand = (float)attackRandom.NextDouble();
        }

        foreach (Stats s in Stat)
            {
                (Stats stat, int amount) = GetModifierAmount(s);

                if (Randomize)
                {
                    if (rand <= RandomChance) amount = -amount;

                    Debug.Log($"{rand} <= {RandomChance}");
                    Debug.Log($"negative? {rand <= RandomChance} Amount {amount}");
                }

                if (!negative)
                {
                    StatModifier mod = new(stat, amount, TurnDuration);

                    Attacker.AddModifier(this, mod);
                }
                else
                {
                    StatModifier mod = new(stat, -amount, TurnDuration);

                    Target.AddModifier(this, mod);
                }
            }
    }
    private (Stats,int) GetModifierAmount(Stats stat)
    {
        (Stats, int) value = (stat, 0);

        switch (stat)
        {
            case Stats.Attack:
                value.Item2 = (int)(Attacker.Attack * AmountPercent);
                break;

            case Stats.Defense:
                value.Item2 = (int)(Attacker.Defense * AmountPercent);
                break;

            case Stats.Speed:
                value.Item2 = (int)(Attacker.Speed * AmountPercent);
                break;

            case Stats.Random:
                value = GetModifierAmount(stat.GetRandomStat());
                break;
        }

        return value;
    }
    public float GetEffectiveness(Type target)
    {
        if (Type.StrongAgainst.Contains(target)) return 2.0f;
        else if (Type.WeakAgainst.Contains(target)) return 0.5f;
        else return 1.0f;
    }
    public float GetSTAB()
    {
        if (Type == Attacker.Type) return 1.5f;
        else return 1.0f;
    }
    public float CriticalChance()
    {
        int rnd = attackRandom.Next(1, 100);

        if (rnd < CritRate) return 2.0f;
        else return 1.0f;
    }

    public string GetAttackMessage() => $"{Attacker.Name} used \n{Name} !";

    public Attack CreateAttack()
    {
        Attack newAtk = Instantiate(this);

        newAtk.ResetUsedTimes();

        return newAtk;
    }

}
