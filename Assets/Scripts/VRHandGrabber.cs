using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HandSide
{
    Left,
    Right
}

public class VRHandGrabber : MonoBehaviour
{
    [Header("Hand Settings")]
    public HandSide handSide = HandSide.Right;
    public Transform holdPoint;
    public float grabRadius = 0.45f;
    public float rayMaxDistance = 10f;
    public LayerMask grabMask = ~0;

    [Header("Raycast & Pull Settings")]
    public float pullSpeed = 14f;
    public Color rayIdleColor = new Color(0.2f, 0.7f, 1f, 0.4f);
    public Color rayHighlightColor = new Color(0.2f, 1f, 0.4f, 0.85f);
    public float rayWidth = 0.008f;

    [Header("Current State")]
    public InteractableItem heldItem;
    public InteractableItem pointedItem;

    [Header("Audio / Feedback")]
    public AudioSource audioSource;

    private OVRInput.Controller controllerType;
    private LineRenderer pointerLine;
    private InteractableItem pullingItem;
    private float dropCooldown = 0f;

    private void Awake()
    {
        controllerType = (handSide == HandSide.Left) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        if (holdPoint == null)
        {
            holdPoint = transform;
        }

        SetupPointerLine();
    }

    private void SetupPointerLine()
    {
        if (pointerLine == null)
        {
            GameObject lineObj = new GameObject(handSide + "_GrabPointerLine");
            lineObj.transform.SetParent(transform, false);
            pointerLine = lineObj.AddComponent<LineRenderer>();
            pointerLine.startWidth = rayWidth;
            pointerLine.endWidth = rayWidth * 0.5f;
            pointerLine.material = new Material(Shader.Find("Sprites/Default"));
            pointerLine.useWorldSpace = true;
            pointerLine.enabled = false;
        }
    }

    private void Update()
    {
        if (dropCooldown > 0f)
        {
            dropCooldown -= Time.deltaTime;
        }

        // Check input triggers
        bool gripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controllerType);
        bool triggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controllerType);
        bool actionButtonDown = (handSide == HandSide.Left)
            ? (OVRInput.GetDown(OVRInput.Button.Three, controllerType) || OVRInput.GetDown(OVRInput.Button.Four, controllerType))
            : (OVRInput.GetDown(OVRInput.Button.One, controllerType) || OVRInput.GetDown(OVRInput.Button.Two, controllerType));

        // Keyboard debug fallbacks
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (handSide == HandSide.Left && kb.gKey.wasPressedThisFrame) gripDown = true;
            if (handSide == HandSide.Right && (kb.hKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) gripDown = true;
            if (handSide == HandSide.Right && kb.eKey.wasPressedThisFrame) actionButtonDown = true;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (handSide == HandSide.Left && Input.GetKeyDown(KeyCode.G)) gripDown = true;
        if (handSide == HandSide.Right && (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.P))) gripDown = true;
        if (handSide == HandSide.Right && Input.GetKeyDown(KeyCode.E)) actionButtonDown = true;
#endif

        // If an item is currently flying toward hand via ray pull
        if (pullingItem != null)
        {
            UpdatePullingItem();
            return;
        }

        if (heldItem == null)
        {
            UpdateRaycastPointing();

            // Try picking up: only when hand is empty and not on drop cooldown
            if (dropCooldown <= 0f && (gripDown || triggerDown || actionButtonDown))
            {
                // 1. Try immediate close-range touch grab
                InteractableItem closeItem = FindCloseItem();
                if (closeItem != null)
                {
                    GrabItem(closeItem);
                    return;
                }

                // 2. Try distance ray grab or interaction
                if (pointedItem != null)
                {
                    if (pointedItem.item != null && pointedItem.item.isPickable)
                    {
                        StartPullingItem(pointedItem);
                    }
                    else if (pointedItem.item != null && pointedItem.item.isInteractable)
                    {
                        // Direct interaction through ray
                        TryInteractWithTarget(pointedItem);
                    }
                }
            }
        }
        else
        {
            // When holding an item (e.g. wire or torch):
            // Still allow the hand to aim ray at puzzle targets (like the generator) to use the held item!
            UpdateRaycastPointing();

            // If user presses Trigger or Action Button while pointing at an interactable (e.g. Generator, Switch, Computer)
            if (dropCooldown <= 0f && (triggerDown || actionButtonDown) && pointedItem != null && pointedItem.item != null && pointedItem.item.isInteractable)
            {
                TryInteractWithTarget(pointedItem);
                return;
            }

            // Grip button drops the held item onto the floor anywhere
            if (gripDown)
            {
                DropItem();
            }
        }
    }

    public void TryInteractWithTarget(InteractableItem target)
    {
        if (target == null) return;

        // Special handling: If holding the USB key (pd / Pendrive) and targeting any computer workstation part
        bool isUsbKey = (heldItem != null && (heldItem.name.ToLower().Contains("pd") || (heldItem.item != null && heldItem.item.name.ToLower().Contains("pendrive")))) ||
                        (GetOtherHand() != null && GetOtherHand().heldItem != null && (GetOtherHand().heldItem.name.ToLower().Contains("pd") || (GetOtherHand().heldItem.item != null && GetOtherHand().heldItem.item.name.ToLower().Contains("pendrive"))));

        bool isComputerStation = target.name.ToLower().Contains("keyboard") ||
                                 target.name.ToLower().Contains("computer") ||
                                 target.name.ToLower().Contains("monitor");

        if (isUsbKey && isComputerStation)
        {
            InteractableItem usbItem = (heldItem != null && (heldItem.name.ToLower().Contains("pd") || (heldItem.item != null && heldItem.item.name.ToLower().Contains("pendrive"))))
                ? heldItem : GetOtherHand().heldItem;

            // Set missing piece resolved on all keyboard / computer components so USB is plugged in
            var allItems = Resources.FindObjectsOfTypeAll<InteractableItem>();
            foreach (var it in allItems)
            {
                if (it.gameObject.scene.name == "ModernOffice_Interior_SampleLayout" &&
                    (it.name.Contains("Keyboard") || it.name.Contains("Computer") || it.name.Contains("Monitor")))
                {
                    it.ChangeMissingPiece(true);
                }
            }

            // Consume USB key from hand
            if (usbItem == heldItem) heldItem = null;
            else if (GetOtherHand() != null) GetOtherHand().heldItem = null;

            if (Application.isPlaying) Destroy(usbItem.gameObject);
            else usbItem.gameObject.SetActive(false);

            if (ItemFloatingTooltip.Instance != null)
                ItemFloatingTooltip.Instance.Show(target);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();

            Debug.Log("[VRHandGrabber] USB Key plugged in! Now click the keyboard to type the override password and boot the PC.");
            return;
        }

        // If user interacts with Keyboard / Monitor / Computer after USB is already plugged in
        if (isComputerStation && target.hasMissingPiece)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.StartComputer();
            }

            if (ItemFloatingTooltip.Instance != null)
                ItemFloatingTooltip.Instance.Show(target);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();

            Debug.Log("[VRHandGrabber] Typed on keyboard! Override code executed. Computer online and exit door unlocked!");
            return;
        }

        // Check if held item in this hand (or other hand) matches target's missing piece
        InteractableItem matchingHeldItem = null;
        if (heldItem != null && target.missingPiece != null && heldItem.item == target.missingPiece)
        {
            matchingHeldItem = heldItem;
        }
        else
        {
            VRHandGrabber other = GetOtherHand();
            if (other != null && other.heldItem != null && target.missingPiece != null && other.heldItem.item == target.missingPiece)
            {
                matchingHeldItem = other.heldItem;
            }
        }

        if (matchingHeldItem != null)
        {
            // Successfully apply the missing piece!
            target.ChangeMissingPiece(true);
            target.PerformAction();

            // Destroy or consume the used item from hand
            if (matchingHeldItem == heldItem)
            {
                heldItem = null;
            }
            else
            {
                VRHandGrabber other = GetOtherHand();
                if (other != null) other.heldItem = null;
            }

            if (Application.isPlaying)
            {
                Destroy(matchingHeldItem.gameObject);
            }
            else
            {
                matchingHeldItem.gameObject.SetActive(false);
            }

            if (ItemFloatingTooltip.Instance != null)
                ItemFloatingTooltip.Instance.Show(target);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();

            Debug.Log($"[VRHandGrabber] Used {matchingHeldItem.name} on {target.name} successfully!");
            return;
        }

        // Strict Progression Enforcement:
        // 1. Exit Door: Can ONLY be opened after computer terminal override has unlocked it (target.hasMissingPiece == true)
        if (target.name.ToLower().Contains("door"))
        {
            if (!target.hasMissingPiece)
            {
                Debug.Log("[VRHandGrabber] Exit Door is locked! Override computer terminal first.");
                if (ItemFloatingTooltip.Instance != null)
                    ItemFloatingTooltip.Instance.Show(target);
                return;
            }

            target.PerformAction();
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();
            return;
        }

        // 2. Light Switches: Can ONLY be turned on after Generator has been repaired with wire (generator hasMissingPiece == true)
        if (target.name.ToLower().Contains("switch"))
        {
            var genObj = GameObject.Find("generator");
            var genItem = genObj != null ? genObj.GetComponent<InteractableItem>() : null;
            if (genItem != null && !genItem.hasMissingPiece)
            {
                Debug.Log("[VRHandGrabber] No power! Generator must be repaired with wire before turning on lights.");
                if (ItemFloatingTooltip.Instance != null)
                    ItemFloatingTooltip.Instance.Show(target);
                return;
            }

            // Power is available - operate switch
            target.ChangeMissingPiece(true);
            target.PerformAction();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();

            if (ItemFloatingTooltip.Instance != null)
                ItemFloatingTooltip.Instance.Show(target);
            return;
        }

        // If no matching held item, perform standard interaction or check if already solved
        if (target.hasMissingPiece || target.missingPiece == null)
        {
            target.PerformAction();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();
        }
        else
        {
            Debug.Log($"[VRHandGrabber] {target.name} requires {target.missingPiece.itemName}!");
        }

        if (ItemFloatingTooltip.Instance != null)
            ItemFloatingTooltip.Instance.Show(target);
    }

    private void UpdateRaycastPointing()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, rayMaxDistance, grabMask, QueryTriggerInteraction.Ignore);

        InteractableItem hitItem = null;
        Vector3 rayEnd = ray.origin + ray.direction * (hasHit ? hit.distance : 2.5f);

        if (hasHit)
        {
            hitItem = hit.collider.GetComponentInParent<InteractableItem>();
        }

        // Generous spherecast fallback if direct ray hit floor or air near a small item
        if (hitItem == null)
        {
            RaycastHit sphereHit;
            if (Physics.SphereCast(ray, 0.18f, out sphereHit, rayMaxDistance, grabMask, QueryTriggerInteraction.Ignore))
            {
                hitItem = sphereHit.collider.GetComponentInParent<InteractableItem>();
                if (hitItem != null)
                {
                    rayEnd = sphereHit.point;
                }
            }
        }

        // If ray didn't hit an item, check if there is an item right next to the hand
        if (hitItem == null)
        {
            hitItem = FindCloseItem();
        }

        // Filter out items already held by this or other hand
        if (hitItem != null)
        {
            VRHandGrabber grabber = hitItem.GetComponentInParent<VRHandGrabber>();
            if (grabber != null) hitItem = null;
        }

        pointedItem = hitItem;

        // Update ray visual beam
        if (pointerLine != null)
        {
            pointerLine.enabled = true;
            pointerLine.positionCount = 2;
            pointerLine.SetPosition(0, transform.position);
            pointerLine.SetPosition(1, rayEnd);

            Color c = (pointedItem != null) ? rayHighlightColor : rayIdleColor;
            pointerLine.startColor = c;
            pointerLine.endColor = c;
        }

        // Update floating world tooltip directly above the item
        if (pointedItem != null)
        {
            ItemFloatingTooltip tooltip = ItemFloatingTooltip.Instance ?? ItemFloatingTooltip.EnsureInstanceInScene();
            tooltip.Show(pointedItem);
        }
        else
        {
            // Only hide if other hand is not currently pointing at something
            VRHandGrabber otherHand = GetOtherHand();
            if (otherHand == null || otherHand.pointedItem == null)
            {
                if (ItemFloatingTooltip.Instance != null)
                    ItemFloatingTooltip.Instance.Hide();
            }
        }
    }

    private VRHandGrabber GetOtherHand()
    {
        var grabbers = FindObjectsByType<VRHandGrabber>(FindObjectsSortMode.None);
        foreach (var g in grabbers)
        {
            if (g != this) return g;
        }
        return null;
    }

    private InteractableItem FindCloseItem()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, grabRadius, grabMask, QueryTriggerInteraction.Ignore);
        InteractableItem bestItem = null;
        float bestDist = float.MaxValue;

        foreach (var col in colliders)
        {
            InteractableItem item = col.GetComponentInParent<InteractableItem>();
            if (item != null && item.item != null && item.item.isPickable)
            {
                VRHandGrabber other = item.GetComponentInParent<VRHandGrabber>();
                if (other != null) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestItem = item;
                }
            }
        }
        return bestItem;
    }

    private void StartPullingItem(InteractableItem item)
    {
        if (item == null) return;

        // If other hand is holding or pulling it, ignore
        VRHandGrabber other = item.GetComponentInParent<VRHandGrabber>();
        if (other != null) return;

        pullingItem = item;

        // Disable physics while flying
        Rigidbody rb = pullingItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = pullingItem.GetComponentInChildren<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void UpdatePullingItem()
    {
        if (pullingItem == null) return;

        Vector3 dest = holdPoint.position;
        pullingItem.transform.position = Vector3.MoveTowards(pullingItem.transform.position, dest, pullSpeed * Time.deltaTime);

        // Beam points directly to the pulling item
        if (pointerLine != null)
        {
            pointerLine.enabled = true;
            pointerLine.positionCount = 2;
            pointerLine.SetPosition(0, transform.position);
            pointerLine.SetPosition(1, pullingItem.transform.position);
            pointerLine.startColor = rayHighlightColor;
            pointerLine.endColor = rayHighlightColor;
        }

        if (Vector3.Distance(pullingItem.transform.position, dest) <= 0.15f)
        {
            Collider col = pullingItem.GetComponentInChildren<Collider>();
            if (col != null) col.isTrigger = false;

            InteractableItem arrived = pullingItem;
            pullingItem = null;
            GrabItem(arrived);
        }
    }

    public void GrabItem(InteractableItem item)
    {
        if (item == null) return;

        if (heldItem != null)
        {
            DropItem();
        }

        heldItem = item;
        pointedItem = null;

        // Hide floating tooltip when grabbed
        if (ItemFloatingTooltip.Instance != null)
            ItemFloatingTooltip.Instance.Hide();

        // Parent to hold point
        Transform t = item.transform;
        t.SetParent(holdPoint, true);

        // Disable physics if any
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        AlignHeldItem(item);

        // Turn on torch spot light immediately if grabbing torch
        if (item.name.ToLower().Contains("torch"))
        {
            Light torchLight = item.GetComponentInChildren<Light>(true);
            if (torchLight != null)
            {
                torchLight.enabled = true;
                torchLight.gameObject.SetActive(true);
            }
        }

        // Notify inventory or game systems
        Inventory inv = FindFirstObjectByType<Inventory>();
        if (inv != null)
        {
            inv.ItemPicked(item);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickup();

        Debug.Log($"[VRHandGrabber] {handSide} Hand grabbed: {item.name}");
    }

    public void DropItem()
    {
        if (heldItem == null) return;

        InteractableItem item = heldItem;
        heldItem = null;
        dropCooldown = 0.35f; // Prevent immediately re-grabbing or clicking through to something else on the same click

        // Unparent
        item.transform.SetParent(null, true);

        // Ensure collider is enabled and physical
        Collider col = item.GetComponentInChildren<Collider>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        // Re-enable physics with gravity so it drops onto the floor
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = OVRInput.GetLocalControllerVelocity(controllerType);
            rb.angularVelocity = OVRInput.GetLocalControllerAngularVelocity(controllerType);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickup();

        Debug.Log($"[VRHandGrabber] {handSide} Hand dropped: {item.name}");
    }

    private void AlignHeldItem(InteractableItem item)
    {
        if (item.name.ToLower().Contains("torch"))
        {
            // The torch model + spotlight is oriented along +Z forward.
            // When attached to holdPoint, local identity rotation points the spotlight directly along controller forward (+Z)!
            item.transform.localPosition = new Vector3(0f, -0.04f, 0.12f);
            item.transform.localRotation = Quaternion.identity;

            Transform spot = item.transform.Find("Spot Light");
            if (spot != null)
            {
                spot.gameObject.SetActive(true);
                Light l = spot.GetComponent<Light>();
                if (l != null) l.enabled = true;
            }
        }
        else if (item.name.ToLower().Contains("wire"))
        {
            item.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            item.transform.localRotation = Quaternion.identity;
        }
        else if (item.name.ToLower().Contains("pd"))
        {
            item.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            item.transform.localRotation = Quaternion.identity;
        }
        else
        {
            item.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            item.transform.localRotation = Quaternion.identity;
        }
    }

    private void OnDisable()
    {
        if (pointerLine != null)
            pointerLine.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (handSide == HandSide.Left) ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
        Gizmos.DrawRay(transform.position, transform.forward * rayMaxDistance);
    }
}
