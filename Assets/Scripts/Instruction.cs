using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Instruction : MonoBehaviour
{
    public FirstPersonController player;
    public OVRPlayerController ovrPlayer;
    public GameObject UI;

    void Start()
    {
        if (player != null) player.enabled = false;
        if (ovrPlayer != null)
        {
            ovrPlayer.EnableLinearMovement = false;
            ovrPlayer.EnableRotation = false;
        }
    }

    void Update()
    {
        bool kbDismiss = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            kbDismiss = kb.cKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        kbDismiss = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif

        bool dismiss = kbDismiss ||
                       OVRInput.GetDown(OVRInput.Button.Any) ||
                       OVRInput.GetDown(OVRInput.Button.One) ||
                       OVRInput.GetDown(OVRInput.Button.Two) ||
                       OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);

        if (dismiss)
        {
            if (player != null) player.enabled = true;
            if (ovrPlayer != null)
            {
                ovrPlayer.EnableLinearMovement = true;
                ovrPlayer.EnableRotation = true;
            }
            if (UI != null) UI.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
