using System.Collections.Generic;

using Godot;

public partial class Tutorial : Control
{
    private PanelContainer? _panelContainer;
    private Label? _label;
    private Button? _nextButton;
    private VBoxContainer? _vboxContainer;
    private VideoStreamPlayer? _videoPlayer; // Video Player hinzufügen
    private int _currentStepIndex = 0;

    // Liste der Tutorial-Texte
    private readonly List<string> _tutorialSteps =
    [
        "Welcome to the tutorial!",
        "Left-click to move your allies",
        "You can use the chatbox to interact with your allies. If they find an item or find out about a location, you can tell them what to do (e.g. pick up an item or go to a location).",
        "You can switch between your allies by right-clicking or using the buttons above the text box.",
        "Good Luck!"
    ];

    // Dictionary, um jedem Tutorial-Schritt ein Video zuzuweisen
    private readonly Dictionary<string, string> _tutorialVideos = new()
    {
        { "Left-click to move your allies", "res://assets/videos/movementTutorial.ogv" },
        { "You can use the chatbox to interact with your allies. If they find an item or find out about a location, you can tell them what to do (e.g. pick up an item or go to a location).", "res://assets/videos/talktoAllyTutorial.ogv"},
        { "You can switch between your allies by right-clicking or using the buttons above the text box.", "res://assets/videos/switchAllyTutorial.ogv"},
    };

    public override void _Ready()
    {
        GetTree().Root.SetProcessInput(false);
        _panelContainer = GetNode<PanelContainer>("PanelContainer");
        _panelContainer.Visible = false;

        _vboxContainer = _panelContainer?.GetNode<VBoxContainer>("VBoxContainer");
        _label = _vboxContainer?.GetNode<Label>("Label");
        _nextButton = _vboxContainer?.GetNode<Button>("Button");
        _videoPlayer = _vboxContainer?.GetNode<VideoStreamPlayer>("VideoStreamPlayer"); // VideoPlayer holen

        if (_nextButton != null)
        {
            _nextButton.Connect("pressed", new Callable(this, nameof(OnNextButtonPressed)));
            _nextButton.MouseFilter = Control.MouseFilterEnum.Stop;
        }

        this.MouseFilter = Control.MouseFilterEnum.Stop;
        SetProcessUnhandledInput(true);
        GetViewport().GuiEmbedSubwindows = true;
        GetViewport().SetInputAsHandled();
    }

    [Export] private bool _enableTutorial = true; // Steuerung im Inspector

    public void StartTutorial()
    {
        if (!_enableTutorial)
        {
            FinishTutorial(); // Direkt beenden, falls das Tutorial deaktiviert ist
            return;
        }

        _panelContainer.Visible = true;
        _currentStepIndex = 0;
        ShowCurrentStep();
        GetTree().Paused = true;
    }

    private void ShowCurrentStep()
    {
        if (_label != null)
        {
            _label.Text = _tutorialSteps[_currentStepIndex];
        }

        // Video basierend auf dem aktuellen Schritt wechseln
        string currentText = _tutorialSteps[_currentStepIndex];

        if (_videoPlayer != null && _tutorialVideos.ContainsKey(currentText))
        {
            _videoPlayer.Stream = GD.Load<VideoStream>(_tutorialVideos[currentText]);
            _videoPlayer.Play();
        }
    }

    private void OnNextButtonPressed()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= _tutorialSteps.Count)
        {
            FinishTutorial();
        }
        else
        {
            ShowCurrentStep();
        }
    }

    private void FinishTutorial()
    {
        if (_panelContainer != null)
        {
            _panelContainer.Visible = false;
        }

        if (_videoPlayer != null)
        {
            _videoPlayer.Stop(); // Video stoppen, wenn das Tutorial vorbei ist
        }

        GetTree().Paused = false; // Spiel wieder fortsetzen
        GetTree().Root.SetProcessInput(true);
    }
}
