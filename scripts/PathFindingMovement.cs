using Godot;
using Game.Scripts;

public partial class PathFindingMovement : Node
{
    [Signal] public delegate void ReachedTargetEventHandler();
    [Signal] public delegate void BumpedIntoObjectEventHandler(Node2D collider);

    [Export] private float _minTargetDistance = 40;
    [Export] private float _stopDistance = 50;
    [Export] private float _baseSpeed = 250;
    [Export] private float _minimumSpeed = 150;
    [Export] private bool _avoidanceEnabled = true;
    //[Export] private bool _debug; // Entferne, wenn nicht verwendet

    [Export] private CharacterBody2D _character = null!;
    [Export] private NavigationAgent2D _agent = null!;
    [Export] private Sprite2D _sprite = null!;
    [Export] private AudioStreamPlayer _bumpSound = null!;

    public Vector2 TargetPosition { get; set; }
    public WalkingState CurrentDirection { get; private set; } = WalkingState.IdleLeft;

    private Node2D? _lastCollider;
    private bool _reachedTarget;

    public enum WalkingState
    {
        Left,
        Right,
        IdleLeft,
        IdleRight
    }

    public override void _Ready()
    {
        // Configure navigation agent
        _agent.AvoidanceEnabled = _avoidanceEnabled;
        _agent.PathDesiredDistance = _minTargetDistance;
        _agent.TargetDesiredDistance = _minTargetDistance;
        _agent.PathMaxDistance = 1000f;
        _agent.NavigationLayers = 1; // Stelle sicher, dass dies mit deinen NavigationRegions übereinstimmt!
        _agent.MaxSpeed = 200f;

        _agent.VelocityComputed += OnVelocityComputed;
    }

    public async void GoTo(Vector2 targetLocation)
    {
        Vector2 closestPoint = NavigationServer2D.MapGetClosestPoint(_agent.GetNavigationMap(), targetLocation);
        _agent.TargetPosition = closestPoint;
        
        _reachedTarget = false;
        TargetPosition = targetLocation;
        
        if (!_agent.IsTargetReachable())
        {
            GD.PrintErr("Target not reachable!");
            return;
        }
    }

    private void OnVelocityComputed(Vector2 safeVelocity)
    {
        _character.Velocity = safeVelocity*2;
        _character.MoveAndSlide();
        UpdateDirectionAndSprite(_character.Velocity, _character.GlobalPosition, _agent.GetNextPathPosition(), _character.GlobalPosition.DistanceTo(_agent.TargetPosition));

        if (_character.GlobalPosition.DistanceTo(_agent.TargetPosition) <= _minTargetDistance)
        {
            StopMovement();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Leer, da die gesamte Logik in OnVelocityComputed ist.
    }

    private float CalculateModifiedSpeed()
    {
        // Implementiere die dynamische Geschwindigkeitsanpassung, falls gewünscht.
        return 200f;
    }

    private void StopMovement()
    {
        CurrentDirection = CurrentDirection == WalkingState.Left ? WalkingState.IdleLeft : WalkingState.IdleRight;
        _character.Velocity = Vector2.Zero;
        EmitSignal(SignalName.ReachedTarget);
        _reachedTarget = true;
    }

    private void UpdateDirectionAndSprite(Vector2 velocity, Vector2 currentLocation, Vector2 nextLocation, float distanceToTarget)
    {
        CurrentDirection = nextLocation.X < currentLocation.X ? WalkingState.Left : WalkingState.Right;

        if (velocity.X != 0 && distanceToTarget > _stopDistance)
        {
            _sprite.FlipH = velocity.X > 0;
        }
    }

    private void HandleCollision(KinematicCollision2D collision)
    {
        Node2D collidedObject = (Node2D)collision.GetCollider();
        if (collidedObject == _lastCollider)
        {
            return;
        }

        _lastCollider = collidedObject;
        EmitSignal(SignalName.BumpedIntoObject, _lastCollider);
        _bumpSound.Play();
    }
}