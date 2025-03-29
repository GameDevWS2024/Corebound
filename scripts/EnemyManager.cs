using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Scripts;

public partial class EnemyManager : Node2D
{
    [Export] public PackedScene? EnemyScene { get; set; }
    [Export] private float _minSpawnInterval = 2;
    [Export] private float _maxSpawnInterval = 10;
    [Export] private int _maxEnemies = 10;
    [Export] private float _baseInterval = 5f;
    [Export] private float _minSpawnDistance = 1000; // Now consistently named
    [Export] private float _maxSpawnDistance = 1500;
    [Export] private float _decayFactor = 1.5f;
    [Export] private int _maxSpawnAttempts = 100; // Add a maximum number of attempts

    private double _timeSinceLastSpawn = 0;
    private bool _isSpawn = true;
    private Random _random = new Random();

    private float CalculateSpawnInterval(int enemyCount)
    {
        float adjustedInterval = _baseInterval * Mathf.Pow(0.9f, enemyCount);
        return Mathf.Clamp(adjustedInterval, _minSpawnInterval, _maxSpawnInterval);
    }

    public override void _Ready()
    {
        // No player reference needed in this version
    }

    public override void _Process(double delta)
    {
        if (!_isSpawn)
        {
            return;
        }

        _timeSinceLastSpawn += delta;

        int enemyCount = GetTree().GetNodesInGroup("Enemies").Count;
        float spawnInterval = CalculateSpawnInterval(enemyCount);

        if (_timeSinceLastSpawn >= spawnInterval)
        {
            SpawnEnemy();
            _timeSinceLastSpawn = 0;
        }
    }

    private void SpawnEnemy()
    {
        if (EnemyScene == null)
        {
            GD.PrintErr("EnemyScene is not set!");
            return;
        }

        if (GetTree().GetNodesInGroup("Enemies").Count >= _maxEnemies)
        {
            return;
        }

        List<Node2D> entities = GetTree().GetNodesInGroup("Entities").OfType<Node2D>().ToList();

        if (entities.Count == 0)
        {
            GD.Print("No entities found in the 'Entities' group.");
            return;
        }

        Node2D targetEntity = entities[_random.Next(entities.Count)];

        // --- LOOP TO ENSURE MINIMUM DISTANCE ---
        Vector2 spawnPosition;
        int attempts = 0;
        do
        {
            spawnPosition = GetRandomPositionAround(targetEntity.GlobalPosition, _minSpawnDistance, _maxSpawnDistance);
            attempts++;
            if (attempts > _maxSpawnAttempts)
            {
                GD.Print("Max spawn attempts reached. Spawning anyway.");
                break; // Exit the loop after too many attempts
            }

        } while (spawnPosition.DistanceTo(Vector2.Zero) < _minSpawnDistance); // Check distance to the *OFFSET*


        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        GetTree().Root.GetNode<Node2D>("Node2D").AddChild(enemy);
        enemy.AddToGroup("Enemies");
		enemy.Position = targetEntity.GlobalPosition + spawnPosition; // VERY IMPORTANT, add offset vector to the global position!

        GD.Print($"Spawned enemy at: {enemy.Position} (relative to entity at {targetEntity.GlobalPosition}, attempts: {attempts})");
    }

    private Vector2 GetRandomPositionAround(Vector2 center, float minDistance, float maxDistance)
    {
        float distance = (float)(_random.NextDouble() * (maxDistance - minDistance) + minDistance);
        float angle = (float)(_random.NextDouble() * Mathf.Tau);
        float offsetX = Mathf.Cos(angle) * distance;
        float offsetY = Mathf.Sin(angle) * distance;
        return new Vector2(offsetX, offsetY); // Return only the offset.
    }
}