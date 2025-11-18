using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public Image image;
    public Text message;
    public Sprite defaultImage;
    public InteractableItem inventoryItem;

    public ItemTemplate torchTemplate;
    public GameObject torch;

    public void ItemPicked(InteractableItem it)
    {
        if (it.item == torchTemplate)
        {
            torch.SetActive(true);
            it.gameObject.SetActive(false);
            Debug.Log("torch");
            return;
        }
        else
        {
            Debug.Log(it.item.name);
            if (inventoryItem != null)
            {
                inventoryItem.gameObject.SetActive(true);
            }

            inventoryItem = it;
            image.sprite = it.item.itemImage;
            it.gameObject.SetActive(false);
        }
    }

    public void InteractWithItem(InteractableItem it)
    {
        Debug.Log("interacting with " + it.item.name);
        
        if(inventoryItem != null)
        {
            if (it.FindMissingPiece(inventoryItem.item))
            {
                if (it.hasMissingPiece)
                {
                    inventoryItem = null;
                    image.sprite = defaultImage;
                }
            }
        }

        if (!it.hasMissingPiece)
            message.text = it.item.itemInteractFail;
        else
        {
            message.text = it.item.itemInteractSuccess;
            it.PerformAction();
        }
    }
}
