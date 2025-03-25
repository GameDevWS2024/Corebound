using System;

using Game.Scripts;

using Godot;
using Godot.NativeInterop;

public partial class Teleport : Node2D
{
    [Export] public int Length { get; set; }
    [Export] public int Width { get; set; }
    [Export] public bool Vertical { get; set; }
    [Export] public required Node2D Destination { get; set; }
    Ally _ally1 = null!;
    Ally _ally2 = null!;

    public override void _Ready()
    {
        _ally1 = GetTree().Root.GetNode<Ally>("Node2D/Ally");
        _ally2 = GetTree().Root.GetNode<Ally>("Node2D/Ally2");
    }


    public override void _Process(double delta)
    {
        if (!Vertical && _ally1.GlobalPosition.Y < GlobalPosition.Y + Width && _ally1.GlobalPosition.Y > GlobalPosition.Y - Width && _ally1.GlobalPosition.X < GlobalPosition.X + Length && _ally1.GlobalPosition.X > GlobalPosition.X - Length)
        {
            _ally1.GlobalPosition = Destination.GlobalPosition;
            _ally1.PathFindingMovement.GoTo(Destination.GlobalPosition);
            GD.Print("Teleportet to " + Destination.GlobalPosition);
        }
        else if (Vertical && _ally1.GlobalPosition.Y < GlobalPosition.Y + Length && _ally1.GlobalPosition.Y > GlobalPosition.Y - Length && _ally1.GlobalPosition.X < GlobalPosition.X + Width && _ally1.GlobalPosition.X > GlobalPosition.X - Width)
        {
            _ally1.GlobalPosition = Destination.GlobalPosition;
            _ally1.PathFindingMovement.GoTo(Destination.GlobalPosition);
            GD.Print("Teleportet to " + Destination.GlobalPosition);
        }
        if (!Vertical && _ally2.GlobalPosition.Y < GlobalPosition.Y + Width && _ally2.GlobalPosition.Y > GlobalPosition.Y - Width && _ally2.GlobalPosition.X < GlobalPosition.X + Length && _ally2.GlobalPosition.X > GlobalPosition.X - Length)
        {
            _ally2.GlobalPosition = Destination.GlobalPosition;
            _ally2.PathFindingMovement.GoTo(Destination.GlobalPosition);
            GD.Print("Teleportet to " + Destination.GlobalPosition);
        }
        else if (Vertical && _ally2.GlobalPosition.Y < GlobalPosition.Y + Length && _ally2.GlobalPosition.Y > GlobalPosition.Y - Length && _ally2.GlobalPosition.X < GlobalPosition.X + Width && _ally2.GlobalPosition.X > GlobalPosition.X - Width)
        {
            _ally2.GlobalPosition = Destination.GlobalPosition;
            _ally2.PathFindingMovement.GoTo(Destination.GlobalPosition);
            GD.Print("Teleportet to " + Destination.GlobalPosition);
        }
    }
}
