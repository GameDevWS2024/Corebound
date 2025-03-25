using System;

using Game.Scripts;

using Godot;

public partial class WalkOverSound : Node2D
{
    private Vector2 _objectPosition;
    [Export] public float InsideRadius;
    private bool _isInsideRadius = false;
    [Export] public Ally WalkingObject = null!;
    [Export] public Ally WalkingObject2 = null!;

    [Export] public float SoundCooldown = 1.0f;
    [Export] public AudioStreamPlayer Sound = null!;

    private bool _isOnCooldown = false;
    private Timer _cooldownTimer = null!;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _objectPosition = this.GlobalPosition;

        _cooldownTimer = new Timer();
        _cooldownTimer.WaitTime = SoundCooldown;
        _cooldownTimer.OneShot = true;
        _cooldownTimer.Timeout += () => _isOnCooldown = false;
        AddChild(_cooldownTimer);
    }

    public bool IsObjectInsideRadius()
    {
        Vector2 distance1 = WalkingObject.GlobalPosition - _objectPosition;
        float objectDistance = distance1.Length();
        Vector2 distance2 = WalkingObject2.GlobalPosition - _objectPosition;
        float objectDistance1 = distance2.Length();

        return objectDistance < InsideRadius || objectDistance1 < InsideRadius;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        _isInsideRadius = IsObjectInsideRadius();
        if (_isInsideRadius && !_isOnCooldown)
        {
            Sound.Play();
            _isOnCooldown = true;
            _cooldownTimer.Start();
        }
    }

}
