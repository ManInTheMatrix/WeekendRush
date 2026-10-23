using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject[] allLights;
    public GameObject torch;
    public GameObject computer;
    public InteractableItem door;
    public InteractableItem[] allComputerParts;
    bool lightOff = true;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (lightOff)
        {
            int count = 0;
            foreach (var item in allLights)
            {
                if (item.activeInHierarchy)
                    count++;
            }
            if (count == allLights.Length)
                torch.gameObject.SetActive(false);
        }

    }

    public void StartComputer()
    {
        int count = 0;
        foreach (var item in allComputerParts)
        {
            if (item.hasMissingPiece)
                count++;
        }
        if (count == allComputerParts.Length)
        {
            computer.SetActive(true);
            door.ChangeMissingPiece(true);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();
        }
    }

    public void DoorUnlocked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySuccess();

        SceneManager.LoadScene(0);
    }
}
