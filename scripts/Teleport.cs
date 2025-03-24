using Game.Scripts;

using Godot;
using System;

public partial class Teleport : Node2D
{
	[Export] public int X {get; set; }
	[Export] public int Y { get; set; }
	[Export] public int Length { get; set; }
	[Export] public int Width { get; set; }
	[Export] public bool Vertical { get; set; }
	Ally _ally1 = null!;
	Ally _ally2 = null!;

	public override void _Ready()
	{
		_ally1 = GetTree().Root.GetNode<Ally>("Node2D/Ally");
		_ally2 = GetTree().Root.GetNode<Ally>("Node2D/Ally2");
	}


	public override void _Process(double delta)
	{
		if(!Vertical &&_ally1.GlobalPosition.Y < GlobalPosition.Y + Width && _ally1.GlobalPosition.Y > GlobalPosition.Y - Width && _ally1.GlobalPosition.X < GlobalPosition.X + Length && _ally1.GlobalPosition.X > GlobalPosition.X - Length) {
			_ally1.GlobalPosition = new Vector2(X, GlobalPosition.Y);
		} else if(Vertical && _ally1.GlobalPosition.Y < GlobalPosition.Y + Length && _ally1.GlobalPosition.Y > GlobalPosition.Y - Length && _ally1.GlobalPosition.X < GlobalPosition.X + Width && _ally1.GlobalPosition.X > GlobalPosition.X - Width) {
			_ally1.GlobalPosition = new Vector2(X, Y);
		}
		if(!Vertical &&_ally2.GlobalPosition.Y < GlobalPosition.Y + Width && _ally2.GlobalPosition.Y > GlobalPosition.Y - Width && _ally2.GlobalPosition.X < GlobalPosition.X + Length && _ally2.GlobalPosition.X > GlobalPosition.X - Length) {
			_ally2.GlobalPosition = new Vector2(X, Y);
		} else if(Vertical && _ally2.GlobalPosition.Y < GlobalPosition.Y + Length && _ally2.GlobalPosition.Y > GlobalPosition.Y - Length && _ally2.GlobalPosition.X < GlobalPosition.X + Width && _ally2.GlobalPosition.X > GlobalPosition.X - Width) {
			_ally2.GlobalPosition = new Vector2(X, Y);
		}
	}
}
