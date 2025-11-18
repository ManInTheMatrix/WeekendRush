using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableItem : MonoBehaviour
{
    public ItemTemplate item;
    public bool hasMissingPiece = false;
    public ItemTemplate missingPiece;
    public UnityEvent events;

    public bool FindMissingPiece(ItemTemplate it)
    {
        if(missingPiece != null)
        {
            hasMissingPiece = (it == missingPiece);
            return true;
        }
        return false;
    }

    public void PerformAction()
    {
        events.Invoke();
    }
    public void ChangeMissingPiece(bool flag)
    {
        hasMissingPiece = flag;
    }
}
