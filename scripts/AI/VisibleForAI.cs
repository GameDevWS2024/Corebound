using Godot;

namespace Game.Scripts.AI;

[GlobalClass]
public partial class VisibleForAI : Node2D
{
    public const string GroupName = "AiVisible";
    [Export] public string NameForAi = "";
    [Export] public string DescriptionForAi = "";

    public override void _Ready()
    {
        AddToGroup(GroupName);
    }

    public override string ToString()
    {
        if (IsInstanceValid(this))
        {
            return $"{NameForAi} at ({GlobalPosition.X:F0}, {GlobalPosition.Y:F0}): [Description] {DescriptionForAi}";
        }
        return "";
    }
}
