using System;
using System.Collections.Generic;
using System.Linq;

using Game.Scripts;
using Game.Scripts.AI;
using Game.Scripts.Interaction;
using Game.Scripts.Items;

using Godot;
using Godot.Collections;

using Material = Game.Scripts.Items.Material;

[GlobalClass]
public partial class ShowWhileInRadius : Node2D
{
    [Export] public PackedScene? SceneToShow { get; set; }
    [Export] public int Radius { get; set; }
    public bool IsInArea { get; set; } = false;

    [Export] public Material NeedsToBeInInventoryName { get; set; }
    [Export] public bool ItemActivationStatus { get; set; } = true;
    private bool _debugOnce = false;

    Core? _core = null;
    private Area2D? _insideArea;
    Godot.Collections.Array<Node> _entities = null!;
    Ally _nearestAlly = null!;
    private Array<Node> _entitiesList = null!;
    private bool _festiveStaffCollected;
    private bool _copperCollected;
    Boolean _ghostspawned = false;
    Boolean _notebookspawned = false;
    AiNode _notebookCode = null!;

    // Load the scene you want to instance.   ONLY FOR CHEST INSIDE BIG HOUSE
    private PackedScene _sceneToInstance = null!;

    public override void _Ready()
    {
        _notebookCode = GetTree().Root.GetNode<AiNode>("Node2D/NotebookWithCode");
        _core = GetTree().GetNodesInGroup("Core").Cast<Core>().SingleOrDefault();
        _entitiesList = GetTree().GetNodesInGroup("Entities");
        float dist = float.MaxValue;
        foreach (Ally ally in _entitiesList)
        {
            if (ally.GlobalPosition.DistanceTo(GlobalPosition) <= dist)
            {
                dist = ally.GlobalPosition.DistanceTo(GlobalPosition);
                _nearestAlly = ally;
            }
        }
        _sceneToInstance = GD.Load<PackedScene>("res://scenes/prefabs/ai_node.tscn");
    }

    public override void _PhysicsProcess(double delta)
    {
        GD.Print(Interactable.TreeCured);
        if (ItemActivationStatus == true && !_debugOnce)
        {
            GD.Print("ItemActivationStatus = true");
            _debugOnce = true;
        }
        base._PhysicsProcess(delta);
        Array<Node> entities = GetTree().GetNodesInGroup("Entities");
        bool show = false;
        int smallest = int.MaxValue;
        if (delta % delta * 2000 == 0)
        {
            entities = GetTree().GetNodesInGroup("Entities");
            float dist = float.MaxValue;
            foreach (Ally ally in entities)
            {
                if (ally.GlobalPosition.DistanceTo(GlobalPosition) <= dist)
                {
                    dist = ally.GlobalPosition.DistanceTo(GlobalPosition);
                    _nearestAlly = ally;
                }
            }
            //  GD.Print("nearestAlly = " + _nearestAlly.GetName());
            // GD.Print("needsToBeInInventoryName: " + NeedsToBeInInventoryName + ", has it: " +
            //          _nearestAlly.SsInventory.ContainsMaterial(NeedsToBeInInventoryName));
        }
        foreach (Node entity in entities)
        {
            if (entity is CharacterBody2D body)
            {
                // GD.Print(body!.GlobalPosition);
                /*GD.Print((body.GlobalPosition.DistanceTo(GlobalPosition) < Radius) + " dist cond., "+(NeedsToBeInInventoryName == Game.Scripts.Items.Material.None || (_nearestAlly.SsInventory.ContainsMaterial(NeedsToBeInInventoryName) && ItemActivationStatus)));
                GD.Print(show + " show state");
                GD.Print(_nearestAlly.SsInventory.ContainsMaterial(NeedsToBeInInventoryName));
                GD.Print(ItemActivationStatus);
                GD.Print(GetParent().GetParent().GetName()+"\n"); */
                if (body.GlobalPosition.DistanceTo(GlobalPosition) < Radius
                         && (NeedsToBeInInventoryName == Game.Scripts.Items.Material.None || (_nearestAlly.SsInventory.ContainsMaterial(NeedsToBeInInventoryName) && _nearestAlly.Lit)))
                {
                    show = true;

                    //creates the festive staff when the chest is spawned 
                    if (this.Name == "ChestInsideHouse" && !_festiveStaffCollected)
                    {
                        PackedScene scene = (PackedScene)ResourceLoader.Load("res://scenes/prefabs/ai_node.tscn");
                        AiNode instance = scene.Instantiate<AiNode>();
                        instance.Position = new Vector2(0, 20);
                        instance.ObjectName = "Festive Staff";
                        instance.Interactable = true;
                        instance.RemovedAfter = true;
                        instance.ObjectDescription = "A ceremonial stick which seems to have some cultural background and might have been used for rituals at (8630, -2846)";
                        instance.ObjectHint = "Tell the commander about the object you just spotted. You may use the command [GOTO and INTERACT] on this object if the commander explicitly told you to engage with it.";
                        instance.CustomOverrideMessage = "you have picked up the festive staff, it could be useful for something at the rune, you remember that there is a hole there that could be the perfect fit";
                        ItemAdder itemAdder = instance.GetNode<ItemAdder>("ItemAdder");
                        itemAdder.ItemToAdd = Game.Scripts.Items.Material.FestiveStaff;
                        itemAdder.Amount = 1;
                        itemAdder.ItemToAddName = "Festive Staff";
                        AddChild(instance);
                        _festiveStaffCollected = true;
                    }

                    /*
                   if (this.GetName() != "Sprite2D")
                   {
                       Sprite2D? sprite = GetParent() as Sprite2D;
                       if (sprite != null)
                       {
                           sprite!.Visible = true;
                       }
                   }
                   else
                   {
                       GD.Print("this is Sprite2D error");
                   }

                   if (this.GetName() == "ChestInsideHouse")
                   {
                       SpawnChild();
                       // has AiNode (Node2D) now
                       AiNode aiNode = GetNode<AiNode>("AiNode");
                       aiNode.IsVisibleForAi = true;
                       aiNode.NameForAi = "Wooden Chest";
                       aiNode.DescriptionForAi = "Seems to be locked. Maybe there's a key in the city?";
                       aiNode.IsInteractable = true;
                       aiNode.IsAddItemOnInteract = true;
                       aiNode.Amount = 1;
                       aiNode.FromChosenMaterial = Game.Scripts.Items.Material.FestiveStaff;
                   }
                   */
                }
                //For insideBigTree only
                if(body.GlobalPosition.DistanceTo(GlobalPosition) < Radius && Interactable.TreeCured) {
                    show = true;
                }


                if (entity is Ally allyinv)
                {
                    Node2D parentNode = this.GetParent<Node2D>();
                    //GD.Print("Parent Node Name: ", parentNode.Name);
                    //GD.Print("Distance to RuneHolder: ", allyinv.GlobalPosition.DistanceTo(parentNode.GlobalPosition));
                    //GD.Print("Ally has FestiveStaff: ", allyinv.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.FestiveStaff));
                    if (parentNode.Name == "Rune" && allyinv.GlobalPosition.DistanceTo(parentNode.GlobalPosition) < 250 && allyinv.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.FestiveStaff) && !_ghostspawned)
                    {
                        GD.Print("Ghost spawned");
                        PackedScene scene = (PackedScene)ResourceLoader.Load("res://scenes/prefabs/ai_node.tscn");
                        AiNode instance = scene.Instantiate<AiNode>();
                        instance.Position = new Vector2(250, 0);
                        instance.ObjectName = "Ghost";
                        instance.ObjectDescription = "A ghost appears in front of you and tells you to stay here and command another ally to get some copper";
                        instance.Interactable = false;
                        instance.RemovedAfter = false;
                        AddChild(instance);
                        _ghostspawned = true;
                    }

                    if (parentNode.Name == "Rune" && allyinv.GlobalPosition.DistanceTo(GetTree().Root.GetNode<Node2D>("Node2D/Spaceport/Spaceship").GlobalPosition) < 250 && allyinv.SsInventory.ContainsMaterial(Game.Scripts.Items.Material.Copper) && _ghostspawned && !_notebookspawned)
                    {
                        GD.Print("Notebook spawned");
                        _notebookspawned = true;
                        _notebookCode.Visible = true;
                        VisibleForAI instance = new VisibleForAI();
                        instance.NameForAi = "Notebook";
                        instance.DescriptionForAi = "A Notebook that contains the code for the runeholder which is 1234";
                        _notebookCode.AddChild(instance);
                        _notebookCode.ObjectName = "Notebook";
                        _notebookCode.ObjectDescription = "A Notebook that contains the code for the runeholder";
                        _notebookCode.CustomOverrideMessage = "Tell the commander that the code for the rune holder is 1234";
                    }
                }
            }
        }
        if (this.GetParent().Name == "Sprite2D")
        {
            Sprite2D? sprite = GetParent<Sprite2D>();
            if (sprite != null)
            {
                SetShowSceneState(sprite, show);
            }
            else
            {
                GD.Print("Sprite2D is null. Can't show chest right now!");
            }
        }


    }

    private void SpawnChild()
    {
        if (_sceneToInstance != null)
        {
            // Instance the scene.
            Node2D instance = _sceneToInstance.Instantiate<Node2D>();

            // Add the instance as a child of the current node.
            AddChild(instance);
        }
    }

    public static void SetShowSceneState(Sprite2D? sprite, bool state)
    {
        if (sprite != null)
        {
            sprite!.Visible = state;
        }
    }
}
