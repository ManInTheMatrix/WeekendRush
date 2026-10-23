using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        Debug.Log("[Menu] StartGame called. Loading game scene index 1...");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccess();
        }
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        Debug.Log("[Menu] QuitGame called. Exiting application...");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPickup();
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
