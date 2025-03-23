using System;

namespace Game.Scripts;

public class Chance
{
    private readonly Random _rand = new Random();

    public bool IsHappening(int motivation)
    {
        return _rand.Next(10) + 1 <= motivation; ;
    }
}
