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

    private void Start()
    {
        _dialogueQueue = new Queue<string>();
        _wfs = new WaitForSeconds(_textSpeed);
        _wff = new WaitForEndOfFrame();
        _wfk = new WaitForKeyDown(_jumpKey);
    }

    public void AddDialogue(string dialogue) => _dialogueQueue.Enqueue(dialogue);
    public void StartDialogues() => StartCoroutine(PlayDialogues());

    private IEnumerator PlayDialogues()
    {
        int size = _dialogueQueue.Count;
        int i = 0;

        while (i < size)
        {
            string dialogue = _dialogueQueue.Dequeue();

            _dialogueBox.text = "";

            foreach (char c in dialogue)
            {
                _dialogueBox.text += c;

                if (Input.GetButtonDown(_jumpKey))
                {
                    _dialogueBox.text = dialogue;
                    break;
                }

                yield return _wfs;
            }

            yield return _wff;
            yield return _wfk;
            yield return _wff;

            i++;
        }
    }
}
