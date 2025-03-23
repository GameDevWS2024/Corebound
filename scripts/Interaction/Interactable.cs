using Game.Scripts;

using Godot;

[GlobalClass]
public partial class Interactable : Node2D
{
    public const string GroupName = "Interactable";
    [Signal] public delegate void InteractFromNodeEventHandler(Node caller);
    [Signal] public delegate void InteractEventHandler();

    public string? SystemMessageForAlly;

    public override void _Ready()
    {
        AddToGroup(GroupName);
    }

    public void Trigger(Node caller)
    {
        if (!string.IsNullOrEmpty(SystemMessageForAlly) && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            ally.Chat.SendSystemMessage(SystemMessageForAlly, ally);
        }
        EmitSignal(SignalName.Interact);
        EmitSignal(SignalName.InteractFromNode, caller);
    }
}
