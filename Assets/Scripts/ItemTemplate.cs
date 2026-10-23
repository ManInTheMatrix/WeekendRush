using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu]
public class ItemTemplate : ScriptableObject
{
    public string itemName;
    public bool isInteractable;
    public bool isPickable;
    public bool isViewable;
    public string itemDescription;
    public string nextHint;
    public Sprite itemImage;

    public string itemInteractSuccess;
    public string itemInteractFail;
}
