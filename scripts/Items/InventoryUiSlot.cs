using System;

using Game.Scripts.Items;

using Godot;

public partial class InventoryUiSlot : Panel
{
    private Sprite2D _icon = null!;
    private Label _count = null!;
    public override void _Ready()
    {
        _icon = GetChild<CenterContainer>(1).GetChild<Panel>(0).GetChild<Sprite2D>(0);
        if (_icon == null)
        {
            GD.PrintErr("ERR: Cannot find icon <InventoryUISlot.cs>");
        }
        _count = GetChild<CenterContainer>(1).GetChild<Panel>(0).GetChild<Label>(1);
        if (_count == null)
        {
            GD.PrintErr("ERR: Cannot find icon <InventoryUISlot.cs>");
        }
    }


    public override void _Process(double delta)
    {
    }

    public void Update(Itemstack item)
    {
        switch (item.Material)
        {
            case Game.Scripts.Items.Material.None:
                _icon.Visible = false;
                return;
            case Game.Scripts.Items.Material.Diamond:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/diamond.png");
                break;
            case Game.Scripts.Items.Material.Iron:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/iron.png");
                break;
            case Game.Scripts.Items.Material.Copper:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/copper.png");
                break;

            case Game.Scripts.Items.Material.Wood:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/wood.png");
                break;

            case Game.Scripts.Items.Material.Stone:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/stone.png");
                break;

            case Game.Scripts.Items.Material.Gold:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/gold.png");
                break;

            case Game.Scripts.Items.Material.Torch:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/torch.png");
                break;

            case Game.Scripts.Items.Material.LightedTorch:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/lighted_torch.png");
                break;

            case Game.Scripts.Items.Material.Notebook:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/notebook.png");
                break;

            case Game.Scripts.Items.Material.BucketWater:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/bucket_water.png");
                break;

            case Game.Scripts.Items.Material.BucketEmpty:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/bucket_empty.png");
                break;

            default:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/missing_texture.png");
                break;

            case Game.Scripts.Items.Material.FestiveStaff:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/festiveStaff.png");
                break;

            case Game.Scripts.Items.Material.Chipcard:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://assets/items/chip_card.png");
                break;

            case Game.Scripts.Items.Material.Key:
                _icon.Visible = true;
                _icon.Texture = GD.Load<Texture2D>("res://Key.png");
                break;
        }
        _count.Text = item.Amount <= 1 ? "" : item.Amount.ToString();
    }
}
