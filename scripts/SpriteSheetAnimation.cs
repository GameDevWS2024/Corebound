using System;

using Godot;

public partial class SpriteSheetAnimation : Sprite2D
{
    private Sprite2D _animTree = null!;
    private int _currentFrame = 0;
    private float _frameTimer = 0f;
    public static bool IsAnimating = false;
    private float _frameDuration = 0.1f;
    private int _frameCount = 17;

    public override void _Ready()
    {
        _animTree = GetTree().Root.GetNode<Sprite2D>("Node2D/Node2D/TreeScrub");
    }

    public override void _Process(double delta)
    {
        if (IsAnimating)
        {
            _animTree.Frame += 1;
            _frameTimer += (float)delta;
            if (_frameTimer >= _frameDuration)
            {
                if (_animTree.Frame == 9)
                {
                    IsAnimating = false;
                }

                if (_animTree.Frame == 16)
                {
                    IsAnimating = false;
                }
            }

        }
    }
}
