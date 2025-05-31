using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dialogueBox;
    [SerializeField] private float _textSpeed;
    [SerializeField][InputAxis] private string _jumpKey;

    private Queue<string> _dialogueQueue;
    private YieldInstruction _wfs;
    private YieldInstruction _wff;
    private WaitForKeyDown _wfk;
    private bool _dialoguePlaying;

    public void SetUpDialogueManager()
    {
        _dialogueQueue = new Queue<string>();
        _wfs = new WaitForSeconds(_textSpeed);
        _wff = new WaitForEndOfFrame();
        _wfk = new WaitForKeyDown(_jumpKey);
    }

    public void AddDialogue(string dialogue) => _dialogueQueue.Enqueue(dialogue);
    public void StartDialogues()
    {
        StopAllCoroutines();
        StartCoroutine(PlayDialogues());
    }
    public void StartDialogues(string dialogue)
    {
        StopAllCoroutines();
        AddDialogue(dialogue);
        StartCoroutine(PlayDialogues());
    }
    public bool CheckDialogEnd()
    {
        return !_dialoguePlaying;
    }

    private IEnumerator PlayDialogues()
    {
        _dialoguePlaying = true;

        int size = _dialogueQueue.Count;
        int i = 0;

        while (i < size)
        {
            string dialogue = _dialogueQueue.Dequeue();

            _dialogueBox.text = "";

            yield return _wff;

            foreach (char c in dialogue)
            {
                if (Input.GetButtonDown("Submit"))
                {
                    _dialogueBox.text = dialogue;
                    break;
                }

                _dialogueBox.text += c;

                yield return _wfs;
            }

            yield return _wff;
            if (_dialogueQueue.Count > 0) yield return _wfk;
            yield return _wff;

            i++;
        }

        _dialoguePlaying = false;
    }
}
