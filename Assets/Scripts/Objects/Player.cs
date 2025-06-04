using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "Player", menuName = "Scriptable Objects/Player")]
public class Player : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public int Level { get; private set; }
    [field: SerializeField] public int EXP { get; private set; }
    [field: SerializeField] public Creature CreatureData { get; private set; }

    private Creature _creature;
    public Creature Creature => _creature;
    public void SetName(string name) => Name = name;
    public void SetCreature(Creature creature) => _creature = creature;

    public Player CreatePlayer(string name)
    {
        Player newPlr = Instantiate(this);

        _creature = CreatureData.CreateCreature(this);

        return newPlr;
    }
}
