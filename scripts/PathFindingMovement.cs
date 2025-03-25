using System;

using Game.Scenes.Levels;
using Game.Scripts;

using Godot;

public partial class PathFindingMovement : Node
{
    [Signal] public delegate void ReachedTargetEventHandler();

    [Export] private int _minTargetDistance = 40;
    [Export] private int _targetDistanceVariation = 50; // Not currently used, consider removing or implementing variation
    [Export] private int _speed = 250;
    [Export] int _minimumSpeed = 50;
    private int _origMinimumSpeed;

    [Export] bool _debug = false;

    [Export] CharacterBody2D _character = null!;
    [Export] NavigationAgent2D _agent = null!;
    [Export] Sprite2D _sprite = null!;

    public Vector2 TargetPosition { get; set; }
    private object? _lastCollider = null; // Speichert den letzten Kollisionspartner
    private bool _recentlyBumped = false; // Verhindert Dauersound
    private bool _reachedTarget;
    private int _currentTargetDistance;
    private AudioStreamPlayer? _bumpSound = null!;
    private ButtonControl _buttonControl = null!;

    public enum WalkingState
    {
        Left,
        Right,
        IdleLeft,
        IdleRight
    }

    public WalkingState CurrentDirection { get; private set; } = WalkingState.IdleLeft;


    public override void _Ready()
    {
        _origMinimumSpeed = _minimumSpeed;
        _currentTargetDistance = _minTargetDistance;
        this.CallDeferred("ActorSetup"); // Still good to defer setup
        _bumpSound = GetTree().Root.GetNode<AudioStreamPlayer>("Node2D/AudioManager/bump_sound");
        _buttonControl = GetTree().Root.GetNode<ButtonControl>("Node2D/UI");
    }

    public async void ActorSetup()
    {
        await ToSignal(GetTree(), "physics_frame");
    }

    public void GoTo(Vector2 loc)
    {
        _agent.SetTargetPosition(loc);
        TargetPosition = loc;
    }

public override void _PhysicsProcess(double delta)
{
    _speed = 250;
    _agent.SetTargetPosition(TargetPosition);

    float distanceToTarget = _character.GlobalPosition.DistanceTo(_agent.TargetPosition);

    if (distanceToTarget > _currentTargetDistance)
    {
        _reachedTarget = false;

        Vector2 currentLocation = _character.GlobalPosition;
        Vector2 nextLocation = _agent.GetNextPathPosition();

        // Motivation und Speed-Berechnung
        Motivation motivation = GetParent().GetNode<Motivation>("Motivation");
        double motivationFactor = (double)motivation.Amount / 10;
        int modifiedSpeed = (int)(_minimumSpeed + (_speed - _minimumSpeed) * motivationFactor);

        // Bewegung berechnen
        Vector2 newVel = (nextLocation - currentLocation).Normalized() * modifiedSpeed;

        // Richtung für die Animation setzen
        CurrentDirection = nextLocation.X < currentLocation.X ? WalkingState.Left : WalkingState.Right;

        if (newVel.X != 0 && distanceToTarget > 50)
        {
            _sprite.FlipH = newVel.X > 0;
        }

        // **Langsamer werden bei Zielnähe (weiches Bremsen)**
        if (distanceToTarget < 50)
        {
            float slowdownFactor = Mathf.Clamp(distanceToTarget / 50, 0.1f, 1f);
            newVel *= slowdownFactor;
        }

        _character.Velocity = newVel;

        // Kollisionsabfrage bleibt bestehen
        KinematicCollision2D collision = _character.MoveAndCollide(newVel * (float)delta);
        if (collision != null && collision.GetCollider() != _lastCollider)
        {
            if ((_character.Name == "Ally" && _buttonControl.CurrentCamera == 1) ||
                (_character.Name == "Ally2" && _buttonControl.CurrentCamera == 2))
            {
                _lastCollider = collision.GetCollider();
                _bumpSound!.Play();
                _recentlyBumped = true;
            }
        }
        else
        {
            _lastCollider = null;
            _recentlyBumped = false;
        }
    }
    else if (!_reachedTarget)
    {
        // **Weiches Stoppen statt Teleport**
        

        if (distanceToTarget < 45f)  // Bei < 3 Pixel Restdistanz stoppen
        {
            _character.Velocity = Vector2.Zero;

            // Idle-Animation aktivieren
            CurrentDirection = (CurrentDirection == WalkingState.Left) ? WalkingState.IdleLeft : WalkingState.IdleRight;

            _currentTargetDistance = _minTargetDistance;
            EmitSignal(SignalName.ReachedTarget);
            _reachedTarget = true;
        }
    }
}
}
