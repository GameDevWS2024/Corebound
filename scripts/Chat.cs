using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private Ally _ally = null!;
        private string _systemPrompt = "";
        public GeminiService? GeminiService;
        private readonly string _apiKeyPath = ProjectSettings.GlobalizePath("res://api_key.secret");
        private const string ChatPlaceholder = "Type here to chat", EnterApiPlaceholder = "Enter API key";

        private readonly List<VisibleForAI> _alreadySeen = [];

		public override void _Ready()
		{
			_ally = GetParent().GetParent<Ally>();
			TextSubmitted += OnTextSubmitted;

			string systemPromptAbsolutePath = ProjectSettings.GlobalizePath(_systemPromptFile);
			_systemPrompt = File.ReadAllText(systemPromptAbsolutePath);

			InitializeGeminiService(_systemPrompt);

            // Create a Timer to control SeenItems frequency
            Timer timer = new()
            {
                WaitTime = 1.0f, // Adjust as needed (e.g., 1 second)
                OneShot = false
            };
            timer.Timeout += OnTimerTimeout;
            AddChild(timer);
            timer.Start();
        }

        private async void OnTimerTimeout() => SeenItems();

        public async Task SeenItems()
        {
            List<VisibleForAI> newItems = [], visibleItems = _ally.GetCurrentlyVisible();

            if (visibleItems.Count > 0)
            {
                foreach (VisibleForAI item in visibleItems)
                {
                    bool isContains = _alreadySeen.Contains(item);
                    if (!isContains && !string.IsNullOrWhiteSpace(item.NameForAi))
                    {
                        _alreadySeen.Add(item);
                        newItems.Add(item);
                    }
                }
            }

            if (newItems.Count > 0)
            {
                string alreadySeenFormatted = string.Join("\n", _alreadySeen.Select(v => v.NameForAi));
                string newItemsFormatted = string.Join("\n", newItems.Select(v => v.NameForAi));
                string completeInput = $"New Objects:\n\n{newItemsFormatted}\n\nAlready Seen:\n\n{alreadySeenFormatted}\n\nPlayer: ";

                GD.Print($"-------------------------\nInput:\n{completeInput}");

                if (GeminiService != null)
                {
                    string? response = await GeminiService.MakeQuery(completeInput); // Run on background thread
                    if (response != null)
                    {
                        Ally dummy = new();
                        EmitSignal(SignalName.ResponseReceived, response, dummy);
                        GD.Print($"----------------\nResponse:\n{response}");
                    }
                    else
                    {
                        GD.Print("No response");
                    }
                }
                newItems.Clear();
            }
        }

		private void InitializeGeminiService(string systemPrompt)
		{
			try
			{
				GeminiService = new GeminiService(_apiKeyPath, systemPrompt);
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
            GD.Print($"Sending message from: {sender!.Name}, Message: {systemMessage}");
            try
            {
                string? txt = await Task.Run(() => GeminiService!.MakeQuery("[SYSTEM MESSAGE] " + systemMessage + " [SYSTEM MESSAGE END] \n"));
                GD.Print(txt);
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

        private void OnTextSubmitted(string input)
        {
            Task.Run(() => HandleInputAsync(input));
        }

        private async Task HandleInputAsync(string input)
        {
            List<VisibleForAI> visibleItems = _ally.GetCurrentlyVisible().Concat(_ally.AlwaysVisible).ToList();
            string alreadySeenFormatted = string.Join("\n", _alreadySeen.Select(v => v.NameForAi));
            string completeInput = $"New Objects:\n\n\n\nAlready Seen:\n\n{alreadySeenFormatted}\n\nPlayer: {input}";

            GD.Print($"-------------------------\nInput:\n{completeInput}");

            if (GeminiService == null)
            {
                await File.WriteAllTextAsync(_apiKeyPath, input.Trim());
                InitializeGeminiService(_systemPrompt);
            }
            else
            {
                string? response = await GeminiService.MakeQuery(completeInput); //Run on background thread
                if (response is not null or "")
                {
                    EmitSignal(SignalName.ResponseReceived, response, new Ally());
                    GD.Print($"----------------\nResponse:\n{response}");
                }
                else
                {
                    GD.Print("No response");
                }
            }
            Clear();
        }
    }
}
