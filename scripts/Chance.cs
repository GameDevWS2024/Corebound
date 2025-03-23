using System;

namespace Game.scripts;

public class Chance
{
    private readonly Random _rand = new Random();

    public bool IsHappening(int motivation)
    {
        return _rand.Next(10)+1 <= motivation; ;
    }
}
