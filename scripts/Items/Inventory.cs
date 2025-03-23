using System;
using System.Collections.Generic;
using System.Linq;

using Godot;
namespace Game.Scripts.Items;

public partial class Inventory
{
    public int Size { get; }
    private Itemstack?[] Items { get; set; }

    public Inventory(int size)
    {
        Size = size;
        Items = new Itemstack?[size];
        for (int i = 0; i < size; i++)
        {
            Items[i] = new Itemstack(Material.None);
        }
        Print();
    }
    public override string ToString()
    {
        string inv = "Inventory: [";

        for (int i = 0; i < Size; i++)
        {
            inv += Items[i].Material + " : " + Items[i].Amount + ", ";
        }

        if (inv.EndsWith(", "))
        {
            inv = inv.Remove(inv.Length - 2); // Remove trailing comma and space
        }

        inv += "]";
        return inv;
    }

    public void Print()
    {
        GD.Print();
        GD.Print("--------------Inventory--------------");
        GD.Print(ToString());
        GD.Print();
    }

    public Itemstack GetItem(int i)
    {
        if (i < 0 || i > Size)
        {
            return new Itemstack(Material.None);
        }
        return Items[i]!;
    }

    public bool FitItem(Itemstack stack)
    {
        Material mat = stack.Material;
        for (int i = 0; i < Size; i++)
        {
            if (Items[i]!.Material == Material.None) { return true; }
            if (Items[i]!.Material == mat && Items[i]!.Amount < 64) { return true; }
        }
        return false;
    }

    public bool HasSpace()
    {
        foreach (Itemstack? item in Items)
        {
            if (item!.Material == Material.None)
            {
                return true;
            }
        }

        return false;
    }

    public bool ContainsMaterial(Material mat)
    {
        foreach (Itemstack item in Items)
        {
            if (item!.Material == mat)
            {
                return true;
            }
        }
        return false;
    }

    public void AddItem(Itemstack itemstack)
    {
        //GD.Print(itemstack.Material.ToString() + " with amount:"+ itemstack.Amount +"added to Inventory");
        if (itemstack.Amount == 0)
        {
            GD.Print("[Inventory.cs] Did not add item " + itemstack.Material.ToString() + " to inventory because Amount was 0");
            return;
        }

        int none = -1;
        for (int i = 0; i < Size; i++)
        {

            if (Items[i]!.Material == itemstack.Material && Items[i]!.Stackable && Items[i]!.Amount < 64 && itemstack.Stackable)
            {
                if (itemstack.Material == Material.Notebook) { GD.Print("Notebook!"); }
                int space = 64 - Items[i]!.Amount;
                if (itemstack.Amount <= space)
                {
                    Items[i]!.Amount += itemstack.Amount;
                    itemstack.Amount = 0;
                }
                else
                {
                    Items[i]!.Amount += space;
                    itemstack.Amount -= space;
                    AddItem(itemstack); // recursively fill the inventory until stack is empty or inventory is full.
                }

                return;
            }

            if (Items[i]!.Material == Material.None && none == -1)
            {
                none = i;
            }
        }
        if (none != -1)
        {
            if (itemstack.Amount <= 64)
            {
                Items[none] = new Itemstack(itemstack.Material, itemstack.Amount, itemstack.Stackable);
                itemstack.Amount = 0;
            }
            else
            {
                Items[none] = new Itemstack(itemstack.Material, 64, itemstack.Stackable);
                itemstack.Amount -= 64;
                AddItem(itemstack); // recursively fill the inventory until stack is empty or inventory is full.
            }
        }
    }

    public void SetItem(Itemstack? itemstack, int i)
    {
        if (i < 0 || i >= Size)
        {
            throw new Exception($"Index {i} is out of bounds for inventory size {Size}");
        }

        Items[i] = itemstack;
    }

    public Itemstack? ExtractItem(int i)
    {
        if (i < 0 || i >= Size)
        {
            GD.PrintErr($"Index {i} is out of bounds for inventory size {Size}");
            return null;
        }
        Itemstack? rtrn = Items[i];
        Items[i] = new Itemstack(Material.None);
        return rtrn;
    }

    public void DeleteItem(int i)
    {
        if (i < 0 || i >= Size)
        {
            throw new Exception($"Index {i} is out of bounds for inventory size {Size}");
        }

        Items[i] = new Itemstack(Material.None);
    }

    public void SwapItems(int i, int j)
    {
        if (i < 0 || i >= Size || j < 0 || j >= Size)
        {
            throw new Exception($"Index {i} is out of bounds for inventory size {Size}");
        }

        (Items[i], Items[j]) = (Items[j], Items[i]);
    }

    public void HardSwapItems(Material originalMat, Material newMat)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i]!.Material == originalMat)
            {
                Items[i]!.SetMaterial(newMat);
                return;
            }
        }
    }

    public int GetTotalItemCount()
    {
        int totalCount = 0;
        for (int i = 0; i < Size; i++)
        {
            if (Items[i]!.Material != Material.None)
            {
                totalCount += Items[i]!.Amount; // Add the number of items in each slot
                                                // GD.Print(Items[i].Material);
            }
        }
        //GD.Print(totalCount);
        return totalCount;
    }

    public void Clear()
    {
        for (int i = 0; i < Size; i++)
        {
            Items[i] = new Itemstack(Material.None);
        }
    }

    public Itemstack[] GetItems()
    {
        return Items!;
    }
}
