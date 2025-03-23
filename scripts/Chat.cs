using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Game.Scripts.AI;

using GenerativeAI.Exceptions;

using Godot;
namespace Game.Scripts
{
    public partial class Chat : LineEdit
    {
        [Signal] public delegate void ResponseReceivedEventHandler(string response, Ally? sender);

        [Export(PropertyHint.File, "ally_system_prompt.txt")]
        private string? _systemPromptFile;

        //  [Export(PropertyHint.File, "introduction_ally_system_prompt.txt")]
        //  private string? _introductionSystemPromptFile;

        private Ally _ally = null!;

        private string _systemPrompt = "";
        //    private string _introductionSystemPrompt = "";
        public GeminiService? GeminiService;
        private readonly string _apiKeyPath = ProjectSettings.GlobalizePath("res://api_key.secret");
        private const string ChatPlaceholder = "Type here to chat", EnterApiPlaceholder = "Enter API key";

        private int _responseCount;
        private readonly List<VisibleForAI> _alreadySeen = [];
        private Godot.Collections.Array<Node> _entityList = null!;
        private VisibleForAI _ally1VisibleForAi = null!, _ally2VisibleForAi = null!;

        public override void _Ready()
        {
            _ally = GetParent().GetParent<Ally>();
            _responseCount = 0;
            TextSubmitted += OnTextSubmitted;
            //  AlreadySeen = _ally.AlwaysVisible.ToList();

            string systemPromptAbsolutePath = ProjectSettings.GlobalizePath(_systemPromptFile);
            //   string introductionSystemPromptAbsolutePath = ProjectSettings.GlobalizePath(_introductionSystemPromptFile);

            _systemPrompt = File.ReadAllText(systemPromptAbsolutePath); // Load system prompt into SystemPrompt
                                                                        // _introductionSystemPrompt = File.ReadAllText(introductionSystemPromptAbsolutePath); // Load intro prompt

            InitializeGeminiService(_systemPrompt); // Pass system prompt to InitializeGeminiService
            /* foreach (Ally ally in GetTree().GetNodesInGroup("Entities").OfType<Ally>())
             {
                 if (ally.GetName().ToString().Contains('2'))
                 {
                     _ally2VisibleForAi = ally.GetNode<VisibleForAI>("VisibleForAI");
                     AlreadySeen.Add(_ally2VisibleForAi);
                 }
                 else
                 {
                     _ally1VisibleForAi = ally.GetNode<VisibleForAI>("VisibleForAI");
                     AlreadySeen.Add(_ally1VisibleForAi);
                 }
             }
             */
        }

        public async void SeenItems()
        {
            List<VisibleForAI> newItems = [];
            List<VisibleForAI> visibleItems = _ally.GetCurrentlyVisible();

            if (visibleItems.Count > 0)
            {
                foreach (VisibleForAI item in visibleItems)
                {
                    bool isContains = false;
                    foreach (VisibleForAI vfai in _alreadySeen)
                    {
                        if (vfai == item) { isContains = true; break; }
                    }
                    if (!isContains && item.NameForAi.Trim() != "")
                    {
                        _alreadySeen.Add(item);
                        newItems.Add(item);
                    }
                }
            }
            if (newItems.Count > 0)
            {
                GD.Print("prompt");
                string alreadySeenFormatted = string.Join("\n", _alreadySeen);
                string newItemsFormatted = string.Join("\n", newItems);
                string completeInput = $"New Objects:\n\n{newItemsFormatted}\n\n" + $"Already Seen:\n\n{alreadySeenFormatted}\n\n" + "Player: ";

                GD.Print($"-------------------------\nInput:\n{completeInput}");

                string? response = await GeminiService!.MakeQuery(completeInput);
                if (response != null)
                {
                    Ally dummy = new Ally();
                    EmitSignal(SignalName.ResponseReceived, response, dummy);
                    GD.Print($"----------------\nResponse:\n{response}");
                }
                else
                {
                    GD.Print("No response");
                }

                newItems.Clear();
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            SeenItems();
        }
        private void InitializeGeminiService(string systemPrompt)
        {
            try
            {
                GeminiService = new GeminiService(_apiKeyPath, systemPrompt); // Pass system prompt to GeminiService constructor
                PlaceholderText = ChatPlaceholder;
            }

            catch (Exception ex)
            {
                GD.Print(ex.Message);
                PlaceholderText = EnterApiPlaceholder;
            }
        }

        public async void SendSystemMessage(string systemMessage, Ally? sender)
        {
            GD.Print($"Sending message from: {sender!.Name}, Message: {systemMessage}"); // ADD THIS
            try
            {
                string? txt = await GeminiService!.MakeQuery("[SYSTEM MESSAGE] " + systemMessage + " [SYSTEM MESSAGE END] \n"); GD.Print(txt); // put it into text box
                if (txt == null)
                {
                    GD.Print("AI response is null.");
                }
                GetParent<Camera2D>().GetParent<Ally>().HandleResponse(txt!, sender);
            }
            catch (Exception e)
            {
                throw new GenerativeAIException("AI query got an error.", "at system_message: " + systemMessage + " with error message " + e.Message);
            }
        }


        private async void OnTextSubmitted(string input)
        {
            List<VisibleForAI> visibleItems = _ally.GetCurrentlyVisible().Concat(_ally.AlwaysVisible).ToList();
            string visibleItemsFormatted = string.Join("\n", visibleItems),
                alreadySeenFormatted = string.Join("\n", _alreadySeen),
                completeInput = $"New Objects:\n\n\n\n" + $"Already Seen:\n\n{alreadySeenFormatted}\n\n" + $"Player: {input}";
            GD.Print($"-------------------------\nInput:\n{completeInput}");

            if (GeminiService != null)
            {
                string? response = await GeminiService.MakeQuery(completeInput);
                if (response != null)
                {
                    Ally dummy = new Ally();
                    EmitSignal(SignalName.ResponseReceived, response, dummy);
                    GD.Print($"----------------\nResponse:\n{response}");
                }

                else
                {
                    GD.Print("No response");
                }
            }
            else
            {
                await File.WriteAllTextAsync(_apiKeyPath, input.Trim());
                InitializeGeminiService(_systemPrompt);
            }

            Clear();
        }
    }
}
