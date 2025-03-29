using System;
using System.Collections.Generic;
using System.Linq;

using Game.Scripts;
using Game.Scripts.Items;

using Godot;

using Material = Game.Scripts.Items.Material;

namespace Game.Scripts.Interaction;

[GlobalClass]
public partial class ItemAdder : Node2D
{
    [Export] public String? ItemToAddName { get; set; }
    private AudioStreamPlayer _pickUpSound = null!;
    public Material ItemToAdd { get; set; }
    [Export] public int Amount { get; set; }
    [Export] public bool ListenToInteract { get; set; } = true;

    public override void _Ready()
    {
        _pickUpSound = GetTree().Root.GetNode<AudioStreamPlayer>("Node2D/AudioManager/new_item_sound");
        if (Enum.TryParse(ItemToAddName, out Material parsedMaterial))
        {
            ItemToAdd = parsedMaterial;
            GD.Print("item: " + ItemToAddName);
        }
        else
        {
            GD.Print("item not found");
        }
        if (ListenToInteract)
        {
            Interactable interactable = GetParent().GetNode<Interactable>("Interactable");
            if (interactable != null)
            {
                interactable.Interact += AddItem;
            }
        }
    }

    // Make this public so it can be called from anywhere
    public void AddItem()
    {
        List<CharacterBody2D> entityGroup = GetTree().GetNodesInGroup("Entities").OfType<CharacterBody2D>().ToList();
        float nearestDistance = float.MaxValue;
        Ally nearestEntity = null!;
        foreach (Ally entity in entityGroup)
        {
            float distance = entity.GlobalPosition.DistanceTo(GlobalPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEntity = entity;
            }
        }
        nearestEntity.SsInventory.AddItem(new Itemstack(ItemToAdd, Amount));
        _pickUpSound.Play();
    }
}
