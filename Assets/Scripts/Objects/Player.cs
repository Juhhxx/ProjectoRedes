using UnityEngine;
using NaughtyAttributes;
using System.IO;

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
    public void SetLevelEXP(int level, int exp)
    {
        Level = level;
        EXP = exp;
    }
    public void LoadCreature(string name, params string[] moves)
    {
        if (name == null) return;
        
        Creature creature = GetReference<Creature>("Creatures", name).CreateCreature(this);

        Debug.Log(creature.Name);

        SetCreature(creature);

        string attacksPath = Path.Combine("Attacks", name);

        Debug.Log(attacksPath);

        foreach (string move in moves)
        {
            Attack attack = GetReference<Attack>(attacksPath, move);

            creature.AddAttack(attack);
        }
    }
    private T GetReference<T>(string type, string name) where T : ScriptableObject
    {
        string path = Path.Combine("GameAssets",type,name);

        Debug.Log(path);

        return Resources.Load<T>(path);
    }

    public Player CreatePlayer(string name)
    {
        Player newPlr = Instantiate(this);

        newPlr.SetName(name);

        return newPlr;
    }
}
