using System;

using Game.Scenes.Levels;
using Game.Scripts;

using Godot;

public partial class PathFindingMovement : Node
{
    [Signal] public delegate void ReachedTargetEventHandler();

    [Export] private int _minTargetDistance = 40;
    [Export] private int _targetDistanceVariation = 50;
    [Export] private int _speed = 250;
    [Export] int _minimumSpeed = 150;
    private int _origMinimumSpeed;

    [Export] bool _debug = false;

    [Export] CharacterBody2D _character = null!;
    [Export] NavigationAgent2D _agent = null!;
    [Export] Sprite2D _sprite = null!;

    public Vector2 TargetPosition { get; set; }
    private object _lastCollider = null;
    private bool _recentlyBumped = false;
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
        this.CallDeferred("ActorSetup");
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

        if (_debug)
        {
            float distance = _character.GlobalPosition.DistanceTo(_agent.TargetPosition);
            //GD.Print($"Distance: {distance}, Target Position: {_agent.TargetPosition}");
        }

        float distanceToTarget = _character.GlobalPosition.DistanceTo(_agent.TargetPosition);

        if (distanceToTarget > _currentTargetDistance)
        {
            _reachedTarget = false;
            Vector2 currentLocation = _character.GlobalPosition, nextLocation = _agent.GetNextPathPosition();

           /* Motivation motivation = GetParent().GetNode<Motivation>("Motivation");
            double motivationFactor = (double)motivation.Amount / 10;*/
            int modifiedSpeed = (int)(_minimumSpeed + (_speed - _minimumSpeed) * 2);
            Ally ally = GetParent().GetParent().GetChild<Ally>(0);
            Chat chat = ally.FindChild("Speech").GetParent() as Chat ?? throw new InvalidOperationException();
            _minimumSpeed = (chat!.GeminiService.IsBusy() || ally!.GetResponseQueue().Count > 0 || !ally!.IsTextBoxReady) ? 0 : _origMinimumSpeed; // dont move while responding or if more than one response is being processed.
            Vector2 newVel = (nextLocation - currentLocation).Normalized() * modifiedSpeed;
            if (nextLocation.X < currentLocation.X)
            {
                CurrentDirection = WalkingState.Left;
            }
            else
            {
                CurrentDirection = WalkingState.Right;
            }

            if (newVel.X != 0 && distanceToTarget > 50)
            {
                _sprite.FlipH = newVel.X > 0;
            }

            _character.Velocity = newVel;
            KinematicCollision2D collision = _character.MoveAndCollide(newVel * (float)delta);
            if (collision != null)
            {
                if (collision.GetCollider() != _lastCollider)
                {
                    if (_character.Name == "Ally" && _buttonControl.CurrentCamera == 1 || _character.Name == "Ally2" && _buttonControl.CurrentCamera == 2)
                    {
                        _lastCollider = collision.GetCollider();
                        _bumpSound!.Play();
                        _recentlyBumped = true;
                    }
                }
            }
            else
            {
                _lastCollider = null;
                _recentlyBumped = false;
            }

            _character.Velocity = newVel;
        }
        else if (!_reachedTarget) // only emit and set _reachedTarget once, when the condition is first met
        {
            if (CurrentDirection == PathFindingMovement.WalkingState.Left)
            {
                CurrentDirection = WalkingState.IdleLeft;
            }
            else
            {
                CurrentDirection = WalkingState.IdleRight;
            }

            _currentTargetDistance = _minTargetDistance; // reset for the next target
            EmitSignal(SignalName.ReachedTarget);
            _reachedTarget = true;
        }
    }
}