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

        private readonly HashSet<VisibleForAI> _alreadySeen = [];
        private Timer _seenItemsTimer = null!;

        public override void _Ready()
        {
            _ally = GetParent().GetParent<Ally>();
            TextSubmitted += OnTextSubmitted;

            string systemPromptAbsolutePath = ProjectSettings.GlobalizePath(_systemPromptFile);
            _systemPrompt = File.ReadAllText(systemPromptAbsolutePath);

            InitializeGeminiService(_systemPrompt);

            _seenItemsTimer = new Timer();
            _seenItemsTimer.WaitTime = 1.0f;
            _seenItemsTimer.OneShot = false;
            _seenItemsTimer.Timeout += OnSeenItemsTimeout;
            AddChild(_seenItemsTimer);
            _seenItemsTimer.Start();
        }

        private void OnSeenItemsTimeout()
        {
            List<VisibleForAI> visibleItems = _ally.GetCurrentlyVisible(), newItems = [];
            Task.Run(() => Task.FromResult(ProcessSeenItems(visibleItems)));
        }
        
         private async Task ProcessSeenItems(List<VisibleForAI> visibleItems)
        {
            // Diese Methode läuft jetzt in einem HINTERGRUNDTHREAD.

            // 1. Sammle die Daten (im Hintergrundthread).
            List<VisibleForAI> newItems = [];
            HashSet<string> alreadySeenNames = []; // Verwende HashSet<string> für effizienten Vergleich

            // Baue das HashSet der bereits gesehenen Namen AUßERHALB der Schleife auf.
            foreach (VisibleForAI item in _alreadySeen)
            {
                alreadySeenNames.Add(item.NameForAi);
            }

            newItems.AddRange(visibleItems.Where(item => !alreadySeenNames.Contains(item.NameForAi) && !string.IsNullOrWhiteSpace(item.NameForAi)));

            if (newItems.Count == 0)
            {
                return; // Keine neuen Items, frühzeitiger Abbruch.
            }

            // 2. Baue den String (immer noch im Hintergrundthread).
            StringBuilder sb = new();
            sb.AppendLine("New Objects:");
            foreach (VisibleForAI item in newItems)
            {
                sb.AppendLine(item.NameForAi);
            }
            sb.AppendLine();

            sb.AppendLine("Already Seen:");
            foreach (VisibleForAI item in _alreadySeen) // Gehe durch das Original-_alreadySeen
            {
                sb.AppendLine(item.NameForAi);
            }
            sb.AppendLine();
            sb.Append("Player: "); // Kein Input hier.

            string completeInput = sb.ToString();

            // 3. Sende die Anfrage und warte auf die Antwort (immer noch im Hintergrundthread).
            if (GeminiService != null)
            {
                string? response = await GeminiService.MakeQuery(completeInput);

                // 4. Wechsle ZURÜCK zum Hauptthread, um die UI zu aktualisieren.  WICHTIG!
                CallDeferred(nameof(HandleSeenItemsResponse), response);
            }
            //Füge die neuen Items zum _alreadySeen HashSet hinzu, NACHDEM du den String erstellt hast
           foreach(VisibleForAI item in newItems)
            {
                _alreadySeen.Add(item);
            }
        }

        private void HandleSeenItemsResponse(string response)
        {
            // Diese Methode läuft im HAUPTTHREAD.  Hier kannst du die UI sicher aktualisieren.

            if (!string.IsNullOrEmpty(response))
            {
                EmitSignal(SignalName.ResponseReceived, response, new Ally()); //Verwende new Ally(), _ally kann im falschen Kontext sein
                GD.Print($"----------------\nResponse:\n{response}");
            }
            else
            {
                GD.Print("No response");
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

        public async Task SendSystemMessage(string systemMessage, Ally? sender = null)
        {
            if (sender is not null)
            {
                await SendAiQuery($"[SYSTEM MESSAGE FROM {sender.Name}] " + systemMessage);
            }
            else
            {
                await SendAiQuery("[SYSTEM MESSAGE] " + systemMessage);
            }
        }

        private async Task SendAiQuery(string playerInput = "")
        {
            StringBuilder sb = new();

            sb.AppendLine("New Objects:");
            foreach (VisibleForAI item in _ally.GetCurrentlyVisible().Where(item => !_alreadySeen.Contains(item)))
            {
                sb.AppendLine(item.NameForAi);
            }
            sb.AppendLine();

            sb.AppendLine("Already Seen:");
            foreach (VisibleForAI item in _alreadySeen)
            {
                sb.AppendLine(item.NameForAi);
            }
            sb.AppendLine();
            sb.Append("Player: ");
            sb.AppendLine(playerInput);

            string completeInput = sb.ToString();
            GD.Print("SendAIQuery: Input = ", completeInput); // DEBUG: Zeige den vollständigen Input
            
            if (GeminiService != null)
            {
                GD.Print("SendAIQuery: GeminiService ist initialisiert."); // DEBUG
                // Hier nutzen wir die Queue des GeminiService.
                string? response = await GeminiService.MakeQuery(completeInput); //WICHTIG: await
                GD.Print("SendAIQuery: Antwort von GeminiService erhalten: ", response); // DEBUG: Zeige die Antwort
                if (!string.IsNullOrEmpty(response))
                {
                    EmitSignal(SignalName.ResponseReceived, response, new Ally());
                    GD.Print($"----------------\nResponse:\n{response}"); // Debug-Ausgabe
                }
                else
                {
                    GD.Print("No response");
                }
            }
            else if (!string.IsNullOrWhiteSpace(playerInput)) //API Key handling
            {
                await File.WriteAllTextAsync(_apiKeyPath, playerInput.Trim());
                InitializeGeminiService(_systemPrompt);
            }
        }


        private void OnTextSubmitted(string input)
        {
            _ = SendAiQuery(input); // Fire and forget.  Die GeminiService-Queue kümmert sich um die Reihenfolge.
            Clear(); // Sofort clearen.
        }
    }
}