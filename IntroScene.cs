using System.Collections.Generic;

using Game.Scripts;

using Godot;

public partial class IntroScene : Control
{
    // Nodes
    private PanelContainer? _panelContainer; // Container for holding the text box UI
    private Label? _label; // Label for displaying dialog text
    private Camera2D? _mainCamera; // Reference to the main camera
    private ColorRect? _blackoutRect; // For the "waking up" effect (screen blackout)
    [Export] private Ally _ally = null!;
    // Variable for activating/deactivating the intro scene
    [Export] private bool _enableIntroScene = true;

    // Dialog text lines
    private readonly List<string> _dialogLines =
    [
        "Wake up!",
        "Please! Wake up!",
        "We need you... Wake up!",
        "You are finally awake! I was losing hope...",
        "Something terrible has happened! All the light has disappeared.",
        "The darkness is dangerous and harmful. We can’t endure it for long...",
        "You are our last hope!",
        "I sense that there is something in the nearby village...",
        "I know you can’t move outside the core...",
        "But your allies can be your hands, eyes, and ears in this world. You are connected.",
        "You may have forgotten everything, but they are your faithful companions and will listen to your command."
    ];

    // Camera positions for specific lines
    private readonly Dictionary<int, Vector2> _cameraPositions = new Dictionary<int, Vector2>
    {
        { 7, new Vector2(5800, -6300) }, // Camera moves to position 1 on the 7th line
		{ 9, new Vector2(0, 0) }         // Camera moves back to default on the 9th line
	};

    // Typing effect variables
    private int _currentLineIndex = 0; // Index of the current dialog line
    private string _currentText = ""; // Gradually built text for the typing effect
    private bool _isTyping = false; // Indicates if the typing effect is in progress
    private Timer? _typingTimer; // Timer to control the typing speed
    private float _typingSpeed = 0.05f; // Seconds per character

    // "Waking up" effect state
    private int _wakeUpState = 0; // 0 = fully black, 1 = partially visible, 2 = fully black again, 3 = fully visible

    // Additional variables for camera shake
    private Timer? _shakeTimer; // Timer for controlling camera shake
    private int _shakeDuration = 30; // Number of shake movements
    private float _shakeIntensity = 50.0f; // Intensity of the shake
    private Control _button = null!;
    private RichTextLabel _ally1ResponseField = null!;
    private Chat _ally1Chat = null!;
    private RichTextLabel _ally2ResponseField = null!;
    private AudioStreamPlayer _introMusic = null!;

    public override void _Ready()
    {
        _introMusic = GetParent().GetNode<AudioStreamPlayer>("AudioManager/intro_music");
        _ally1ResponseField = GetTree().Root.GetNode<RichTextLabel>("Node2D/Ally/ResponseField");
        _ally2ResponseField = GetTree().Root.GetNode<RichTextLabel>("Node2D/Ally2/ResponseField");
        _ally1Chat = GetTree().Root.GetNode<Chat>("Node2D/Ally/Ally1Cam/Ally1Chat");
        _ally1ResponseField.Visible = false;
        _ally2ResponseField.Visible = false;
        _ally1Chat.Visible = false;
        _button = GetTree().Root.GetNode<Control>("Node2D/UI");
        _button.Visible = false;
        _mainCamera = _ally.GetNode<Camera2D>("Ally1Cam");

        // Szene überspringen, wenn sie deaktiviert ist
        if (!_enableIntroScene)
        {
            _panelContainer = GetNodeOrNull<PanelContainer>("PanelContainer");
            _label = _panelContainer?.GetNodeOrNull<Label>("Label");
            _blackoutRect = GetNodeOrNull<ColorRect>("ColorRect");
            DisableIntroElements();
            _currentLineIndex = _dialogLines.Count - 1;

        }

        // Find the PanelContainer and Label for the text box
        _panelContainer = GetNode<PanelContainer>("PanelContainer");
        if (_panelContainer == null)
        {
            GD.PrintErr("PanelContainer not found!");
        }

        _label = _panelContainer?.GetNodeOrNull<Label>("Label");
        if (_label == null)
        {
            GD.PrintErr("Label not found!");
        }

        // Find the ColorRect for the blackout effect
        _blackoutRect = GetNode<ColorRect>("ColorRect");
        if (_blackoutRect == null)
        {
            GD.PrintErr("Blackout ColorRect not found!");
        }
        else
        {
            _blackoutRect.MouseFilter = Control.MouseFilterEnum.Ignore; // Ensure it does not block input
            _blackoutRect.Color = new Color(0, 0, 0, 1); // Set to fully black
        }

        // Initialize the typing timer
        _typingTimer = new Timer();
        _typingTimer.WaitTime = _typingSpeed;
        _typingTimer.OneShot = false;
        _typingTimer.Connect("timeout", new Callable(this, nameof(OnTypingTimerTimeout)));
        AddChild(_typingTimer);

        // Initialize the shake timer
        _shakeTimer = new Timer();
        _shakeTimer.WaitTime = 0.1f; // Time between each shake movement
        _shakeTimer.OneShot = false; // Repeat until shake is done
        _shakeTimer.Connect("timeout", new Callable(this, nameof(OnShakeTimerTimeout)));
        AddChild(_shakeTimer);

        // Pause the game initially
        GetTree().Paused = true;

        _introMusic.Play();

        // Display the first line of dialog
        ShowCurrentLine();

    }
    private void DisableIntroElements()
    {
        // Verstecke den PanelContainer und seine Kinder
        if (_panelContainer != null)
        {
            _panelContainer.Visible = false;
        }

        // Verstecke das Label (falls benötigt, redundant, da es im Container ist)
        if (_label != null)
        {
            _label.Visible = false;
        }

        // Verstecke den Blackout ColorRect
        if (_blackoutRect != null)
        {
            _blackoutRect.Visible = false;
        }
    }


    public override void _Process(double delta)
    {
        // Adjust the position of the text box to follow the camera
        if (_mainCamera != null && _panelContainer != null)
        {
            Vector2 cameraScreenPosition = _mainCamera.GlobalPosition;
            _panelContainer.GlobalPosition = cameraScreenPosition + new Vector2(-900, 200); // Offset to position the text box
        }
        if (_mainCamera == null)
        {
            GD.PrintErr("Main camera not found");
        }
    }

    private void ShowCurrentLine()
    {
        if (_label != null)
        {
            _currentText = ""; // Reset the text
            _label.Text = ""; // Clear the label
            _isTyping = true; // Activate the typing effect
            _typingTimer?.Start(); // Start the timer

            // Change camera position if needed
            if (_mainCamera != null && _cameraPositions.ContainsKey(_currentLineIndex))
            {
                _mainCamera.Position = _cameraPositions[_currentLineIndex];
            }
        }
    }

    private void OnTypingTimerTimeout()
    {
        if (_label != null)
        {
            if (_currentLineIndex < 0 || _currentLineIndex >= _dialogLines.Count)
            {
                GD.PrintErr($"FEHLER: Index {_currentLineIndex} ist außerhalb des gültigen Bereichs (0 bis {_dialogLines.Count - 1}).");
                _typingTimer?.Stop();
                return;
            }

            string fullText = _dialogLines[_currentLineIndex];

            // Append the next character
            if (_currentText.Length < fullText.Length)
            {
                _currentText += fullText[_currentText.Length];
                _label.Text = _currentText; // Update the label
            }
            else
            {
                // Stop the timer when the entire text is displayed
                _typingTimer?.Stop();
                _isTyping = false; // Deactivate the typing effect
            }
        }
    }


    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept") ||
            (@event is InputEventMouseButton clickEvent && clickEvent.ButtonIndex == MouseButton.Left && clickEvent.Pressed))
        {
            if (_wakeUpState == 0)
            {
                SetBlackoutAlpha(0.9f); // Adjust this value for partial visibility
                _wakeUpState = 1;
                ShowNextLine();
                return;
            }
            else if (_wakeUpState == 1)
            {
                SetBlackoutAlpha(1.0f); // Fully black
                _wakeUpState = 2;
                ShowNextLine();
                return;
            }
            else if (_wakeUpState == 2)
            {
                SetBlackoutAlpha(0.0f); // Fully visible
                _wakeUpState = 3;

                // Start camera shake when waking up
                StartCameraShake();

                ShowNextLine();
                return;
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    private void ShowNextLine()
    {
        if (_currentLineIndex >= _dialogLines.Count - 1)
        {
            FinishDialog();
            return;
        }

        _currentLineIndex++;
        ShowCurrentLine();
    }


    private void FinishDialog()
    {
        if (_panelContainer != null)
        {
            _panelContainer.Visible = false; // Hide the text box
        }

        // Reset the camera to follow the player
        if (_mainCamera != null)
        {
            _mainCamera.GlobalPosition = _ally.GlobalPosition; // Reset to the parent node's position
            _mainCamera.MakeCurrent(); // Reactivate camera follow
        }

        // Reactivate the MouseFilter for buttons
        foreach (Node? button in GetTree().GetNodesInGroup("UIButtons"))
        {
            if (button is Button uiButton)
            {
                uiButton.MouseFilter = Control.MouseFilterEnum.Stop;
            }
        }
        _button.Visible = true;
        _ally1ResponseField.Visible = true;
        _ally2ResponseField.Visible = false;
        _ally1Chat.Visible = true;

        // Resume the game
        GetTree().Paused = false;
        _introMusic.Stop();

        // **Tutorial starten**
        Node2D rootNode = GetTree().Root.GetNode<Node2D>("Node2D"); // `Node2D` in ExampleScene
        if (rootNode != null)
        {
            Tutorial tutorial = rootNode.GetNodeOrNull<Tutorial>("Tutorial");
            if (tutorial != null)
            {
                tutorial.StartTutorial();
            }
            else
            {
                GD.PrintErr("Tutorial wurde nicht gefunden! Stelle sicher, dass es in Node2D existiert.");
            }
        }
        else
        {
            GD.PrintErr("Node2D wurde nicht gefunden! Ist ExampleScene wirklich aktiv?");
        }
    }


    private void SetBlackoutAlpha(float alpha)
    {
        if (_blackoutRect != null)
        {
            Color currentColor = _blackoutRect.Color;
            _blackoutRect.Color = new Color(currentColor.R8 / 255.0f, currentColor.G8 / 255.0f, currentColor.B8 / 255.0f, alpha); // Create a new color with updated alpha
        }
    }

    private void StartCameraShake()
    {
        // Reset shake parameters and start the timer
        _shakeDuration = 10; // Reset the shake duration
        _shakeIntensity = 20.0f; // Adjust intensity if needed
        _shakeTimer?.Start();
    }

    private void OnShakeTimerTimeout()
    {
        if (_mainCamera != null && _shakeDuration > 0)
        {
            // Apply a small random offset to the camera position
            Vector2 randomOffset = new Vector2(
                GD.Randf() * _shakeIntensity - _shakeIntensity / 2,
                GD.Randf() * _shakeIntensity - _shakeIntensity / 2
            );
            _mainCamera.Offset = randomOffset;

            _shakeDuration--;
        }
        else
        {
            // Stop shaking and reset camera offset
            _shakeTimer.Stop();
            if (_mainCamera != null)
            {
                _mainCamera.Offset = Vector2.Zero;
            }
        }
    }
}
