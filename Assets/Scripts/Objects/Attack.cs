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
    private int _timesUsed;
    public int CurrenPP => PP - _timesUsed;

    public string GetAttackMessage() => Message.Replace("$name", _attacker.Name)
                                               .Replace("$attackname", Name);

}
