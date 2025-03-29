using System.Threading.Tasks;

using Game.Scripts;
using Game.Scripts.AI;
using Game.Scripts.Items;

using Godot;

[GlobalClass]
public partial class Interactable : Node2D
{
    public const string GroupName = "Interactable";
    public static bool TreeCured = false;

    public static bool ScrubRemoved = false;
    [Signal] public delegate void InteractFromNodeEventHandler(Node caller);
    [Signal] public delegate void InteractEventHandler();

    public string? SystemMessageForAlly;
    private CollisionShape2D _caveEntrance1 = null!;
    private CollisionShape2D _scrub = null!;
    private CollisionShape2D _caveEntrance2 = null!;
    private float _doorDuration = 5.0f;
    private AiNode _scar = null!;
    private AnimationPlayer _animTree = null!;
    private AnimationPlayer _animEntrance = null!;
    private AnimationPlayer _animDoorOpener = null!;
    private Sprite2D _keySprite = null!;

    public override void _Ready()
    {
        AddToGroup(GroupName);
        _caveEntrance1 = GetTree().Root.GetNode<CollisionShape2D>("Node2D/DoorOpener/StaticBody2D/CaveEntrance1");
        _caveEntrance2 = GetTree().Root.GetNode<CollisionShape2D>("Node2D/DoorOpener/StaticBody2D2/CaveEntrance2");
        _scrub = GetTree().Root.GetNode<CollisionShape2D>("Node2D/Big Tree/StaticBody2D/CollisionShape2D");
        _animTree = GetTree().Root.GetNode<AnimationPlayer>("Node2D/Node2D/AnimationPlayer");
        _animEntrance = GetTree().Root.GetNode<AnimationPlayer>("Node2D/CaveEntranceTerminal/AnimationPlayer");
        _animDoorOpener = GetTree().Root.GetNode<AnimationPlayer>("Node2D/DoorOpener/AnimationPlayer");
        _keySprite = GetTree().Root.GetNode<Sprite2D>("Node2D/RuneHolder/Key");
        _scar = GetTree().Root.GetNode<AiNode>("Node2D/Scar");
        if (GetParent().Equals(_scar))
        {
            _scar.GetNode<VisibleForAI>("VisibleForAI").QueueFree();
        }
    }

    public async Task Trigger(Node caller)
    {
        if (GetParent<AiNode>().Name.Equals("End"))
        {
            AnimationPlayer fade = GetTree().Root.GetNode<AnimationPlayer>("Node2D/ColorRect/FadeToWhite");
            fade.Play("fade_to_white");

            await Task.Delay(3000);

            Node2D oben = GetTree().Root.GetNode<Node2D>("Node2D");
            PackedScene victoryScene = ResourceLoader.Load<PackedScene>("scenes/prefabs/VictoryScreen.tscn");
            Node2D victoryInstance = victoryScene.Instantiate<Node2D>();
            oben.AddChild(victoryInstance);
        }

        //Ally response
        if (!string.IsNullOrEmpty(SystemMessageForAlly) && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            ally.Chat.SendSystemMessage(SystemMessageForAlly, new Ally());
        }

        //Open door
        if (GetParent<AiNode>().Name.Equals("LockedDoor") && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            if (!ally.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.Key))
            {
                Ally? confirmedAlly = caller as Ally;
                Chat confirmedAllyChat = confirmedAlly!.Chat;
                confirmedAllyChat.SendSystemMessage("The door is locked, you cannot enter.", new Ally());
                return;
            }
        }

        //Fill bucket with water
        if (GetParent<AiNode>().Name.Equals("Well") && caller.Name.ToString().Contains("Ally"))
        {
            Ally ally = (caller as Ally)!;
            ally.SsInventory.HardSwapItems(Game.Scripts.Items.Material.BucketEmpty, Game.Scripts.Items.Material.BucketWater);
            ally.AnimationIsAlreadyPlaying = true;
            ally.AnimPlayer.Play("Fill-Bucket");
        }
        //Remove scrub with Jones
        if (GetParent<AiNode>().Name.Equals("Big Tree") && caller.Name.ToString().Equals("Ally2") && !ScrubRemoved)
        {
            ScrubRemoved = true;
            Ally? jones = caller as Ally;
            Chat jonesChat = jones!.Chat;
            jonesChat.SendSystemMessage("You've successfully removed the scrub from the tree and a big hideous scar appears underneath it", new Ally());
            _animTree.Play("TreeAnimation");
            GD.Print("Scrub removed!");
            VisibleForAI scarVisibileForAI = new()
            {
                NameForAi = "Scar",
                DescriptionForAi = "A big scar on the tree which could be the reason for the tree looking dead. It is not reachable because of the scrub"
            };
            _scar.AddChild(scarVisibileForAI, false);
            GD.Print("Scar VFAI added");
            _scrub.SetDeferred("disabled", true);
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
                ally!.Chat.SendSystemMessage("Can't interact. Maybe there is something missing.", new Ally());
                return;
            }

            ally.AnimationIsAlreadyPlaying = true;
            ally.AnimPlayer.Play("Empty-Bucket");
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

        if (GetParent<AiNode>().Name.Equals("Key") && caller.Name.ToString().Equals("Ally"))
        {
            _keySprite.Visible = false;
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
