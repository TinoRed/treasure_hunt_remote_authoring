using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableDisableMenu : MonoBehaviour
{
    public GameObject UI_Inventory;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void WhenButtonClicked()
    {
        if (UI_Inventory.activeInHierarchy) 
            UI_Inventory.SetActive(false);
        else 
            UI_Inventory.SetActive(true);

    }
}
