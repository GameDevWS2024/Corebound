using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Game.Scripts.AI;
using Godot;

using Environment = System.Environment;
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
        _audioPlayer.Finished += OnAudioFinished; // Connect the signal here
        _geminiService = new GeminiService(ProjectSettings.GlobalizePath("res://api_key.secret"),
            "You will get tasks of choosing an appropriate emotion for a text. Reply ONLY with the responding emotion, nothing else.");
    }

    private static string? GetPythonExecutablePath()
    {
        // Prioritize environment variable (more reliable across platforms)
        string? pythonPath = Environment.GetEnvironmentVariable("PYTHON_PATH");
        if (!string.IsNullOrEmpty(pythonPath))
        {
            return pythonPath;
        }

        // Fallback to registry (Windows-only)
        #if WINDOWS
        try
        {
            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Python\PythonCore");
            if (key != null)
            {
                string[]? subKeyNames = key.GetSubKeyNames();
                if (subKeyNames != null && subKeyNames.Length > 0)
                {
                    string version = subKeyNames[0]; // Assumes a single version, gets the first one.  Consider sorting if needed.
                    using Microsoft.Win32.RegistryKey? versionKey = key.OpenSubKey(version + @"\InstallPath");
                    if (versionKey != null)
                    {
                        string? installPath = versionKey.GetValue(null)?.ToString();
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            return Path.Combine(installPath, "python.exe");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error reading registry: {ex.Message}"); // More specific error
        }
        #endif

        return null; // No Python found
    }

    public async Task GenerateAndPlaySpeech(string text)
    {
        // Pfad zum Python-Skript ermitteln
        string scriptDirectory = Path.GetDirectoryName(ProjectSettings.GlobalizePath("res://" + PythonScriptPath)) ??
                                 throw new InvalidOperationException("Invalid script path.");

        string? pythonPath = GetPythonExecutablePath();

        if (string.IsNullOrEmpty(pythonPath))
        {
            GD.PrintErr("Python executable not found. Please set the PYTHON_PATH environment variable.");
            return;
        }

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
                ProcessStartInfo startInfo = new()
                {
                    FileName = pythonPath,
                    Arguments = $"\"{Path.Combine(scriptDirectory, PythonScriptPath)}\" \"{text}\" --style \"{DefaultStyle}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = scriptDirectory
                };


                using Process process = new();
                process.StartInfo = startInfo;
                process.Start();

                string error = process.StandardError.ReadToEnd(); // Read errors
                process.WaitForExit(); // Wait *inside* the Task.Run
                int exitCode = process.ExitCode;

                //Verwende CallDeferred um im Hauptthread Aktionen auszuführen
                CallDeferred(nameof(HandleProcessCompletion), audioFilePath, "", error, exitCode);

            });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error starting process: {ex.Message}");
            GD.PrintErr(ex.StackTrace); // Add StackTrace for debugging
            return;
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

            // Versuche, das Format aus der WAV-Datei zu ermitteln (robuster)
            if (wavData.Length >= 44) // Mindestgröße für Header
            {
                // WAV-Header-Parsing (vereinfacht, für PCM)
                ushort formatTag = BitConverter.ToUInt16(wavData, 20);
                ushort channels = BitConverter.ToUInt16(wavData, 22);
                int sampleRate = BitConverter.ToInt32(wavData, 24);
                ushort bitsPerSample = BitConverter.ToUInt16(wavData, 34);


                audioStream.Format = bitsPerSample switch
                {
                    8 => AudioStreamWav.FormatEnum.Format8Bits,
                    16 => AudioStreamWav.FormatEnum.Format16Bits,
                    _ => AudioStreamWav.FormatEnum.Format16Bits // Default, handle other cases if needed
                };

                audioStream.MixRate = sampleRate;
                audioStream.Stereo = channels == 2;
            }
            else
            {
                //Fallback auf Standardwerte, oder wirf einen Fehler wenn die Header-Infos nicht da sind
                GD.PrintErr("Invalid WAV file.  Using default format.");
                audioStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
                audioStream.MixRate = 44100; // or 16000, a common default.
                audioStream.Stereo = false;
            }

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
       
        string audioFilePath = Path.Combine(Path.GetDirectoryName(ProjectSettings.GlobalizePath("res://" + PythonScriptPath)) ?? throw new InvalidOperationException(), "output.wav");

        try
        {
            if (File.Exists(audioFilePath))
            {
                File.Delete(audioFilePath);
                GD.Print("Audio file deleted.");
            }
            else
            {
                GD.Print($"File does not exist: {audioFilePath}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error deleting file: {ex.Message}");
        }
    }
}