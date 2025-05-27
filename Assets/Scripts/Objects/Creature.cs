using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

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
    [field: SerializeField] public int HP { get; private set; }
    [field: SerializeField] public int Attack { get; private set; }
    [field: SerializeField] public int Defense { get; private set; }
    [field: SerializeField] public int Speed { get; private set; }

    private int _currentHP;
    public int CurrentHP => _currentHP;
    private Attack[] _currentAttackSet = new Attack[4];
    public Attack[] CurrentAttackSet => _currentAttackSet;

    public void AddAttack(int id, Attack attack) => _currentAttackSet[id] = attack;

    public Creature CreateCreature() => Instantiate(this);
}
