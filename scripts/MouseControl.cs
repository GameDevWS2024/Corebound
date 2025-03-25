using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Game.Scripts;

using Godot;

public partial class MouseControl : Control
{
    // The RichTextLabel that will follow the mouse
    private RichTextLabel _richTextLabel = null!;
    private CharacterBody2D _selectedEntityGoto = null!;
    private CharacterBody2D _selectedEntityChat = null!;
    private int _clickRadius = 50;
    Node _pathFindingMovement = null!;
    public int CurrentCamera = 2;
    public Camera2D Camera1 = null!;
    public Camera2D Camera2 = null!;
    public Chat Chat1 = null!;
    public Chat Chat2 = null!;
    public Chat Chat = null!;
    [Export] public Ally Ally1 = null!;
    [Export] public Ally Ally2 = null!;

    public override void _Ready()
    {
        _selectedEntityGoto = Ally1;
        // Get the RichTextLabel node
        Camera1 = GetParent().GetNode<Camera2D>("Ally/Ally1Cam");
        Camera2 = GetParent().GetNode<Camera2D>("Ally2/Ally2Cam");
        Chat1 = Camera1.GetNode<Chat>("Ally1Chat");
        Chat2 = Camera2.GetNode<Chat>("Ally2Chat");
        _richTextLabel = GetNode<RichTextLabel>("%MouseLabel");
        _richTextLabel.Visible = true;
        _pathFindingMovement = GetNode<PathFindingMovement>("PathFindingMovementNode");
    }

    public override void _Input(InputEvent @event)
    {
        // Check if the event is a mouse button event and if it's the left button and select entity for goto
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            EntityGoto(GetGlobalMousePosition());
            Chat1.ReleaseFocus();
            Chat2.ReleaseFocus();
        }

        // Check if the event is a mouse button event and if it's the right button and open chat up
        /*else if (@event is InputEventMouseButton mouseEvent1 && mouseEvent1.Pressed && mouseEvent1.ButtonIndex == MouseButton.Right)
		{
			ChatPopUp(GetGlobalMousePosition());
		}*/
        else if (@event is InputEventMouseButton mouseEvent1 && mouseEvent1.Pressed && mouseEvent1.ButtonIndex == MouseButton.Right)
        {
            //SwitchCamera();
        }
    }

    private void SwitchCamera()
    {
        GD.Print("DEBUG: Switching camera");
        if (CurrentCamera == 1)
        {
            _selectedEntityGoto = Ally2;
            Camera2.Enabled = true;
            Camera1.Enabled = false;
            Camera1.GetNode<Chat>("Ally1Chat").Visible = false;
            Camera2.GetNode<Chat>("Ally2Chat").GrabFocus();
            Camera2.GetNode<Chat>("Ally2Chat").Visible = true;
            CurrentCamera = 2;
        }
        else if (CurrentCamera == 2)
        {
            _selectedEntityGoto = Ally1;
            Camera1.Enabled = true;
            Camera2.Enabled = false;
            Camera1.GetNode<Chat>("Ally1Chat").Visible = true;
            Camera1.GetNode<Chat>("Ally1Chat").GrabFocus();
            Camera2.GetNode<Chat>("Ally2Chat").Visible = false;
            CurrentCamera = 1;
        }
    }

    private Tuple<CharacterBody2D, float> SearchNearestEntity(Vector2 clickPosition)
    {
        CharacterBody2D nearestEntity = null;
        float nearestDistance = float.MaxValue;

        List<CharacterBody2D> entityGroup = GetTree().GetNodesInGroup("Entities").OfType<CharacterBody2D>().ToList();

        foreach (CharacterBody2D entity in entityGroup)
        {
            float distance = entity.GlobalPosition.DistanceTo(clickPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEntity = entity;
            }
        }
        return Tuple.Create(nearestEntity, nearestDistance);
    }
    //chat pop up function on right click
    private void ChatPopUp(Vector2 clickPosition)
    {
        Tuple<CharacterBody2D, float> resultTuple = SearchNearestEntity(clickPosition);
        CharacterBody2D nearestEntity = resultTuple.Item1;
        float nearestDistance = resultTuple.Item2;

        // select nearest entity and set coordinates visible
        if (nearestDistance <= _clickRadius && _selectedEntityChat == null)
        {
            _selectedEntityChat = nearestEntity;
            //check what type the entity is and focus on the text box
            if (_selectedEntityChat is Ally ally)
            {
                ally.Chat.Visible = true;
                ally.Chat.GrabFocus();
            }
        }
        //if mouseclick was outside of defined radius
        else
        {
            //check type to send ally or combat ally to selected coordinates 
            if (_selectedEntityChat is Ally ally)
            {
                ally.Chat.Visible = false;
            }
            _selectedEntityChat = null;
        }
    }

    //Select nearest Entity to mouseclick
    private void EntityGoto(Vector2 clickPosition)
    {
        /*
		Tuple<CharacterBody2D, float> resultTuple = SearchNearestEntity(clickPosition);
		CharacterBody2D nearestEntity = resultTuple.Item1;
		float nearestDistance = resultTuple.Item2;

		// select nearest entity and set coordinates visible
		if (nearestDistance <= _clickRadius)
		{
			_richTextLabel.Visible = true;
			_selectedEntityGoto = nearestEntity;
		}
		*/
        //if mouseclick was outside of defined radius
        if (_selectedEntityGoto != null)
        {
            //check type to send ally or ally2 to selected coordinates 
            if (_selectedEntityGoto == Ally1)
            {
                Ally1.PathFindingMovement.TargetPosition = clickPosition;
            }
            else if (_selectedEntityGoto == Ally2)
            {
                Ally2.PathFindingMovement.TargetPosition = clickPosition;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Set the position of the RichTextLabel to the mouse position
        Vector2 mousePosition = GetGlobalMousePosition();
        _richTextLabel.Text = "x: " + (int)mousePosition.X + " y: " + (int)mousePosition.Y;
        _richTextLabel.Position = mousePosition;
    }
}
