using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    private List<Item> itemList;
    public Inventory() 
    {
        itemList = new List<Item>();
        // AddItem(new Item{itemType = Item.ItemType.Yeti});
        // AddItem(new Item{itemType = Item.ItemType.Chick});
        Debug.Log("Inventario");
    }

    public void AddItem(Item item)
    {
        itemList.Add(item);
    }

    public List<Item> GetItemList() 
    {
        return itemList;
    }
}
