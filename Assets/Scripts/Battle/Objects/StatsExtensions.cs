
using System;

public static class StatsExtensions
{
    private static Random rnd = new Random();

    public static Stats GetRandomStat(this Stats stat)
    {
        Array values = Enum.GetValues(typeof(Stats));
        return (Stats)values.GetValue(rnd.Next(values.Length - 1));
    }
}