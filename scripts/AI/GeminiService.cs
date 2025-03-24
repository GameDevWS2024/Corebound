using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

using GenerativeAI.Exceptions;
using GenerativeAI.Methods;
using GenerativeAI.Models;
using GenerativeAI.Types;

using Godot;
using Godot.Collections;

namespace Game.Scripts.AI;

public partial class GeminiService : Node
{
    private readonly GenerativeModel _model;
    public ChatSession Chat;
    public GeminiService(string apiKeyFilePath, string systemPrompt) // Add systemPrompt parameter
    {
        if (string.IsNullOrEmpty(apiKeyFilePath))
        {
            throw new ArgumentNullException(nameof(apiKeyFilePath), "API key file path cannot be null or empty");
        }
        if (!File.Exists(apiKeyFilePath))
        {
            throw new FileNotFoundException($"API key file not found at path: {apiKeyFilePath}");
        }
        try
        {
            // Read the API key from the file, trimming any whitespace
            string apiKey = File.ReadAllText(apiKeyFilePath).Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("API key file is empty");
            }
            _model = new GenerativeModel(apiKey: apiKey, model: "gemini-2.0-flash-lite")
            {
                // gemini-2.0-pro-exp-02-05
                SafetySettings =
                [
                    new SafetySetting { Category = HarmCategory.HARM_CATEGORY_HARASSMENT, Threshold = HarmBlockThreshold.BLOCK_NONE },
                    new SafetySetting { Category = HarmCategory.HARM_CATEGORY_HATE_SPEECH, Threshold = HarmBlockThreshold.BLOCK_NONE },
                    new SafetySetting { Category = HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT, Threshold = HarmBlockThreshold.BLOCK_NONE },
                    new SafetySetting { Category = HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT, Threshold = HarmBlockThreshold.BLOCK_NONE }
                ]
            };

            Chat = _model.StartChat(new StartChatParams());

            // Send system prompt as the first message in the chat history
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                Task.Run(async () => await Chat.SendMessageAsync(systemPrompt)).Wait(); // Run synchronously for initialization. Be cautious in real async scenarios.
            }
        }
        catch (Exception ex) when (ex is not FileNotFoundException && ex is not ArgumentNullException)
        {
            throw new Exception($"Error reading API key from file: {ex.Message}", ex);
        }
    }

    private readonly Queue<string> _queryQueue = new Queue<string>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public bool IsBusy()
    {
        return _queryQueue.Count > 0;
    }

    public async Task<string?> MakeQuery(string input)
    {
        _queryQueue.Enqueue(input);
        return await ProcessQueryQueue();
    }

    private async Task<string?> ProcessQueryQueue()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_queryQueue.Count == 0)
            {
                return null;
            }

            string input = _queryQueue.Dequeue();

            string? result = null;
            int tryCount = 0;
            while (result is null && tryCount++ <= 3) // try to get a response 3 times
            {
                result = await InternalSendMessage(input);
                if (result == null)
                {
                    GD.Print("waiting for 1s because query failed.");
                    await Task.Delay(1000);
                }
            }

            if (tryCount == 3)
            {
                GD.Print("tried 3 times but didn't get a response. Giving up now.");
            }
        
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> InternalSendMessage(string input)
    {
        string? result = null;
        try
        {
            result = await Chat.SendMessageAsync(input);
        }
        catch (Exception ex)
        {
            GD.Print(ex.Message);
        }
        return result;
    }

    public IReadOnlyList<Content> GetChatHistory()
    {
        return Chat.History;
    }

    public void ClearChat()
    {
        Chat = _model.StartChat(new StartChatParams());
    }
}
