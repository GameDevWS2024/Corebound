using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
    private AudioOutput _audioOutput = null!;


    private RichTextLabel _ally1ResponseField = null!, _ally2ResponseField = null!;

    [Export] private int _visionRadius = 300, _interactionRadius = 150;

    private bool _interactOnArrival, _busy, _reached, _harvest, _returning;
    public bool IsTextBoxReady = true, Lit;


    [Export] public Chat Chat = null!;
    public Map? Map;
    [Export] public VisibleForAI[] AlwaysVisible = [];
    private GenerativeAI.Methods.ChatSession? _chat;
    private GeminiService? _geminiService;
    [Export] AnimationPlayer _animPlayer = null!;
    private PointLight2D _coreLight = null!;

    private PointLight2D _torch = null!;

    //Enum with states for ally in darkness, in bigger or smaller circle for map damage system
    public enum AllyState
    {
        Darkness,
        SmallCircle,
        BigCircle
    }
    public AllyState CurrentState { get; private set; } = AllyState.SmallCircle;

    private Ally _otherAlly = null!;
    public override void _Ready()
    {
        _coreLight = GetParent().GetNode<PointLight2D>("%Core/%CoreLight");
        foreach (Ally ally in GetTree().GetNodesInGroup("Entities").OfType<Ally>().ToList())
        {
            if (ally != this)
            {
                _otherAlly = ally;
            }
        }
        /*
        SsInventory.AddItem(new Itemstack(Game.Scripts.Items.Material.Torch));
        lit = true; */
        // SsInventory.AddItem(new Itemstack(Items.Material.FestiveStaff, 1));
        //SsInventory.AddItem(new Itemstack(Items.Material.Copper, 1));
        _torch = GetNode<PointLight2D>("AllyTorch");

        _ally1ResponseField = GetNode<RichTextLabel>("ResponseField");
        _ally2ResponseField = GetNode<RichTextLabel>("ResponseField");
        _audioOutput = Chat.GetNode<AudioOutput>("Speech");

        _core = GetTree().GetNodesInGroup("Core").OfType<Core>().FirstOrDefault()!;
        Map = GetTree().Root.GetNode<Map>("Node2D");

        //sorgt dafür dass die zwei allies am Anfang nicht wegrennen
        PathFindingMovement.TargetPosition = this.GlobalPosition;

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

        PathFindingMovement = GetNode<PathFindingMovement>("PathFindingMovement");
        if (PathFindingMovement == null)
        {
            GD.Print("PathFindingMovement node is not assigned in the editor!");
        }
        Chat.Visible = false;
        PathFindingMovement!.ReachedTarget += HandleTargetReached;
        if (PathFindingMovement == null)
        {
            GD.PrintErr("PathFindingMovement node is not assigned in the editor!");
        }
        Chat.ResponseReceived += HandleResponse;
        _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer2");
        _animPlayer.Play("Idle-Left");
    }

    private void HandleTargetReached()
    {
        GD.Print("HandleTargetReached");
        if (_interactOnArrival)
        {
            GD.Print("interacting on arrival\n\n");
            Interact();
            _interactOnArrival = false;
        }
        else
        {
            GD.Print("interacting off but reached target. \n\n");
        }
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
        Vector2 distance = this.GlobalPosition - _coreLight.GlobalPosition;
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

    private bool _hasSeenOtherAlly = false;
    public override void _PhysicsProcess(double delta)
    {

        if (this.GlobalPosition.DistanceTo(_otherAlly.GlobalPosition) > 1500)
        {
            _hasSeenOtherAlly = false;
        }

        if (!_torch.Enabled)
        {
            if (Lit)
            {
                _torch.Enabled = true;
            }
        }

        if (!_hasSeenOtherAlly)
        {
            foreach (VisibleForAI vfai in GetCurrentlyVisible())
            {
                if (vfai.GetParent() != this && vfai.GetParent() is Ally)
                {
                    _hasSeenOtherAlly = true;
                }
            }
        }



        //Check where ally is (darkness, bigger, smaller)
        SetAllyInDarkness();

        UpdateTarget();

        if (PathFindingMovement.CurrentDirection == PathFindingMovement.WalkingState.Left)
        {
            _animPlayer.Play("Walk-Left");
        }
        else if (PathFindingMovement.CurrentDirection == PathFindingMovement.WalkingState.Right)
        {
            _animPlayer.Play("Walk-Right");
        }
        else if (PathFindingMovement.CurrentDirection == PathFindingMovement.WalkingState.IdleLeft)
        {
            _animPlayer.Play("Idle-Left");
        }
        else { _animPlayer.Play("Idle-Right"); }

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
            Chat.SendSystemMessage("The torch has now been lit by the commander using the CORE. Tell the Commander what a genius idea it was to use the Core for that purpose and hint the commander back at the haunted forest village.", null);

            //GD.Print("homie hat die Fackel und ist am core");
            /* GD.Print("Distance to core" + GlobalPosition.DistanceTo(GetNode<Core>("%Core").GlobalPosition));
             GD.Print("Core position" + GetNode<Core>("%Core").GlobalPosition);
             GD.Print("Core position" + GetNode<PointLight2D>("%CoreLight").GlobalPosition);
             */
        }
    }//Node2D/Abandoned Village/HauntedForestVillage/Big House/Sprite2D/InsideBigHouse2/InsideBigHouse/Sprite2D/ChestInsideHouse

    private void UpdateTarget()
    {
        if (_harvest)
        {
            if (_returning)
            {
                PointLight2D cl = _core.GetNode<PointLight2D>("CoreLight");
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
    private readonly Queue<string> _responseQueue = new Queue<string>();
    public Queue<String> GetResponseQueue()
    {
        return _responseQueue;
    }
    public async void HandleResponse(string response, Ally? sender)
    {
        GD.Print($"{Name} received message from: {(sender == null ? "null" : sender.Name)}, Response: {response}"); // ADD THIS
        if (sender == this)
        {
            return; // Ignore messages from myself to prevent infinite talking loops
        }

        // send text to AudioOutput Script
        if (_audioOutput.Synthesize)
        {
            string spokenResponse = "couldn't extract";
            foreach ((string op, string content) in ExtractRelevantLines(response))
            {
                if (op == "RESPONSE")
                {
                    spokenResponse = content.Replace("\"", "");
                }
            }

            if (spokenResponse != "couldn't extract")
            {
                _audioOutput.GenerateAndPlaySpeech(spokenResponse);
                GeminiService? geminiService = new(ProjectSettings.GlobalizePath("res://api_key.secret"), "You will get tasks of choosing an appropriate emotion for a text. Reply ONLY with the responding emotion, nothing else.");

                _audioOutput.DefaultStyle = await geminiService.InternalSendMessage("Choose a correct emotion for the following text. \n" + spokenResponse + " \n The emotion options are: newscast, angry, cheerful, sad, excited, friendly, terrified, shouting, unfriendly, whispering, hopeful. Choose one and reply ONLY(!) with that emotion exactly as it is written here.\n");  // retrieve correct style from ai.
                _audioOutput.DefaultStyle = _audioOutput!.DefaultStyle.Replace("\n", "").ToLower();
                GD.Print("\n" + _audioOutput.DefaultStyle + " \n");
            }
            else
            {
                GD.Print("Couldn't extract the relevant part to be spoken.");
            }
        }
        //

        _responseQueue.Enqueue(response);
        ProcessResponseQueue();

        // probably not necessary here
        GD.Print("got response of length: " + response.Length + ". Waiting for: " + (int)(1000 * 0.009f * response.Length) + " ms.");
        await Task.Delay((int)(1000 * 0.015f * response.Length));

    }

    private async void ProcessResponseQueue()
    {
        while (_responseQueue.Count > 0)
        {
            IsTextBoxReady = false;
            string response = _responseQueue.Dequeue();
            GD.Print($"{Name}: processing response: {response}");



            /*if (!_hasSeenOtherAlly)
            {
                _otherAlly.Chat.SendSystemMessage("Hello, this is " + this.Name + ", the other ally speaking to you. Before, I've said " + response + ". What do you think about that?]", this);
                _hasSeenOtherAlly = true;
            }*/

            _matches = ExtractRelevantLines(response); // Split lines into tuples. Put command in first spot, args in second spot, keep only tuples with an allowed command
            string? richtext = "";
            foreach ((string op, string content) in _matches!) // foreach command-content-tuple
            {
                richtext += FormatPart(op, content);

                DecideWhatCommandToDo(op, content);
            }

            // formatted text with TypeWriter effect into response field
            ButtonControl buttonControl = GetTree().Root.GetNode<ButtonControl>("Node2D/UI");
            await buttonControl.TypeWriterEffect(richtext, _responseField);
            IsTextBoxReady = true;
        }
    }

    private void DecideWhatCommandToDo(string command, string content)
    {
        // differentiate what to do based on command op
        switch (command)
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
            case "GOTO": // goto (x, y) location
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
        }
    }

    private void SetInteractOnArrival(bool interactOnArrival)
    {
        _interactOnArrival = interactOnArrival;
    }

    private void Goto(String content)
    {
        Vector2 gotoLoc = GlobalPosition;

        // try matching if in form GOTO (300, 300)
        const string goToPattern = @"^\s*\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)\s*$";
        Match goToMatch = Regex.Match(content.Trim(), goToPattern);
        if (goToMatch.Success)
        {
            gotoLoc = new Vector2(int.Parse(goToMatch.Groups[1].Value), int.Parse(goToMatch.Groups[2].Value));
        }
        else
        {
            // try matching if in form GOTO 300 300
            const string goToPattern2 = @"^\s*(-?\d+)\s+(-?\d+)\s*$";
            Match goToMatch2 = Regex.Match(content.Trim(), goToPattern2);
            if (goToMatch2.Success)
            {
                gotoLoc = new Vector2(int.Parse(goToMatch2.Groups[1].Value), int.Parse(goToMatch2.Groups[2].Value));
            }
            else
            {
                // Handle the case where neither pattern matches
                GD.PrintErr("Invalid GOTO format.");
            }
        }
        PathFindingMovement.GoTo(gotoLoc);
    }

    private static string FormatPart(string op, string content)
    {
        return op switch // format response based on different ops or response types
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
        GD.Print("DEBUG: INTERACT Match");
    }

    private static List<(string, string)>? ExtractRelevantLines(string response)
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
            // "HARVEST",
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
