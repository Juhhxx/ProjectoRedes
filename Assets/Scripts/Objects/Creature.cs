using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "Creature", menuName = "Scriptable Objects/Creature")]
public class Creature : ScriptableObject
{
    [SerializeField][ShowAssetPreview] private Sprite _frontSprite;
    public Sprite FrontSprite => _frontSprite;
    [SerializeField][ShowAssetPreview] private Sprite _backSprite;
    public Sprite BackSprite => _backSprite;
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public Type Type { get; private set; }
    [field: SerializeField] public Attack[] Attacks { get; private set; }
    [field: SerializeField] public int HP { get; private set; }
    [field: SerializeField] public int Attack { get; private set; }
    [field: SerializeField] public int Defense { get; private set; }
    [field: SerializeField] public int Speed { get; private set; }

    private int _currentHP;
    public int CurrentHP => _currentHP;
    private Attack[] _currentAttackSet;

    public Creature CreateCreature() => Instantiate(this);
}
