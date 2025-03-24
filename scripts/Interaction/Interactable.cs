using Game.Scripts;
using Game.Scripts.AI;
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
    private CollisionShape2D _caveEntrance1 = null!;
    private CollisionShape2D _caveEntrance2 = null!;
    private float _doorDuration = 5.0f;
    private AiNode _scar = null!;

    public override void _Ready()
    {
        AddToGroup(GroupName);
        _caveEntrance1 = GetTree().Root.GetNode<CollisionShape2D>("Node2D/DoorOpener/StaticBody2D/CaveEntrance1");
        _caveEntrance2 = GetTree().Root.GetNode<CollisionShape2D>("Node2D/DoorOpener/StaticBody2D2/CaveEntrance2");
        _scar = GetTree().Root.GetNode<AiNode>("Node2D/Scar");
        if(GetParent().Equals(_scar)) {
            _scar.GetNode<VisibleForAI>("VisibleForAI").QueueFree();
        }
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
            ally.AnimationIsAlreadyPlaying = true;
            ally._animPlayer.Play("Fill-Bucket");
        }
        //Remove scrub with Jones
        if(this.GetParent<AiNode>().Name.Equals("Big Tree") && caller.Name.ToString().Contains("Ally2")) {
            VisibleForAI scarVisibileForAI = new VisibleForAI();
            scarVisibileForAI.NameForAi = "Scar";
            scarVisibileForAI.DescriptionForAi = "A big scar on the tree which could be the reason for the tree looking dead. It is not reachable because of the scrub";
            _scar.AddChild(scarVisibileForAI, true, InternalMode.Disabled);
            GD.Print("Scar VFAI added");
            EmitSignal(SignalName.Interact);
            EmitSignal(SignalName.InteractFromNode, caller);
        }
        //Water on scar
        if(this.GetParent<AiNode>().Name.Equals("Scar") && caller.Name.ToString().Contains("Ally")) {
            Ally ally = (caller as Ally)!;
            if(!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.BucketWater)) {
                return;
            }
            ally.AnimationIsAlreadyPlaying = true;
            ally._animPlayer.Play("Empty-Bucket");
            ally.SsInventory.HardSwapItems(Game.Scripts.Items.Material.BucketWater, Game.Scripts.Items.Material.BucketEmpty);
            //Tree is now cured (for story progression)
            GD.Print("Tree cured");
            TreeCured = true;
        }
        //Cave entrance
        if(this.GetParent<AiNode>().Name.Equals("CaveEntranceTerminal") && caller.Name.ToString().Contains("Ally")) {
            Ally ally = (caller as Ally)!;
            if(!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.Chipcard)) {
                return;
            }
        }
        //Door opener
        if(this.GetParent<AiNode>().Name.Equals("DoorOpener") && caller.Name.ToString().Contains("Ally")) {
            Ally ally = (caller as Ally)!;
            if(!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.Chipcard)) {
                return;
            }
    	    TemporarilyDisable();
        }

        if(this.GetParent<AiNode>().Name.Equals("EmptyBucket") && caller.Name.ToString().Contains("Ally2")) {
            GD.Print("Jones already has his machine gun, so he can't carry the bucket");
            //Nachricht an Ally 2, dass ers nich aufheben kann
            return;
        }
        
        EmitSignal(SignalName.Interact);
        EmitSignal(SignalName.InteractFromNode, caller);
    }

    public async void TemporarilyDisable() {
        _caveEntrance1.SetDeferred("disabled", true);
        _caveEntrance2.SetDeferred("disabled", true);
        GD.Print("Door is open!");

        await ToSignal(GetTree().CreateTimer(_doorDuration), "timeout");

        _caveEntrance1.SetDeferred("disabled", false);
        _caveEntrance2.SetDeferred("disabled", false);
        GD.Print("Door is closed");
    }
}
