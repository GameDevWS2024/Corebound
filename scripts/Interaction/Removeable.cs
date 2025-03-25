using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Game.Scripts;
using Game.Scripts.AI;

using Godot;

[GlobalClass]
public partial class Removeable : Node
{
    [Export] public bool ListenToInteract { get; set; } = true;
    public override void _Ready()
    {
        if (!ListenToInteract)
        {
            return;
        }

        Interactable interactable = GetParent().GetNode<Interactable>("Interactable");
        if (interactable != null)
        {
            interactable.Interact += Remove;
        }
    }
    public void Remove()
    {
        GD.Print("Removed something");
        GetParent().CallDeferred(Node.MethodName.QueueFree);
    }
}
