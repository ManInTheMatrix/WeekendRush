using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LocomotionMode
{
    Hybrid,          // Left stick = smooth walk, Right stick forward = teleport
    TeleportOnly,    // Smooth walk disabled, Left or Right stick = teleport
    SmoothWalkOnly   // Teleport disabled, Left stick = smooth walk
}

public enum TeleportHand
{
    Right,
    Left
}

public class VRTeleport : MonoBehaviour
{
    [Header("Locomotion Mode")]
    [Tooltip("Choose whether the player walks, teleports, or both")]
    public LocomotionMode locomotionMode = LocomotionMode.Hybrid;

    [Header("Teleport Aiming")]
    public TeleportHand aimHand = TeleportHand.Right;
    public Transform leftController;
    public Transform rightController;
    public Transform centerEye;

    [Header("Arc Physics")]
    public float initialVelocity = 8.5f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public int arcSegments = 40;
    public float segmentStep = 0.035f;
    public LayerMask hitMask = ~0;
    [Range(0.4f, 0.95f)]
    public float minFloorSlope = 0.65f; // Ensures surfaces are walkable floors, not walls

    [Header("Visuals")]
    public LineRenderer arcLineRenderer;
    public GameObject reticlePrefab;
    public Color validColor = new Color(0.1f, 0.9f, 1f, 0.85f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.85f);
    public float arcWidth = 0.025f;

    [Header("Components")]
    public CharacterController characterController;
    public OVRPlayerController ovrPlayerController;

    private GameObject activeReticle;
    private bool isAiming = false;
    private bool isHitValid = false;
    private Vector3 targetPosition;
    private Vector3 targetNormal;
    private List<Vector3> arcPoints = new List<Vector3>();

    private Vector3 initialSpawnPosition;

    private void Awake()
    {
        initialSpawnPosition = transform.position;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (ovrPlayerController == null)
            ovrPlayerController = GetComponent<OVRPlayerController>();

        // Create LineRenderer if not set
        if (arcLineRenderer == null)
        {
            GameObject lineObj = new GameObject("TeleportArcLine");
            lineObj.transform.SetParent(transform);
            arcLineRenderer = lineObj.AddComponent<LineRenderer>();
            arcLineRenderer.startWidth = arcWidth;
            arcLineRenderer.endWidth = arcWidth * 0.5f;
            arcLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            arcLineRenderer.useWorldSpace = true;
            arcLineRenderer.enabled = false;
        }

        // Create Reticle if not set
        if (activeReticle == null)
        {
            if (reticlePrefab != null)
            {
                activeReticle = Instantiate(reticlePrefab, transform);
            }
            else
            {
                activeReticle = CreateDefaultReticle();
            }
            activeReticle.SetActive(false);
        }
    }

    private void Start()
    {
        ApplyLocomotionMode();
        StartCoroutine(SnapToSpawnPositionRoutine());
    }

    private System.Collections.IEnumerator SnapToSpawnPositionRoutine()
    {
        // Ensure characterController starts placed squarely on the floor at the placed scene position
        for (int i = 0; i < 3; i++)
        {
            if (characterController != null) characterController.enabled = false;
            transform.position = initialSpawnPosition;
            Physics.SyncTransforms();
            if (characterController != null) characterController.enabled = true;
            yield return null;
        }
    }

    public void SetLocomotionMode(LocomotionMode mode)
    {
        locomotionMode = mode;
        ApplyLocomotionMode();
    }

    private void ApplyLocomotionMode()
    {
        if (ovrPlayerController != null)
        {
            switch (locomotionMode)
            {
                case LocomotionMode.Hybrid:
                case LocomotionMode.SmoothWalkOnly:
                    ovrPlayerController.EnableLinearMovement = true;
                    break;
                case LocomotionMode.TeleportOnly:
                    ovrPlayerController.EnableLinearMovement = false;
                    break;
            }
        }
    }

    private void Update()
    {
        if (locomotionMode == LocomotionMode.SmoothWalkOnly)
        {
            if (isAiming) StopAiming();
            return;
        }

        // Check aim input
        Transform activeHand = GetActiveHand();
        bool aimInput = CheckAimInput();

        if (aimInput)
        {
            if (!isAiming)
            {
                StartAiming();
            }
            UpdateAimArc(activeHand);

            // Also allow confirming teleport with index trigger, button, or stick click while aiming
            if (isHitValid && CheckTeleportConfirmInput())
            {
                ExecuteTeleport();
                StopAiming();
                return;
            }
        }
        else
        {
            if (isAiming)
            {
                // Released aiming (stick returned to neutral) - teleport if valid
                if (isHitValid)
                {
                    ExecuteTeleport();
                }
                StopAiming();
            }
        }
    }

    private Transform GetActiveHand()
    {
        if (aimHand == TeleportHand.Left && leftController != null)
            return leftController;
        if (rightController != null)
            return rightController;
        return transform;
    }

    private OVRInput.Controller GetActiveController()
    {
        return (aimHand == TeleportHand.Left) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
    }

    private bool CheckAimInput()
    {
        // 1. Right thumbstick forward (Y > 0.45f) if right hand
        if (aimHand == TeleportHand.Right)
        {
            Vector2 stick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            if (stick.y > 0.45f) return true;
        }
        else
        {
            Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (stick.y > 0.45f) return true;
        }

        // 2. In TeleportOnly mode, primary thumbstick push on left hand can also aim
        if (locomotionMode == LocomotionMode.TeleportOnly)
        {
            Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (leftStick.sqrMagnitude > 0.25f) return true;
        }

        // 3. Keyboard test fallback (holding T key)
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.isPressed)
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.T)) return true;
#endif

        return false;
    }

    private bool CheckTeleportConfirmInput()
    {
        OVRInput.Controller controller = GetActiveController();

        // Index trigger, primary button (A/X), or thumbstick click confirms teleport
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller) ||
            OVRInput.GetDown(OVRInput.Button.One, controller) ||
            OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, controller) ||
            OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick, controller))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) return true;
        }
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            return true;
#endif

        return false;
    }

    private void StartAiming()
    {
        isAiming = true;
        arcLineRenderer.enabled = true;
        if (activeReticle != null) activeReticle.SetActive(true);
    }

    private void StopAiming()
    {
        isAiming = false;
        isHitValid = false;
        if (arcLineRenderer != null) arcLineRenderer.enabled = false;
        if (activeReticle != null) activeReticle.SetActive(false);
    }

    private void UpdateAimArc(Transform handTransform)
    {
        arcPoints.Clear();
        Vector3 currentPos = handTransform.position;
        Vector3 currentVel = handTransform.forward * initialVelocity;

        arcPoints.Add(currentPos);

        isHitValid = false;
        RaycastHit hit;

        for (int i = 0; i < arcSegments; i++)
        {
            Vector3 nextPos = currentPos + currentVel * segmentStep + 0.5f * gravity * segmentStep * segmentStep;
            currentVel += gravity * segmentStep;

            Vector3 stepDir = nextPos - currentPos;
            float stepDist = stepDir.magnitude;

            if (Physics.Raycast(currentPos, stepDir.normalized, out hit, stepDist, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == characterController || hit.collider.transform.IsChildOf(transform))
                {
                    arcPoints.Add(nextPos);
                    currentPos = nextPos;
                    continue;
                }

                arcPoints.Add(hit.point);

                // Check if hit surface is a walkable floor (normal facing upwards)
                if (hit.normal.y >= minFloorSlope)
                {
                    isHitValid = true;
                    targetPosition = hit.point;
                    targetNormal = hit.normal;
                }
                else
                {
                    isHitValid = false;
                }
                break;
            }

            arcPoints.Add(nextPos);
            currentPos = nextPos;
        }

        // Update LineRenderer
        arcLineRenderer.positionCount = arcPoints.Count;
        arcLineRenderer.SetPositions(arcPoints.ToArray());

        Color currentColor = isHitValid ? validColor : invalidColor;
        arcLineRenderer.startColor = currentColor;
        arcLineRenderer.endColor = currentColor;

        // Update Reticle position
        if (activeReticle != null)
        {
            activeReticle.SetActive(isHitValid);
            if (isHitValid)
            {
                activeReticle.transform.position = targetPosition + targetNormal * 0.02f;
                activeReticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, targetNormal);
            }
        }
    }

    private void ExecuteTeleport()
    {
        if (characterController != null)
            characterController.enabled = false;

        // Align target position with HMD eye offset
        Vector3 finalPos = targetPosition;
        if (centerEye != null)
        {
            Vector3 eyeOffset = centerEye.position - transform.position;
            eyeOffset.y = 0; // Only offset horizontal plane
            finalPos -= eyeOffset;
        }

        transform.position = finalPos;

        if (characterController != null)
            characterController.enabled = true;

        Debug.Log($"[VRTeleport] Teleported player to floor at {finalPos}");
    }

    private GameObject CreateDefaultReticle()
    {
        GameObject reticle = new GameObject("TeleportFloorReticle");
        reticle.transform.SetParent(transform);

        // Outer ring cylinder
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Disc";
        disc.transform.SetParent(reticle.transform, false);
        disc.transform.localScale = new Vector3(0.6f, 0.005f, 0.6f);

        // Remove collider
        Collider col = disc.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Cyan glow material
        Renderer rend = disc.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = validColor;
            rend.material = mat;
        }

        return reticle;
    }
}
