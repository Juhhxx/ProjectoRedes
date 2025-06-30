using System.Collections.Generic;

public class StatModifier
{
    public Stats Stat { get; private set; }
    public int Amount { get; private set; }
    public int TurnsActive { get; private set; }
    public int TurnsPassed { get; private set; }

    public void TurnPass() => TurnsPassed++;
    public bool CheckDone() => TurnsActive == TurnsPassed;

    public StatModifier(Stats stat, int amount, int turnsActive)
    {
        Stat = stat;
        Amount = amount;
        TurnsActive = turnsActive;
    }
    
}