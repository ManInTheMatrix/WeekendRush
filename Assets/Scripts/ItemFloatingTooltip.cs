using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemFloatingTooltip : MonoBehaviour
{
    private static ItemFloatingTooltip _instance;
    public static ItemFloatingTooltip Instance => _instance;

    [Header("UI References")]
    public Canvas worldCanvas;
    public RectTransform panelRect;
    public Text titleText;
    public Text descriptionText;
    public Text nextHintText;
    public Text actionHintText;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 0.25f, 0);
    public float smoothSpeed = 15f;

    private Transform currentTarget;
    private Camera targetCamera;
    private bool isVisible = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        Hide();
    }

    private void LateUpdate()
    {
        if (!isVisible || currentTarget == null)
        {
            if (worldCanvas != null && worldCanvas.enabled)
                worldCanvas.enabled = false;
            return;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null) return;

        if (!worldCanvas.enabled)
            worldCanvas.enabled = true;

        Vector3 targetPos = CalculateTooltipPosition(currentTarget);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        // Billboard towards player's headset camera
        Vector3 lookDir = transform.position - targetCamera.transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    private Vector3 CalculateTooltipPosition(Transform target)
    {
        if (target == null) return transform.position;

        if (targetCamera == null) targetCamera = Camera.main;

        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null && targetCamera != null)
        {
            // Vector from object center towards camera
            Vector3 center = col.bounds.center;
            Vector3 toCam = (targetCamera.transform.position - center);
            toCam.y = 0; // horizontal direction
            if (toCam.sqrMagnitude < 0.001f) toCam = Vector3.forward;
            toCam.Normalize();

            // Find how far the bounding box extends horizontally towards the camera
            float horizontalExtent = Mathf.Abs(toCam.x * col.bounds.extents.x) + Mathf.Abs(toCam.z * col.bounds.extents.z);

            // Position at the upper front facing the player
            Vector3 frontTopPos = center + toCam * (horizontalExtent + 0.25f);
            frontTopPos.y = Mathf.Min(col.bounds.max.y + 0.15f, center.y + 0.8f);

            return frontTopPos;
        }

        return target.position + offset;
    }

    public void Show(InteractableItem item)
    {
        if (item == null || item.item == null)
        {
            Hide();
            return;
        }

        currentTarget = item.transform;
        isVisible = true;

        if (titleText != null)
        {
            string name = !string.IsNullOrEmpty(item.item.itemName) ? item.item.itemName : item.name;
            titleText.text = name;
        }

        if (descriptionText != null)
        {
            string desc = item.item.itemDescription;
            if (string.IsNullOrEmpty(desc))
            {
                if (item.item.isPickable) desc = "Can be picked up and used.";
                else if (item.item.isInteractable) desc = "Can be operated or activated.";
                else desc = "";
            }
            descriptionText.text = desc;
        }

        if (nextHintText != null)
        {
            if (!string.IsNullOrEmpty(item.item.nextHint))
            {
                nextHintText.gameObject.SetActive(true);
                nextHintText.text = "NEXT: " + item.item.nextHint;
            }
            else
            {
                nextHintText.gameObject.SetActive(false);
            }
        }

        if (actionHintText != null)
        {
            if (item.item.isPickable)
            {
                actionHintText.text = "[Trigger] Pull & Grab  |  [Grip] Drop";
                actionHintText.color = new Color(0.2f, 0.9f, 0.3f);
            }
            else if (item.item.isInteractable)
            {
                bool isComputerStation = item.name.ToLower().Contains("keyboard") ||
                                         item.name.ToLower().Contains("computer") ||
                                         item.name.ToLower().Contains("monitor");

                bool isDoor = item.name.ToLower().Contains("door");

                if (isDoor)
                {
                    if (item.hasMissingPiece)
                    {
                        actionHintText.text = "[Trigger / A] Open Door & Escape!";
                        actionHintText.color = new Color(0.2f, 1f, 0.3f);
                    }
                    else
                    {
                        actionHintText.text = "LOCKED - Requires computer terminal override";
                        actionHintText.color = new Color(1f, 0.3f, 0.3f);
                    }
                }
                else if (isComputerStation)
                {
                    GameManager gm = FindFirstObjectByType<GameManager>();
                    bool isPcOn = gm != null && gm.computer != null && gm.computer.activeSelf;

                    if (isPcOn)
                    {
                        actionHintText.text = "TERMINAL ONLINE - Exit Door Unlocked!";
                        actionHintText.color = new Color(0.2f, 1f, 0.4f);
                    }
                    else if (item.hasMissingPiece)
                    {
                        actionHintText.text = "[Trigger / A] Press Keyboard to Execute Override";
                        actionHintText.color = new Color(0.2f, 0.9f, 1f);
                    }
                    else
                    {
                        actionHintText.text = "[Trigger / A] Plug in USB Pendrive Key";
                        actionHintText.color = new Color(1f, 0.7f, 0.2f);
                    }
                }
                else if (item.name.ToLower().Contains("switch"))
                {
                    var genObj = GameObject.Find("generator");
                    var genItem = genObj != null ? genObj.GetComponent<InteractableItem>() : null;
                    bool hasPower = genItem != null && genItem.hasMissingPiece;

                    if (!hasPower)
                    {
                        actionHintText.text = "NO POWER - Repair generator with wire first";
                        actionHintText.color = new Color(1f, 0.4f, 0.2f);
                    }
                    else if (item.hasMissingPiece)
                    {
                        actionHintText.text = "LIGHTS ON";
                        actionHintText.color = new Color(0.2f, 1f, 0.4f);
                    }
                    else
                    {
                        actionHintText.text = "[Trigger / A] Turn On Light Switch";
                        actionHintText.color = new Color(0.2f, 0.8f, 1f);
                    }
                }
                else if (item.missingPiece != null && !item.hasMissingPiece)
                {
                    actionHintText.text = $"[Trigger / A] Connect {item.missingPiece.itemName}";
                    actionHintText.color = new Color(1f, 0.6f, 0.2f);
                }
                else
                {
                    actionHintText.text = "[Trigger / A] Activate / Operate";
                    actionHintText.color = new Color(0.2f, 0.8f, 1f);
                }
            }
            else
            {
                actionHintText.text = "";
            }
        }

        if (worldCanvas != null)
            worldCanvas.enabled = true;

        // Position immediately on show so it doesn't lerp from old position
        if (targetCamera == null) targetCamera = Camera.main;
        transform.position = CalculateTooltipPosition(currentTarget);
        if (targetCamera != null)
        {
            Vector3 lookDir = transform.position - targetCamera.transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    public void Hide()
    {
        isVisible = false;
        currentTarget = null;
        if (worldCanvas != null)
            worldCanvas.enabled = false;
    }

    public static ItemFloatingTooltip EnsureInstanceInScene()
    {
        if (_instance != null) return _instance;

        GameObject tooltipObj = new GameObject("ItemFloatingTooltip");
        Canvas canvas = tooltipObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = tooltipObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20;

        RectTransform rt = tooltipObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 200);
        rt.localScale = Vector3.one * 0.0018f;

        // Background panel
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(tooltipObj.transform, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.11f, 0.92f);

        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = defaultFont;
        title.fontSize = 22;
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, 0.88f, 0.35f);
        title.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.72f);
        titleRt.anchorMax = new Vector2(0.95f, 0.96f);
        titleRt.sizeDelta = Vector2.zero;

        // Description text
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(panelObj.transform, false);
        Text desc = descObj.AddComponent<Text>();
        desc.font = defaultFont;
        desc.fontSize = 15;
        desc.color = new Color(0.9f, 0.9f, 0.95f);
        desc.alignment = TextAnchor.MiddleCenter;
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.05f, 0.44f);
        descRt.anchorMax = new Vector2(0.95f, 0.72f);
        descRt.sizeDelta = Vector2.zero;

        // Next hint text
        GameObject nextHintObj = new GameObject("NextHint");
        nextHintObj.transform.SetParent(panelObj.transform, false);
        Text nextHint = nextHintObj.AddComponent<Text>();
        nextHint.font = defaultFont;
        nextHint.fontSize = 14;
        nextHint.fontStyle = FontStyle.Bold;
        nextHint.color = new Color(1f, 0.75f, 0.2f);
        nextHint.alignment = TextAnchor.MiddleCenter;
        nextHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform nextHintRt = nextHintObj.GetComponent<RectTransform>();
        nextHintRt.anchorMin = new Vector2(0.05f, 0.20f);
        nextHintRt.anchorMax = new Vector2(0.95f, 0.44f);
        nextHintRt.sizeDelta = Vector2.zero;

        // Action hint text
        GameObject hintObj = new GameObject("ActionHint");
        hintObj.transform.SetParent(panelObj.transform, false);
        Text hint = hintObj.AddComponent<Text>();
        hint.font = defaultFont;
        hint.fontSize = 13;
        hint.fontStyle = FontStyle.Italic;
        hint.color = new Color(0.2f, 0.9f, 0.4f);
        hint.alignment = TextAnchor.MiddleCenter;
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.05f, 0.03f);
        hintRt.anchorMax = new Vector2(0.95f, 0.20f);
        hintRt.sizeDelta = Vector2.zero;

        ItemFloatingTooltip tooltipComp = tooltipObj.AddComponent<ItemFloatingTooltip>();
        tooltipComp.worldCanvas = canvas;
        tooltipComp.panelRect = panelRt;
        tooltipComp.titleText = title;
        tooltipComp.descriptionText = desc;
        tooltipComp.nextHintText = nextHint;
        tooltipComp.actionHintText = hint;

        canvas.enabled = false;
        return tooltipComp;
    }
}