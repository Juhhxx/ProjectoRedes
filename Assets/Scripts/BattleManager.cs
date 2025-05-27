using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField][Range(1, 2)] private int _actionsListSize;

    [SerializeField] private UpdateUI _ui;
    [SerializeField] private DialogueManager _dialogueManager;

    private List<Attack> _playerActions;

    public void RegisterAction(Attack attack) => _playerActions.Add(attack);
    public IEnumerator BattleStart()
    {
        yield return new WaitForPlayerActions(() => _playerActions.Count == _actionsListSize);
    }

}
