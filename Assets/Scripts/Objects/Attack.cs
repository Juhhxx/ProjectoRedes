using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class Attack : ScriptableObject
{
    [field: SerializeField] public string Name;
    [field: SerializeField] public AttackType AttackType;
    [ShowIf("AttackType", AttackType.Physical)][field: SerializeField] public int Power;
    [ShowIf("AttackType", AttackType.Physical)][field: SerializeField] public int Accuracy;
    [ShowIf("AttackType", AttackType.StatBooster)][field: SerializeField] public List<Stats> Stat;
    [ShowIf("AttackType", AttackType.StatBooster)][field: SerializeField] public float AmountPercent;
    [ShowIf("AttackType", AttackType.StatBooster)][field: SerializeField] public int TurnDuration;
    [field: SerializeField] public int PP;
    [field: SerializeField] public Type Type;
    [field: SerializeField] public string Description;

    private Creature _attacker;
    public Creature Attacker => _attacker;
    private Creature _target;
    public Creature Target => _target;
    private int _timesUsed;
    public int CurrenPP => PP - _timesUsed;

    public void SetAttacker(Creature creature)
    {
        _attacker = creature;

        if (AttackType == AttackType.StatBooster)
        {
            Power = 0;
            Accuracy = 100;
        }
    }
    public void SetTarget(Creature creature)
    {
        _target = creature;
    }
    public void ResetUsedTimes() => _timesUsed = 0;
    public void Used() => _timesUsed++;

    public float DoAttack()
    {
        switch (AttackType)
        {
            case AttackType.Physical:
                return DoDamage();

            case AttackType.StatBooster:
                AddModifier();
                break;
        }

        return 0f;
    }
    private float DoDamage()
    {
        Debug.Log($"ATTACK {Name} from {Attacker.Name}");

        float damage = Target.TakeDamage(this);

        return damage;
    }
    private void AddModifier()
    {
        foreach (Stats s in Stat)
        {
            int amount = 0;

            switch (s)
            {
                case Stats.Attack:
                    amount += (int)(Attacker.Attack * AmountPercent);
                    break;

                case Stats.Defense:
                    amount += (int)(Attacker.Defense * AmountPercent);
                    break;

                case Stats.Speed:
                    amount += (int)(Attacker.Speed * AmountPercent);
                    break;
            }

            StatModifier mod = new(s, amount, TurnDuration);

            Attacker.AddModifier(this, mod);
        }
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
        int rnd = Random.Range(1, 100);

        if (rnd < 25) return 2.0f;
        else return 1.0f;
    }

    public string GetAttackMessage() => $"{Attacker.Name,-7} used {Name} !";

    public Attack CreateAttack()
    {
        Attack newAtk = Instantiate(this);

        newAtk.ResetUsedTimes();

        return newAtk;
    }

}
