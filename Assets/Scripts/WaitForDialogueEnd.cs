using UnityEngine;

public class WaitForDialogueEnd : CustomYieldInstruction
{
    private DialogueManager _dialogue;
    public override bool keepWaiting
    {
        get
        {
            return !_dialogue.CheckDialogEnd();
        }
    }

    public WaitForDialogueEnd(DialogueManager dialogue)
    {
        _dialogue = dialogue;
    }
}