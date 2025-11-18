using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerEyes : MonoBehaviour
{
    public GameObject interact;
    public GameObject pick;
    public GameObject view;

    public GameObject secondCamera;
    public ItemDisplay itemDisplay;
    public FirstPersonController fpsScript;
    public Inventory inventoryScript;

    Camera mainCam;
    RaycastHit hit;
    bool isItemInView = false;

    void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (hit.collider != null && !isItemInView)
        {
            InteractableItem i = hit.collider.GetComponent<InteractableItem>();

            if (Input.GetKeyDown(KeyCode.V) && i.item.isViewable)
            {
                isItemInView = true;
                fpsScript.enabled = false;
                SwitchMenuItems(false, null);
                secondCamera.SetActive(true);
                itemDisplay.gameObject.SetActive(true);
                itemDisplay.DisplayData(i.item);
                
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                //InteractableItem i = hit.collider.GetComponent<InteractableItem>();
                if (i.item.isPickable)
                {
                    inventoryScript.ItemPicked(i);
                }
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                //InteractableItem i = hit.collider.GetComponent<InteractableItem>();
                if (i.item.isInteractable)
                {
                    inventoryScript.InteractWithItem(i);
                }
            }
        }
        else
        {
            SwitchMenuItems(false, null);
        }
        if (isItemInView && Input.GetKeyDown(KeyCode.C))
        {
            isItemInView = false;
            fpsScript.enabled = true;
            secondCamera.SetActive(false);
            itemDisplay.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        //Debug.DrawRay(ray.origin, ray.direction);
        if (Physics.Raycast(ray, out hit, 5f) && !isItemInView)
        {
            InteractableItem i = hit.collider.GetComponent<InteractableItem>();
            if (i != null)
            {
                SwitchMenuItems(true, i);
            }
            else
            {
                SwitchMenuItems(false, i);
            }
        }
        
    }

    void SwitchMenuItems(bool flag, InteractableItem i) 
    {
        if (flag)
        {
            if (i.item.isInteractable)
            {
                interact.SetActive(true);
            }
            if (i.item.isPickable)
            {
                pick.SetActive(true);
            }
            if (i.item.isViewable)
            {
                view.SetActive(true);
            }
        }
        else
        {
            interact.SetActive(false);
            pick.SetActive(false);
            view.SetActive(false);
        }
    }
}
