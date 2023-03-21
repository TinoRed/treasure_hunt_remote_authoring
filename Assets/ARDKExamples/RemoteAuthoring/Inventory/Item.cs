using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    public enum ItemType {
        Yeti,
        Chick,
        Cube,
        Ball
    }
    public ItemType itemType;

    public Sprite GetSprite() 
    {
        switch (itemType) 
        {
            default:
            case ItemType.Yeti: return ItemAssets.Instance.yetiSprite;
            case ItemType.Ball: return ItemAssets.Instance.ballSprite;
            case ItemType.Cube: return ItemAssets.Instance.cubeSprite;
            case ItemType.Chick: return ItemAssets.Instance.chickSprite;

        }
    }
}
