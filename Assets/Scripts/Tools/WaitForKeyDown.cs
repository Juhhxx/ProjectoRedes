using UnityEngine;

public class WaitForKeyDown : CustomYieldInstruction
{
    private string _key;
    public override bool keepWaiting
    {
        get
        {
            return !Input.GetButtonDown(_key);
        }
    }

    public WaitForKeyDown(string key)
    {
        _key = key;
    }
}