using System.Threading.Tasks;

using Game.Scenes.Levels;
using Game.Scripts;

using Godot;
namespace Game.Scenes.Levels;

public partial class ButtonControl : Control
{
    [Export] private Camera2D _camera1 = null!;
    [Export] private Camera2D _camera2 = null!;
    [Export] private Chat _ally1Chat = null!;
    [Export] private Chat _ally2Chat = null!;
    [Export] private CharacterBody2D _ally1 = null!;
    [Export] private CharacterBody2D _ally2 = null!;
    [Export] private Button _buttonAlly1 = null!;
    [Export] private Button _buttonAlly2 = null!;
    [Export] private PathFindingMovement _ally1Pathfinder = null!;
    [Export] private PathFindingMovement _ally2Pathfinder = null!;
    [Export] private RichTextLabel _ally1ResponseField = null!;
    [Export] private RichTextLabel _ally2ResponseField = null!;

    public int CurrentCamera = 1; // Tracks which camera is active (1 = _ally1, 2 = _ally2)
    private CharacterBody2D? _activeCharacter = null; // currently active character
    private PathFindingMovement? _activePathfinder = null; // The active character's pathfinding logic
    private Vector2 _targetPosition; // Target position for movement
    private float _moveSpeed = 150f; // Movement speed
    private bool _isMouseOverUi = false; // Tracks if the mouse is hovering over the UI
    private bool _isManualMovement = false; // Flag for manual movement
    private bool _stopPathfinding = false; // Flag to stop pathfinding

    public override void _Ready()
    {
        base._Ready();

        // Connect the response signal from both chat instances to handle responses
        _ally1Chat.Connect("ResponseReceived", new Callable(this, nameof(HandleResponse)));
        _ally2Chat.Connect("ResponseReceived", new Callable(this, nameof(HandleResponse)));

        // Automatically assign pathfinding components to characters
        _ally1Pathfinder = _ally1.GetNode<PathFindingMovement>("PathFindingMovement");
        _ally2Pathfinder = _ally2.GetNode<PathFindingMovement>("PathFindingMovement");

        // Connect button signals to switch between characters
        _buttonAlly1.Pressed += OnButtonAlly1Pressed;
        _buttonAlly2.Pressed += OnButtonAlly2Pressed;

        // Connect mouse signals for UI elements
        _ally1Chat.Connect("mouse_entered", new Callable(this, nameof(OnMouseEnteredUI)));
        _ally1Chat.Connect("mouse_exited", new Callable(this, nameof(OnMouseExitedUI)));
        _ally2Chat.Connect("mouse_entered", new Callable(this, nameof(OnMouseEnteredUI)));
        _ally2Chat.Connect("mouse_exited", new Callable(this, nameof(OnMouseExitedUI)));

        // Activate _ally1 by default
        SwitchToAlly(1);
        _ally2Chat.Visible = false;
        _ally2ResponseField.Visible = false;
    }

    private void OnMouseEnteredUI()
    {
        _isMouseOverUi = true;
    }

    private void OnMouseExitedUI()
    {
        _isMouseOverUi = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Synchronize UI positions with the camera
        Vector2 cameraPosition = (CurrentCamera == 1) ? _camera1.GlobalPosition : _camera2.GlobalPosition;

        _ally1Chat.GlobalPosition = cameraPosition + new Vector2(519, 168);
        _ally2Chat.GlobalPosition = cameraPosition + new Vector2(519, 168);

        _buttonAlly1.GlobalPosition = cameraPosition + new Vector2(400, 300);
        _buttonAlly2.GlobalPosition = cameraPosition + new Vector2(600, 300);

        _ally1ResponseField.GlobalPosition = cameraPosition + new Vector2(519, 280);
        _ally2ResponseField.GlobalPosition = cameraPosition + new Vector2(519, 280);

        UpdateButtonPositions();

        // Handle manual movement if active
        if (_activeCharacter != null && _isManualMovement)
        {
            MoveManually(delta);
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Ignore input if the mouse is over the UI
        if (_isMouseOverUi)
        {
            return;
        }

        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Right)
            {
                // Switch to the other character on right-click
                int nextAlly = _activeCharacter == _ally1 ? 2 : 1;
                SwitchToAlly(nextAlly);
                return;
            }

            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                // Get the global mouse position
                Vector2 mousePosition = GetGlobalMousePosition();

                // Ignore clicks on buttons
                if (_buttonAlly1.GetGlobalRect().HasPoint(mousePosition) ||
                    _buttonAlly2.GetGlobalRect().HasPoint(mousePosition))
                {
                    return;
                }

                // Set the target position for the active character
                if (_activeCharacter != null && _activePathfinder != null)
                {
                    _targetPosition = mousePosition;
                    _activePathfinder.TargetPosition = mousePosition;
                    _isManualMovement = true;
                }
            }
        }
    }

    private void MoveManually(double delta)
    {
        if (_activeCharacter != null)
        {
            // Move the character toward the target position
            if (_activeCharacter.GlobalPosition != _targetPosition)
            {
                Vector2 direction = (_targetPosition - _activeCharacter.GlobalPosition).Normalized();
                _activeCharacter.GlobalPosition += direction * _moveSpeed * (float)delta;

                // Stop movement when the target is reached
                if (_activeCharacter.GlobalPosition.DistanceTo(_targetPosition) < 5f)
                {
                    _activeCharacter.GlobalPosition = _targetPosition;
                    _isManualMovement = false;
                    _stopPathfinding = false;
                }
            }
        }
    }

    private void OnButtonAlly1Pressed()
    {
        SwitchToAlly(1);
    }

    private void OnButtonAlly2Pressed()
    {
        SwitchToAlly(2);
    }

    private void SwitchToAlly(int allyNumber)
    {
        if (allyNumber == 1)
        {
            _camera1.Enabled = true; // Activate _ally1's camera
            _camera2.Enabled = false; // Deactivate _ally2's camera

            _ally1Chat.Visible = true;
            _ally2Chat.Visible = false;

            _ally1ResponseField.Visible = true;
            _ally2ResponseField.Visible = false;

            _activeCharacter = _ally1;
            _activePathfinder = _ally1Pathfinder;
            CurrentCamera = 1;

            // Update button states
            _buttonAlly1.SetPressedNoSignal(true);
            _buttonAlly2.SetPressedNoSignal(false);
        }
        else if (allyNumber == 2)
        {
            _camera1.Enabled = false; // Activate _ally2's camera
            _camera2.Enabled = true; // Deactivate _ally1's camera

            _ally1Chat.Visible = false;
            _ally2Chat.Visible = true;

            _ally1ResponseField.Visible = false;
            _ally2ResponseField.Visible = true;

            _activeCharacter = _ally2;
            _activePathfinder = _ally2Pathfinder;
            CurrentCamera = 2;

            // Update button states
            _buttonAlly1.SetPressedNoSignal(false);
            _buttonAlly2.SetPressedNoSignal(true);
        }

        UpdateButtonPositions();
    }

    private void UpdateButtonPositions()
    {
        // Update button positions relative to the active chat's position
        Chat activeChat = CurrentCamera == 1 ? _ally1Chat : _ally2Chat;
        Vector2 chatGlobalPosition = activeChat.GlobalPosition;

        _buttonAlly1.GlobalPosition = chatGlobalPosition - new Vector2(0, 80);
        _buttonAlly2.GlobalPosition = chatGlobalPosition + new Vector2(150, -80);
    }

    public async Task TypeWriterEffect(string fullText, RichTextLabel label, float delay = 0.008f)
    {
        // Display text in a typewriter effect
        label.Text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            label.Text += fullText[i];
            await ToSignal(GetTree().CreateTimer(delay), "timeout");
        }
    }

    private async void DisplayResponse(string response, int allyNumber)
    {
        // Display the response in the correct response field
        RichTextLabel label = (allyNumber == 1) ? _ally1ResponseField : _ally2ResponseField;
        await TypeWriterEffect(response, label);
    }

    private void HandleResponse(string response, object ally)
    {
        // Handle the response received from the chat
        int activeAlly = CurrentCamera;
        DisplayResponse(response, activeAlly);
    }
}
