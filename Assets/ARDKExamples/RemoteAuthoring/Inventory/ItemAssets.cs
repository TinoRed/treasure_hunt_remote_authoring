using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemAssets : MonoBehaviour
{
    public static ItemAssets Instance { get; private set; }
    private void Awake ()
    {
        Instance = this;
    }

    public Sprite chickSprite;
    public Sprite yetiSprite;
    public Sprite cubeSprite;
    public Sprite ballSprite;
}
