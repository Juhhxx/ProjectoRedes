using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "Player", menuName = "Scriptable Objects/Player")]
public class Player : ScriptableObject
{
    [field: SerializeField] public string Name;
    [field: SerializeField] public int Level;
    [field: SerializeField] public Creature CreatureData;

    [SerializeField] private Creature _creature;
    public Creature Creature => _creature;
    public void SetUpPlayer()
    {
        _creature = CreatureData.CreateCreature(this);
    }
}
