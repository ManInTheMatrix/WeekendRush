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
    public OVRPlayerController ovrPlayerScript;
    public Transform rayOrigin;
    public Inventory inventoryScript;

    [Header("VR Hand Grabbers")]
    public VRHandGrabber leftGrabber;
    public VRHandGrabber rightGrabber;

    public Camera mainCam;
    RaycastHit hit;
    bool isItemInView = false;

    void Start()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
        }
    }

    private void Update()
    {
        bool closeKey = false;
        bool viewKey = false;
        bool pickKey = false;
        bool interactKey = false;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            closeKey = kb.cKey.wasPressedThisFrame;
            viewKey = kb.vKey.wasPressedThisFrame;
            pickKey = kb.pKey.wasPressedThisFrame;
            interactKey = kb.eKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        closeKey = Input.GetKeyDown(KeyCode.C);
        viewKey = Input.GetKeyDown(KeyCode.V);
        pickKey = Input.GetKeyDown(KeyCode.P);
        interactKey = Input.GetKeyDown(KeyCode.E);
#endif

        bool closeInput = closeKey ||
                          OVRInput.GetDown(OVRInput.Button.One) ||
                          OVRInput.GetDown(OVRInput.Button.Two) ||
                          OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);

        if (hit.collider != null && !isItemInView)
        {
            InteractableItem i = hit.collider.GetComponentInParent<InteractableItem>();

            if (i != null && i.item != null)
            {
                bool viewInput = viewKey ||
                                 OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch) ||
                                 OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

                // Right controller pick
                bool pickRightInput = pickKey ||
                                      OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch) ||
                                      OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

                // Left controller pick
                bool pickLeftInput = OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.LTouch) ||
                                     OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);

                bool interactInput = interactKey ||
                                     OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) ||
                                     OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

                if (viewInput && i.item.isViewable)
                {
                    isItemInView = true;
                    if (fpsScript != null) fpsScript.enabled = false;
                    if (ovrPlayerScript != null)
                    {
                        ovrPlayerScript.EnableLinearMovement = false;
                        ovrPlayerScript.EnableRotation = false;
                    }
                    SwitchMenuItems(false, null);
                    if (secondCamera != null) secondCamera.SetActive(true);
                    if (itemDisplay != null)
                    {
                        itemDisplay.gameObject.SetActive(true);
                        itemDisplay.DisplayData(i.item);
                    }
                }
                else if (i.item.isPickable && (pickRightInput || pickLeftInput))
                {
                    // Pick into the hand that pressed the button
                    if (pickLeftInput && leftGrabber != null)
                    {
                        leftGrabber.GrabItem(i);
                    }
                    else if (rightGrabber != null)
                    {
                        rightGrabber.GrabItem(i);
                    }
                    else if (inventoryScript != null)
                    {
                        inventoryScript.ItemPicked(i);
                    }
                }
                else if (interactInput && i.item.isInteractable)
                {
                    if (inventoryScript != null) inventoryScript.InteractWithItem(i);
                }
            }
        }
        else
        {
            SwitchMenuItems(false, null);
        }

        if (isItemInView && closeInput)
        {
            isItemInView = false;
            if (fpsScript != null) fpsScript.enabled = true;
            if (ovrPlayerScript != null)
            {
                ovrPlayerScript.EnableLinearMovement = true;
                ovrPlayerScript.EnableRotation = true;
            }
            if (secondCamera != null) secondCamera.SetActive(false);
            if (itemDisplay != null) itemDisplay.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        bool hasHit = false;

        if (rayOrigin != null)
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            hasHit = Physics.Raycast(ray, out hit, 5f);
        }

        if (!hasHit && mainCam != null)
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            hasHit = Physics.Raycast(ray, out hit, 5f);
        }

        if (hasHit && !isItemInView && hit.collider != null)
        {
            InteractableItem i = hit.collider.GetComponentInParent<InteractableItem>();
            if (i != null)
            {
                SwitchMenuItems(true, i);
            }
            else
            {
                SwitchMenuItems(false, null);
            }
        }
        else if (!isItemInView)
        {
            SwitchMenuItems(false, null);
        }
    }

    void SwitchMenuItems(bool flag, InteractableItem i) 
    {
        // Screen-space menu items are disabled in favor of world-space floating tooltips directly on the items
        if (interact != null) interact.SetActive(false);
        if (pick != null) pick.SetActive(false);
        if (view != null) view.SetActive(false);
    }
}
