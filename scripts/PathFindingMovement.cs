using Godot;

public partial class PathFindingMovement : Node
{
    [Signal] public delegate void ReachedTargetEventHandler();

    [Export] private int _minTargetDistance = 300;
    [Export] private int _targetDistanceVariation = 50; // Not currently used, consider removing or implementing variation
    [Export] private int _speed = 250;
    [Export] bool _debug = false;

    [Export] CharacterBody2D _character = null!;
    [Export] NavigationAgent2D _agent = null!;
    [Export] Sprite2D _sprite = null!;

    public Vector2 TargetPosition { get; set; }
    private object _lastCollider = null; // Speichert den letzten Kollisionspartner
    private bool _recentlyBumped = false; // Verhindert Dauersound
    private bool _reachedTarget;
    private int _currentTargetDistance;
    private AudioStreamPlayer? _bumpSound = null!;

    public override void _Ready()
    {
        _currentTargetDistance = _minTargetDistance;
        this.CallDeferred("ActorSetup"); // Still good to defer setup
        _bumpSound = GetTree().Root.GetNode<AudioStreamPlayer>("Node2D/AudioManager/bump_sound");
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
        _agent.SetTargetPosition(TargetPosition); // Keep this for consistent target setting

        if (_debug)
        {
            float distance = _character.GlobalPosition.DistanceTo(_agent.TargetPosition);
            //GD.Print($"Distance: {distance}, Target Position: {_agent.TargetPosition}");
        }

        float distanceToTarget = _character.GlobalPosition.DistanceTo(_agent.TargetPosition);

        if (distanceToTarget > _currentTargetDistance)
        {
            _reachedTarget = false;
            Vector2 currentLocation = _character.GlobalPosition;
            Vector2 nextLocation = _agent.GetNextPathPosition();
            Vector2 newVel = (nextLocation - currentLocation).Normalized() * _speed;

            if (newVel.X != 0)
            {
                _sprite.FlipH = newVel.X > 0;
            }

            _character.Velocity = newVel;
            KinematicCollision2D collision = _character.MoveAndCollide(newVel * (float)delta);
        if (collision != null)
        {
            // Prüfen, ob der Kollisionspartner neu ist
            if (collision.GetCollider() != _lastCollider)
            {
                _lastCollider = collision.GetCollider(); // Aktualisieren
                _bumpSound.Play();
                _recentlyBumped = true;
            }
        }
        else
        {
            // Keine Kollision mehr, Zustand zurücksetzen
            _lastCollider = null;
            _recentlyBumped = false;
        }

        _character.Velocity = newVel;
        }
        else if (!_reachedTarget) // Only emit and set _reachedTarget once, when the condition is first met
        {
            _currentTargetDistance = _minTargetDistance; // Reset for the next target
            EmitSignal(SignalName.ReachedTarget);
            _reachedTarget = true;
        }
    }
}
