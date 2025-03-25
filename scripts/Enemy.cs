using System.Collections.Generic;
using System.Linq;

using Game.Scripts;

using Godot;
using Godot.Collections;

public partial class Enemy : CharacterBody2D
{
    [Export] PathFindingMovement _pathFindingMovement = null!;
    [Export] private int _damage = 5;
    [Signal] public delegate void DeathEventHandler();
    [Signal] public delegate void HealthChangedEventHandler(int newHealth);


    private bool _attack = true;
    private Array<Node>? _entityGroup;
    private CharacterBody2D? _player;
    private Node2D? _core;
    public Health Health = null!;

    public override void _Ready()
    {
        Health = GetNode<Health>("Health");
        _core = GetTree().CurrentScene.GetNode<Node2D>("%Core");
        
    }

    private float _attackCooldown = 0.5f; // Time between attacks in seconds
    private float _timeSinceLastAttack; // Time accumulator
    private const float AttackRange = 200.0f; // Distance at which enemy can attack

    public override void _PhysicsProcess(double delta)
    {
        if (Health.Amount <= 0)
        {
            QueueFree();
            List<Ally> allies = GetTree().GetNodesInGroup("Entities").OfType<Ally>().ToList();
            foreach (Health health in from ally in allies where ally.Name.Equals("Ally2") select ally.GetNode<Health>("Health"))
            {
                health.Heal(10);
            }
        }

        _timeSinceLastAttack += (float)delta;

        _entityGroup = GetTree().GetNodesInGroup("Entities");
        if (_entityGroup.ToList().Count == 0)
        {
            GetTree().CurrentScene.QueueFree();
            Node gameOverScene = GD.Load<PackedScene>("res://scenes/prefabs/GameOver.tscn").Instantiate();
            GetTree().Root.AddChild(gameOverScene);
        }

        List<(Node2D entity, float distance)> nearestEntities = _entityGroup.OfType<Node2D>().Select(entity => (entity, entity.GlobalPosition.DistanceTo(GlobalPosition))).ToList();

        Node2D nearestEntity = nearestEntities.OrderBy(tup => tup.distance).FirstOrDefault().entity;

        if (nearestEntity == null)
        {
            return;
        }

        Vector2 pos = nearestEntity.GlobalPosition;
        float distanceToTarget = pos.DistanceTo(this.GlobalPosition);

        _pathFindingMovement.GoTo(pos);
        if (!(distanceToTarget < AttackRange) || !(_timeSinceLastAttack >= _attackCooldown) || !_attack)
        {
            return;
        }

        Health allieHealth = nearestEntity.GetNode<Health>("Health");
        allieHealth.Damage(_damage);
        _timeSinceLastAttack = 0;
    }


}
