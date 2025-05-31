using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;
using System.Collections;

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
    [field: SerializeField] public float HP { get; private set; }
    [field: SerializeField] public int Attack { get; private set; }
    [field: SerializeField] public int Defense { get; private set; }
    [field: SerializeField] public int Speed { get; private set; }

    public event Action OnDamageTaken;

    private Player _owner;
    public Player Owner => _owner;
    private float _currentHP;
    public float CurrentHP => _currentHP;
    private Attack[] _currentAttackSet = new Attack[4];
    public Attack[] CurrentAttackSet => _currentAttackSet;

    private YieldInstruction _wff = new WaitForEndOfFrame();

    public void SetOwner(Player owner) => _owner = owner;
    public void AddAttack(int id, Attack attack)
    {
        Attack newAttack = attack.CreateAttack();

        _currentAttackSet[id] = newAttack;
        newAttack.SetAttacker(this);
    }
    public void SetHP(float hp) => _currentHP = hp;
    public float TakeDamage(Attack attack, DialogueManager dialogue)
    {
        Debug.Log($"{attack.Attacker.Name} used {attack.Name} on {Name}");
        int rnd = UnityEngine.Random.Range(1, 100);

        if (rnd > attack.Accuracy)
        {
            dialogue.AddDialogue($"{attack.Attacker.Name} missed...");

            return 0f;
        }

        float damage = CalculateDamage(attack);

        Debug.Log($"DAMAGE : {damage}");

        attack.Used();

        dialogue.AddDialogue(attack.GetAttackMessage());
        if (attack.GetEffectiveness(Type) > 1.0f) dialogue.AddDialogue("It's super effective...");
        else if (attack.GetEffectiveness(Type) < 1.0f) dialogue.AddDialogue("It's not very effective...");

        // OnDamageTaken?.Invoke();

        return damage;
    }
    private float CalculateDamage(Attack attack)
    {
        float rnd = UnityEngine.Random.Range(217, 255);
        rnd /= 255;

        return (((((2 * attack.Attacker.Owner.Level * attack.CriticalChance()) / 5)
                    * attack.Power * (attack.Attacker.Attack / Defense)) / 50) + 2)
                    * attack.GetSTAB() * attack.GetEffectiveness(Type) * rnd;
    }
    public IEnumerator ApplyDamage(float damage)
    {
        float objective = _currentHP - damage;

        if (objective < 0f) objective = 0f;

        while (_currentHP > objective)
        {

            _currentHP -= 0.1f;

            OnDamageTaken?.Invoke();

            Debug.Log($"APPLYING DAMAGE");

            yield return new WaitForSeconds(0.005f);
        }
    }

    public Creature CreateCreature(Player owner)
    {
        Creature newC = Instantiate(this);

        newC.SetHP(HP);
        newC.SetOwner(owner);

        // Temporary
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"{Attacks[i].Name} n{i} to {Name} ({name})");
            Debug.Log(i);
            newC.AddAttack(i, Attacks[i]);
        }

        return newC;
    }
}
