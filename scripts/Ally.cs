using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

using Game.Scenes.Levels;
using Game.Scripts.AI;
using Game.Scripts.Items;

using Godot;

using Vector2 = Godot.Vector2;

namespace Game.Scripts;

public partial class Ally : CharacterBody2D
{
    [Export] RichTextLabel _responseField = null!;
    [Export] public PathFindingMovement PathFindingMovement = null!;
    [Export] private Label _nameLabel = null!;
    private Motivation _motivation = null!;
    private Health _health = null!;
    protected Game.Scripts.Core _core = null!;
    public Inventory SsInventory = new Inventory(12);

    private RichTextLabel _ally1ResponseField = null!;
    private RichTextLabel _ally2ResponseField = null!;

    [Export] private int _visionRadius = 300;
    [Export] private int _interactionRadius = 150;
    private bool _interactOnArrival, _busy, _reached, _harvest, _returning;

    [Export] public Chat Chat = null!;
    public Map? Map;
    [Export] public VisibleForAI[] AlwaysVisible = [];
    private GenerativeAI.Methods.ChatSession? _chat;
    private GeminiService? _geminiService;
    private readonly List<string> _interactionHistory = [];

    public Boolean Lit = false;

    [Export] private int _maxHistory = 5; // Number of interactions to keep

    //Enum with states for ally in darkness, in bigger or smaller circle for map damage system
    public enum AllyState
    {
        Darkness,
        SmallCircle,
        BigCircle
    }
    public AllyState CurrentState { get; private set; } = AllyState.SmallCircle;

    public override void _Ready()
    {
        /*
        SsInventory.AddItem(new Itemstack(Game.Scripts.Items.Material.Torch));
        lit = true; */
        // SsInventory.AddItem(new Itemstack(Items.Material.Torch, 1));


        _ally1ResponseField = GetNode<RichTextLabel>("ResponseField");
        _ally2ResponseField = GetNode<RichTextLabel>("ResponseField");

        _core = GetTree().GetNodesInGroup("Core").OfType<Core>().FirstOrDefault()!;
        Map = GetTree().Root.GetNode<Map>("Node2D");

        _geminiService = Chat.GeminiService;
        _chat = _geminiService!.Chat;
        if (_chat == null)
        {
            GD.PrintErr("Chat node is not assigned in the editor!");
            return;
        }
        if (_geminiService == null)
        {
            GD.PrintErr("Gemini node is not assigned in the editor!");
            return;
        }
        base._Ready();
        _motivation = GetNode<Motivation>("Motivation");
        _health = GetNode<Health>("Health");

        GD.Print(GetTree().GetFirstNodeInGroup("Core").GetType());

        Chat.Visible = false;
        PathFindingMovement.ReachedTarget += HandleTargetReached;
        Chat.ResponseReceived += HandleResponse;
    }

    private async void HandleTargetReached()
    {

    }

    public List<VisibleForAI> GetCurrentlyVisible()
    {
        IEnumerable<VisibleForAI> visibleForAiNodes =
            GetTree().GetNodesInGroup(VisibleForAI.GroupName).OfType<VisibleForAI>();
        return visibleForAiNodes.Where(node => GlobalPosition.DistanceTo(node.GlobalPosition) <= _visionRadius).Where(node => node.GetParent() != this)
            .ToList();
    }

    public List<Interactable> GetCurrentlyInteractables()
    {
        IEnumerable<Interactable> interactable =
            GetTree().GetNodesInGroup(Interactable.GroupName).OfType<Interactable>();
        return interactable.Where(node => GlobalPosition.DistanceTo(node.GlobalPosition) <= _interactionRadius)
            .ToList();
    }

    public void SetAllyInDarkness()
    {
        // Berechne den Abstand zwischen Ally und Core
        Vector2 distance = this.GlobalPosition - _core.GlobalPosition;
        float distanceLength = distance.Length(); // Berechne die Länge des Vektors

        // If ally further away than big circle, he is in the darkness
        if (distanceLength > Core.LightRadiusBiggerCircle)
        {
            CurrentState = AllyState.Darkness;
        }
        //if ally not in darkness and closer than the small Light Radius, he is in small circle
        else if (distanceLength < Core.LightRadiusSmallerCircle)
        {
            CurrentState = AllyState.SmallCircle;
        }
        //if ally not in darkness and not in small circle, ally is in big circle
        else
        {
            CurrentState = AllyState.BigCircle;
        }

    }

    public override void _PhysicsProcess(double delta)
    {
        //Check where ally is (darkness, bigger, smaller)
        SetAllyInDarkness();

        UpdateTarget();

        _reached = GlobalPosition.DistanceTo(PathFindingMovement.TargetPosition) < 150;


        if (_harvest && _reached) // Harvest logic
        {
            Harvest();
        }


        //Torch logic:
        if (SsInventory.ContainsMaterial(Game.Scripts.Items.Material.Torch) && GlobalPosition.DistanceTo(new Vector2(3095, 4475)) < 300)
        {
            Lit = true;
            // remove unlit torch from inv and add lighted torch
            SsInventory.HardSwapItems(Items.Material.Torch, Items.Material.LightedTorch);

            // async func call to print response to torch lighting
            Torchlightingresponse();

            GD.Print("tryna respond to torch lighting event");

            //GD.Print("homie hat die Fackel und ist am core");
            /* GD.Print("Distance to core" + GlobalPosition.DistanceTo(GetNode<Core>("%Core").GlobalPosition));
             GD.Print("Core position" + GetNode<Core>("%Core").GlobalPosition);
             GD.Print("Core position" + GetNode<PointLight2D>("%CoreLight").GlobalPosition);
             */
        }
        if (Lit)
        {
            //    GetParent().GetNode<ShowWhileInRadius>("Abandoned Village/HauntedForestVillage/Big House/Sprite2D/InsideBigHouse2/InsideBigHouse/Sprite2D/ChestInsideHouse").ItemActivationStatus = GlobalPosition.DistanceTo(GetParent().GetNode<Node2D>("Abandoned Village/HauntedForestVillage/%Big House").GlobalPosition) < 1000;
            GetTree().Root.GetNode<ShowWhileInRadius>(
                    "Node2D/Abandoned Village/HauntedForestVillage/Big House/Sprite2D/InsideBigHouse2/InsideBigHouse/Sprite2D/ChestInsideHouse")
            .ItemActivationStatus = GlobalPosition.DistanceTo(new Vector2(8650, -1315)) < 1000;
        }
    }//Node2D/Abandoned Village/HauntedForestVillage/Big House/Sprite2D/InsideBigHouse2/InsideBigHouse/Sprite2D/ChestInsideHouse

    private async void Torchlightingresponse()
    {
        string? txt = "";
        int ctr = 0;
        while (txt is null or "" && ctr <= 3)
        {
            txt = await _geminiService!.MakeQuery("[SYSTEM MESSAGE] The torch has now been lit by the commander using the CORE. Tell the Commander what a genius idea it was to use the Core for that purpose and hint the commander back at the haunted forest village. [SYSTEM MESSAGE END] \n"); GD.Print(txt); // put it into text box
            HandleResponse(txt!);
            ctr++;
            GD.Print();
        }
    }
    private void UpdateTarget()
    {
        if (_harvest)
        {
            if (_returning)
            {
                PointLight2D cl = _core.GetNode<PointLight2D>("CoreLight");
                Vector2 targ = new Vector2(0, 500); // cl.GlobalPosition;
                                                    // Target = core
                PathFindingMovement.TargetPosition = _core.GlobalPosition;
                GD.Print("Target position (should be CORE): " + PathFindingMovement.TargetPosition.ToString());
            }
            else
            {
                Location nearestLocation = Map.GetNearestItemLocation(new Location(GlobalPosition))!;
                //GD.Print("going to nearest loc("+nearestLocation.X +", "+nearestLocation.Y+") from "+ GlobalPosition.X + " " + GlobalPosition.Y);    //Target = nearest item
                PathFindingMovement.TargetPosition = nearestLocation.ToVector2();

            }
        }
    }

    private List<(string, string)>? _matches;
    private string _richtext = "", _part = "";
    private async void HandleResponse(string response)
    {
        if (response.Contains("INTERACT"))
        {
            Interact();
        }
        _matches = ExtractRelevantLines(response);
        _richtext = "";
        foreach ((string op, string content) in _matches!)
        {
            _part = "";
            _richtext += FormatPart(_part, op, content);

            // differentiate what to do based on command op
            switch (op)
            {
                case "MOTIVATION": // set motivation from output
                    _motivation.SetMotivation(content.ToInt());
                    break;
                case "INTERACT":
                    Interact();
                    break;
                case "GOTO AND INTERACT":
                    SetInteractOnArrival(true);
                    Goto(content);
                    break;
                case "GOTO":
                    GD.Print("DEBUG: GOTO Match");
                    Goto(content);
                    break;
                case "HARVEST"
                    when !_busy && Map.Items.Count > 0
                    : // if harvest command and not walking somewhere and items on map
                    GD.Print("harvesting");
                    Harvest();
                    break;
                case "STOP": // stop command stops ally from doing anything
                    _harvest = false;
                    _busy = false;
                    break;
                default:
                    GD.Print("DEBUG: NO MATCH FOR : " + op);
                    break;
            }
        }

        _responseField.ParseBbcode(_richtext); // formatted text into response field
        ButtonControl buttoncontrol = GetTree().Root.GetNode<ButtonControl>("Node2D/UI");
        if (buttoncontrol == null)
        {
            GD.Print("Button control not found");
        }
        else
        {
            RichTextLabel label = this.Name.ToString().Contains('2') ? _ally2ResponseField : _ally1ResponseField;
            await buttoncontrol.TypeWriterEffect(_richtext, label);
        }
    }

    private void SetInteractOnArrival(bool interactOnArrival)
    {
        _interactOnArrival = interactOnArrival;
    }

    private void Goto(String content)
    {
        const string goToPattern = @"^\s*\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)\s*$";
        Match goToMatch = Regex.Match(content.Trim(), goToPattern);

        if (goToMatch.Success)
        {
            int x = int.Parse(goToMatch.Groups[1].Value), y = int.Parse(goToMatch.Groups[2].Value);
            // GD.Print(new Vector2(x, y).ToString());
            GetNode<PathFindingMovement>("PathFindingMovement").GoTo(new Vector2(x, y));
        }
        else
        {
            GD.Print($"goto couldn't match the position, content was: '{content}'");
        }
    }

    private static string FormatPart(string part, string op, string content)
    {
        return part += op switch // format response based on different ops or response types
        {
            "MOTIVATION" => "",
            "THOUGHT" => "[i]" + content + "[/i]\n",
            "RESPONSE" or "COMMAND" or "STOP" => "[b]" + content + "[/b]\n",
            _ => content + "\n"
        };
    }

    private void Interact()
    {
        Interactable? interactable = GetCurrentlyInteractables().FirstOrDefault();
        interactable?.Trigger(this);
        _interactOnArrival = false;
        if (interactable == null)
        {
            GD.Print("Interactable null");
        }
        /*GD.Print("Interacted");
        List<VisibleForAI> visibleItems = GetCurrentlyVisible().Concat(AlwaysVisible).ToList();
        string visibleItemsFormatted = string.Join<VisibleForAI>("\n", visibleItems);
        string completeInput = $"Currently Visible:\n\n{visibleItemsFormatted}\n\n";

        string originalSystemPrompt = Chat.SystemPrompt;
        Chat.SystemPrompt =
            "[System Message] In the following you'll get a list of things you see with coordinates. Respond by telling the commander just what might be important or ask clarifying questions on what to do next. \n";
        string? arrivalResponse = await _geminiService!.MakeQuery(completeInput + "[System Message End] \n");
        RichTextLabel label = GetNode<RichTextLabel>("ResponseField");
        label.Text += "\n" + arrivalResponse;

        Chat.SystemPrompt = originalSystemPrompt;*/
        SetInteractOnArrival(true);
        GD.Print("DEBUG: INTERACT Match");
    }

    public static List<(string, string)>? ExtractRelevantLines(string response)
    {
        string[] lines = response.Split('\n').Where(line => line.Length > 0).ToArray();
        List<(string, string)>? matches = [];

        // Add commands to be extracted here
        List<String> ops =
        [
            "MOTIVATION",
            "THOUGHT",
            "RESPONSE",
            "REMEMBER",
            "GOTO AND INTERACT",
            "GOTO",
            "INTERACT",
            "HARVEST",
            "FOLLOW",
            "STOP"
        ];

        foreach (string line in lines)
        {
            foreach (string op in ops)
            {
                string pattern = op + @"[\s:]+.*"; // \b matcht eine Wortgrenze
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                Match match = regex.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                matches.Add((op, match.Value[(op.Length + 1)..].Trim())); // Extract the operand
                break;
            }
        }

        response = "";
        return matches;
    }

    private void Harvest()
    {
        if (!_returning)
        {
            // extract the nearest item and add to inventory (pickup)
            if (SsInventory.HasSpace()) // if inventory has space
            {
                GD.Print("harvesting...");
                Itemstack item = Map.ExtractNearestItemAtLocation(new Location(GlobalPosition));
                GD.Print(item.Material + " amount: " + item.Amount);
                SsInventory.AddItem(item); // add item to inventory
                SsInventory.Print();
            } // if inventory has no space don't harvest it
            else
            {
                GD.Print("No space");
            }

            _returning = true;
        }
        else
        {
            // Empty inventory into the core

            foreach (Itemstack item in SsInventory.GetItems())
            {
                if (item.Material == Game.Scripts.Items.Material.None)
                {
                    continue;
                }

                Core.IncreaseScale();
                GD.Print("Increased scale");
            }

            SsInventory.Clear();
            _busy = false; // Change busy state  
            _harvest = false; // Change harvest state
            _returning = false; // Change returning state
        }
    }
}
