using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    public Text nameField;
    public Text descriptionField;
    public Transform displayItem;

    public void DisplayData(ItemTemplate it)
    {
        for (int i = 0; i < displayItem.childCount; i++)
        {
            if (displayItem.GetChild(i).GetComponent<InteractableItem>().item == it)
            {
                displayItem.GetChild(i).gameObject.SetActive(true);
            }
            else
                displayItem.GetChild(i).gameObject.SetActive(false);
        }

        nameField.text = it.itemName;
        descriptionField.text = it.itemDescription;
    }
}
