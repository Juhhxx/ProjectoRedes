using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class Attack : ScriptableObject
{
    [field: SerializeField] public string Name;
    [field: SerializeField] public int Power;
    [field: SerializeField] public int PP;
    [field: SerializeField] public int Accuracy;
    [field: SerializeField] public Type Type;
    [field: SerializeField] public string Message;

    private Creature _attacker;
    public Creature Attacker => _attacker;
    private int _timesUsed;
    public int CurrenPP => PP - _timesUsed;

    public void SetAttacker(Creature creature)
    {
        _attacker = creature;
        Debug.Log($"{creature.Name} = {_attacker.Name}");
    }
    public void Used() => _timesUsed++;
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

    public string GetAttackMessage() => Message.Replace("$name", _attacker.Name)
                                               .Replace("$attackname", Name);

    public Attack CreateAttack()
    {
        return Instantiate(this);
    }

}
