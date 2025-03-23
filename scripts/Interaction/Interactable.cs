using Game.Scripts;
using Game.Scripts.Items;

using Godot;

[GlobalClass]
public partial class Interactable : Node2D
{
    public const string GroupName = "Interactable";
    public static bool TreeCured = false;
    [Signal] public delegate void InteractFromNodeEventHandler(Node caller);
    [Signal] public delegate void InteractEventHandler();

    public string? SystemMessageForAlly;

    public override void _Ready()
    {
        AddToGroup(GroupName);
    }

    public void Trigger(Node caller)
    {
        //Ally response
        if (!string.IsNullOrEmpty(SystemMessageForAlly) && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            ally.Chat.SendSystemMessage(SystemMessageForAlly, ally);
        }

        //Fill bucket with water
        if(this.GetParent<AiNode>().Name.Equals("Well") && caller.Name.ToString().Contains("Ally")) {
            Ally ally = (caller as Ally)!;
            ally.SsInventory.HardSwapItems(Game.Scripts.Items.Material.BucketEmpty, Game.Scripts.Items.Material.BucketWater);
            ally._animPlayer.Play("Fill-Bucket");
        }
        //Remove scrub with Jones
        if(this.GetParent<AiNode>().Name.Equals("Big Tree") && caller.Name.ToString().Contains("Ally2")) {
            EmitSignal(SignalName.Interact);
            EmitSignal(SignalName.InteractFromNode, caller);
        }
        //Water on scar
        if(this.GetParent<AiNode>().Name.Equals("Scar") && caller.Name.ToString().Contains("Ally")) {
            Ally ally = (caller as Ally)!;
            if(!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.BucketWater)) {
                return;
            }
            ally._animPlayer.Play("Empty-Bucket");
            //Tree is now cured (for story progression)
            TreeCured = true;
        }

        if(this.GetParent<AiNode>().Name.Equals("EmptyBucket") && caller.Name.ToString().Contains("Ally2")) {
            GD.Print("Jones already has his machine gun, so he can't carry the bucket");
            //Nachricht an Ally 2, dass ers nich aufheben kann
            return;
        }
        
        EmitSignal(SignalName.Interact);
        EmitSignal(SignalName.InteractFromNode, caller);
    }
}
