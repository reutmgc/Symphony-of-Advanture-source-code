using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour, IRegistrableService
{
    [SerializeField]
    GameSettings gameSetings;
    public  UnityEvent gameOverEvent;
    public  UnityEvent startedGameEvent;
    public static bool paused = false;
    private void Awake()
    {     
        ServiceLocator.Instance.Register<GameManager>(this);
    }
    void OnEnable()
    {
        startedGameEvent.AddListener(gameSetings.IncreaseGameSessionIndex);
    }
    void OnDisable()
    {
        startedGameEvent.RemoveListener(gameSetings.IncreaseGameSessionIndex);
    }
    public void UnpauseGame()
    {
        if (paused)
        {
            Debug.Log("Game Unpaused");
            Time.timeScale = 1;
            ServiceLocator.Instance.Get<InputManager>().ActionMapGoBack();
            paused = false;
        }
    }
    public void PauseGame()
    {
        if (!paused)
        {
            Debug.Log("Game paused");
            Time.timeScale = 0;
            ServiceLocator.Instance.Get<InputManager>().ActivatePausedUIMap();
            paused = true;
        }
    }
    public void ExitGame()
    {
        GameOver();
        Application.Quit();
        Debug.Log("exiting game");

    }

    public void GameOver()
    {
        ExportManager exportManager = ServiceLocator.Instance.Get<ExportManager>();
        exportManager.SendCSVByEmail();
        gameOverEvent.Invoke();
    }

}
