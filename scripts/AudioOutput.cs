using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Game.Scripts.AI;

using Godot;

using Microsoft.Win32;

using FileAccess = Godot.FileAccess;

namespace Game.Scripts;

public partial class AudioOutput : Node
{

    [Export] public bool Synthesize = false;
    [Export] public string? DefaultStyle { get; set; } = "cheerful"; // Standardstil setzen
    [Export] public string PythonScriptPath { get; set; } = "Guy.py"; // Dateiname anpassen

    private AudioStreamPlayer _audioPlayer = null!;

    private GeminiService _geminiService = null!;

    public override void _Ready()
    {
        _audioPlayer = new AudioStreamPlayer();
        AddChild(_audioPlayer);
        _geminiService = new GeminiService(ProjectSettings.GlobalizePath("res://api_key.secret"), "You will get tasks of choosing an appropriate emotion for a text. Reply ONLY with the responding emotion, nothing else.");
    }

    private static string? GetPythonExecutablePathFromRegistry()
    {
        try
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Python\PythonCore") ??
                              throw new InvalidOperationException();
            string version = key.GetSubKeyNames()[0]; // Assumes a single version
            RegistryKey? versionKey = key.OpenSubKey(version + @"\InstallPath");
            if (versionKey != null)
            {
                Debug.Assert(versionKey != null, nameof(versionKey) + " != null");
                string? installPath = versionKey.GetValue(null)?.ToString();
                Debug.Assert(installPath != null, nameof(installPath) + " != null");
                return Path.Combine(installPath, "python.exe");
            }
        }
        catch (Exception)
        {
            // Handle exceptions (e.g., registry key not found)
        }

        return null;
    }

    public async Task GenerateAndPlaySpeech(string text)
    {
        // Pfad zum Python-Skript ermitteln
        string scriptDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scripts");
        string? pythonPath = GetPythonExecutablePathFromRegistry();
        string audioFilePath = Path.Combine(scriptDirectory, "output.wav");
        // Lösche die Datei, falls sie existiert (um alte Ergebnisse zu entfernen)
        if (File.Exists(audioFilePath))
        {
            try
            {
                File.Delete(audioFilePath);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Could not delete existing audio file: {ex.Message}");
                return; // Abbruch, wenn Löschen fehlschlägt.
            }
        }

        // Python-Prozess asynchron ausführen
        try
        {
            await Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = pythonPath;
                process.StartInfo.Arguments =
                    $"\"{Path.Join(scriptDirectory, "Guy.py")}\" \"{text}\" --style \"{DefaultStyle}\""; // Use full path
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false; // Read output
                process.StartInfo.RedirectStandardError = true; // Read errors
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = scriptDirectory; // Set working directory

                process.Start();
                //string output = process.StandardOutput.ReadToEnd(); // Read output
                string output = "";
                string error = process.StandardError.ReadToEnd(); // Read errors
                process.WaitForExit(); // Wait *inside* the Task.Run
                int exitCode = process.ExitCode;


                //Verwende CallDeferred um im Hauptthread Aktionen auszuführen
                CallDeferred(nameof(HandleProcessCompletion), audioFilePath, output, error, exitCode);

            });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error starting process: {ex.Message}");
            GD.PrintErr(ex.StackTrace); // Add StackTrace for debugging
            return; // Exit on error
        }
    }

    private void HandleProcessCompletion(string audioFilePath, string output, string error, int exitCode)
    {
        if (!string.IsNullOrWhiteSpace(output)) { GD.Print("Process Output:\n", output); }

        if (!string.IsNullOrWhiteSpace(error)) { GD.PrintErr("Process Error:\n", error); }

        if (exitCode != 0) { GD.PrintErr("Process Exit Code: ", exitCode); }


        if (File.Exists(audioFilePath))
        {
            // KORREKTE METHODE zum Laden von WAV in Godot 4:
            using FileAccess file = FileAccess.Open(audioFilePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr("Error opening WAV file: ", FileAccess.GetOpenError());
                return;
            }

            byte[] wavData = file.GetBuffer((long)file.GetLength()); // Lies gesamte Datei
            AudioStreamWav audioStream = new();
            audioStream.Data = wavData; // Setze die Daten
            audioStream.Format = AudioStreamWav.FormatEnum.Format16Bits; // Sehr wichtig!
            audioStream.MixRate = 16000;
            audioStream.Stereo = false;
            _audioPlayer.PitchScale = 1.0f;
            _audioPlayer.Stream = audioStream;
            _audioPlayer.Play();
        }
        else
        {
            GD.PrintErr("Audio file was not generated.");
        }
    }

    private void OnAudioFinished()
    {
        if (_audioPlayer.Playing)
        {
            _audioPlayer.Stop();
        }

        _audioPlayer.Stream = null;

        // Get the correct path to the audio file
        string scriptPath = ProjectSettings.GlobalizePath("res://" + PythonScriptPath);
        string scriptDirectory = Path.GetDirectoryName(scriptPath) ?? throw new InvalidOperationException();
        string audioFilePath = Path.Combine(scriptDirectory, "output.wav");

        try
        {
            if (File.Exists(audioFilePath))
            {
                File.Delete(audioFilePath);
                GD.Print("Audio file deleted.");
            }
            else { GD.Print($"File does not exist: {audioFilePath}"); }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error deleting file: {ex.Message}");
        }
    }
}
