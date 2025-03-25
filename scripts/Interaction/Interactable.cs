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
    private AnimationPlayer _animTree = null!;
    private AnimationPlayer _animEntrance = null!;
    private AnimationPlayer _animDoorOpener = null!;

    public override void _Ready()
    {
        AddToGroup(GroupName);
        _caveEntrance1 = GetTree().Root.GetNode<CollisionShape2D>("Node2D/DoorOpener/StaticBody2D/CaveEntrance1");
        _caveEntrance2 = GetTree().Root.GetNode<CollisionShape2D>("Node2D/DoorOpener/StaticBody2D2/CaveEntrance2");
        _animTree = GetTree().Root.GetNode<AnimationPlayer>("Node2D/Node2D/AnimationPlayer");
        _animEntrance = GetTree().Root.GetNode<AnimationPlayer>("Node2D/CaveEntranceTerminal/AnimationPlayer");
        _animDoorOpener = GetTree().Root.GetNode<AnimationPlayer>("Node2D/DoorOpener/AnimationPlayer");
        _scar = GetTree().Root.GetNode<AiNode>("Node2D/Scar");
        if (GetParent().Equals(_scar))
        {
            _scar.GetNode<VisibleForAI>("VisibleForAI").QueueFree();
        }
    }

    public void Trigger(Node caller)
    {
        //Ally response
        if (!string.IsNullOrEmpty(SystemMessageForAlly) && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            ally.Chat.SendSystemMessage(SystemMessageForAlly, new Ally());
        }

        //Fill bucket with water
        if (GetParent<AiNode>().Name.Equals("Well") && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            ally.SsInventory.HardSwapItems(Game.Scripts.Items.Material.BucketEmpty, Game.Scripts.Items.Material.BucketWater);
            ally.AnimationIsAlreadyPlaying = true;
            ally._animPlayer.Play("Fill-Bucket");
        }
        //Remove scrub with Jones
        if (GetParent<AiNode>().Name.Equals("Big Tree") && caller.Name.ToString().Equals("Ally2"))
        {
            _animTree.Play("TreeAnimation");
            GD.Print("Scrub removed!");
            VisibleForAI scarVisibileForAI = new()
            {
                NameForAi = "Scar",
                DescriptionForAi = "A big scar on the tree which could be the reason for the tree looking dead. It is not reachable because of the scrub"
            };
            _scar.AddChild(scarVisibileForAI, false);
            GD.Print("Scar VFAI added");
            EmitSignal(SignalName.Interact);
            EmitSignal(SignalName.InteractFromNode, caller);
        }

        else if (GetParent<AiNode>().Name.Equals("Big Tree"))
        {
            Ally? ally = caller as Ally;
            ally!.Chat.SendSystemMessage("Only Jones may remove the scrub here. Only he's skilled enough to do that.", new Ally());
            return;
        }

        //Water on scar
        if (GetParent<AiNode>().Name.Equals("Scar") && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            if (!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.BucketWater))
            {
                return;
            }
            ally.AnimationIsAlreadyPlaying = true;
            ally._animPlayer.Play("Empty-Bucket");
            _animTree.Play("TreeOpens");
            ally.SsInventory.HardSwapItems(Game.Scripts.Items.Material.BucketWater, Game.Scripts.Items.Material.BucketEmpty);
            //Tree is now cured (for story progression)
            GD.Print("Tree cured");
            TreeCured = true;

            GD.Print("Teleport spawned");
            PackedScene scene = (PackedScene)ResourceLoader.Load("res://scenes/prefabs/teleport.tscn");
            Teleport instance = scene.Instantiate<Teleport>();
            instance.Position += new Vector2(0, 100);


        }
        //Cave entrance
        if (GetParent<AiNode>().Name.Equals("CaveEntranceTerminal") && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            if (!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.Chipcard))
            {
                return;
            }
            _animEntrance.Play("Barier");
        }
        //Door opener
        if (GetParent<AiNode>().Name.Equals("DoorOpener") && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            if (!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.Chipcard))
            {
                return;
            }
            TemporarilyDisable();
        }

        if (GetParent<AiNode>().Name.Equals("EmptyBucket") && caller.Name.ToString().Equals("Ally2"))
        {
            GD.Print("Jones already has his machine gun, so he can't carry the bucket");
            Ally? jones = caller as Ally;
            Chat jonesChat = jones!.Chat;
            jonesChat.SendSystemMessage("You're already carrying a machine gun, so you can't carry the bucket. Tell the commander another ally might be beneficial for this task.", new Ally());
            return;
        }

        if (GetParent<AiNode>().Name.Equals("EmptyBucket") && caller.Name.ToString().Equals("Ally"))
        {
            Ally? james = caller as Ally;
            Chat jonesChat = james!.Chat;
            jonesChat.SendSystemMessage("You've picked up the empty bucket. Maybe fill it with something.", new Ally());

        }

        EmitSignal(SignalName.Interact);
        EmitSignal(SignalName.InteractFromNode, caller);
    }

    public async void TemporarilyDisable()
    {
        _animDoorOpener.Play("Barier");
        _caveEntrance1.SetDeferred("disabled", true);
        _caveEntrance2.SetDeferred("disabled", true);
        GD.Print("Door is open!");

        await ToSignal(GetTree().CreateTimer(_doorDuration), "timeout");

        _animDoorOpener.PlayBackwards("Barier");
        _caveEntrance1.SetDeferred("disabled", false);
        _caveEntrance2.SetDeferred("disabled", false);
        GD.Print("Door is closed");
    }
}
