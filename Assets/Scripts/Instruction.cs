using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Instruction : MonoBehaviour
{
    public FirstPersonController player;
    public GameObject UI;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            player.enabled = true;
            UI.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
