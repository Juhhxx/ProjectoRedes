using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "Player", menuName = "Scriptable Objects/Player")]
public class Player : ScriptableObject
{
    [field: SerializeField] public string Name;
    [field: SerializeField] public int Level;
    [OnValueChanged("CopyCreature")][field: SerializeField] public Creature CreatureData;

    private Creature _creature;
    public Creature Creature
    {
        get
        {
            if (_creature == null) _creature = CreatureData.CreateCreature();

            return _creature;
        }
    }
}
