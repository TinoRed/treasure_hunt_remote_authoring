using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableDisableMenu : MonoBehaviour
{
    public GameObject UI_Inventory;
    public GameObject panel;
    public GameObject footer;
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
        {
            panel.SetActive(true);
            footer.SetActive(true);
            UI_Inventory.SetActive(false);
        }
            
        else 
        {
            panel.SetActive(false);
            footer.SetActive(false);
            UI_Inventory.SetActive(true);
        }
    }
}
