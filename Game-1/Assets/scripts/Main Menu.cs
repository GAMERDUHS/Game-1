using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public CanvasGroup Optionpanel;

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Option()
    {
        Optionpanel.alpha = 1;
        Optionpanel.blocksRaycasts = true; 
    }

    public void Back()
    {
        Optionpanel.alpha = 0;
        Optionpanel.blocksRaycasts = false; 
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
