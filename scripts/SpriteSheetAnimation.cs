using System;

using Godot;

public partial class SpriteSheetAnimation : Sprite2D
{
    private Sprite2D _animTree = null!;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    public static bool isAnimating = false;
    private float frameDuration = 0.1f;
    private int frameCount = 17;

    public override void _Ready()
    {
        _animTree = GetTree().Root.GetNode<Sprite2D>("Node2D/Node2D/TreeScrub");
    }

    public override void _Process(double delta)
    {
        if (isAnimating)
        {
            _animTree.Frame += 1;
            frameTimer += (float)delta;
            if (frameTimer >= frameDuration)
            {
                if (_animTree.Frame == 9)
                {
                    isAnimating = false;
                }

                if (_animTree.Frame == 16)
                {
                    isAnimating = false;
                }
            }

        }
    }
}
