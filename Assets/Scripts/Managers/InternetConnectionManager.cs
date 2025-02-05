using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
public class InternetConnectionManager : MonoBehaviour, IRegistrableService
{
    [SerializeField]
    GameObject noInternetPanel;
    [SerializeField]
    InputActionReference tryAgainKey;
    private void Awake()
    {
        ServiceLocator.Instance.Register<InternetConnectionManager>(this);
    }
    private void OnEnable()
    {
        tryAgainKey.action.performed += TryAgain;
    }
    private void OnDisable()
    {
        tryAgainKey.action.performed -= TryAgain;
    }
    public  IEnumerator CheckInternetConnection(Action<bool> action)
    {
        //Debug.Log("Checking for Internet connection");  
        //Yielding the WebRequestAsyncOperation inside a coroutine will cause the coroutine to pause until the UnityWebRequest
        ////encounters a system error or finishes communicating.
        UnityWebRequest request = UnityWebRequest.Get("https://google.com");
        yield return request.SendWebRequest();
        if (request.error != null)
        {
            action(false);
        }
        else
        {
            action(true);
        }
        request.Dispose();
    }


    // the key press connected to this is set up on the Input Manager object under unity events for Pause UI action map
    public void TryAgain(InputAction.CallbackContext a)
    {
        StartCoroutine(CheckInternetConnection(isConnected =>
        {
            if (isConnected)
                ConnectionDetected();
        }));
    }
    
    public void ConnectionDetected()
    {
        Debug.Log("Internet connection found.");
        noInternetPanel.SetActive(false);
        ServiceLocator.Instance.Get<GameManager>().UnpauseGame();
        // if some data was left unpushed due to lack of connection, re-push it now the connection is renewed 
        ServiceLocator.Instance.Get<ExportManager>().ExportPendingData();
    }
    public void NoConnectionDetected()
    {
        
        Debug.Log("Internet connection not found.");
        noInternetPanel.SetActive(true);
        ServiceLocator.Instance.Get<GameManager>().PauseGame();
    }
}
