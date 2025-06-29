using UnityEngine;
using System;

public class WaitForPlayerActions : CustomYieldInstruction
{
    private Func<bool> _checkPlayerActions; 
    public override bool keepWaiting
    {
        get
        {
            return !_checkPlayerActions();
        }
    }

    public WaitForPlayerActions(Func<bool> checkPlayerActions)
    {
        _checkPlayerActions = checkPlayerActions;
    }
}