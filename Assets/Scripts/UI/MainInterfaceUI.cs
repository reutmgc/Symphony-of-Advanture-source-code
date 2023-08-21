using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class MainInterfaceUI : MonoBehaviour
{
    [SerializeField]
    private InputActionReference openMenuAction;
    [SerializeField]
    private InputActionReference closeMenuAction;
    private CanvasGroup canvasGroup;
    [SerializeField]
    WalkmanUI walkmanUI;
    private void Awake()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }
    private void Start()
    {   
        MakeInvisible();
    }
    private void OnEnable()
    {
        openMenuAction.action.performed += context => MakeVisible();
        closeMenuAction.action.performed += context => MakeInvisible();
    }
    private void OnDisable()
    {
        openMenuAction.action.performed -= context => MakeVisible();
        closeMenuAction.action.performed -= context => MakeInvisible();
    }
     void MakeVisible()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        ServiceLocator.Instance.Get<InputManager>().ActivateUIMap();
        walkmanUI.gameObject.SetActive(true);
    }
    void MakeInvisible()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        ServiceLocator.Instance.Get<InputManager>().ActivatePlayerMap();
        walkmanUI.gameObject.SetActive(false);
    }
}