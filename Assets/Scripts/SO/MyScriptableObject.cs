using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
// MAKING IT SO THE SCRIPTABLE OBJECT BEHAVES AS IT WOULD IN A BUILD, I.E. RESETS ITESELF WITH EACH GAME START
public class MyScriptableObject : ScriptableObject
{
#if UNITY_EDITOR
    private string defaultStateInJson;
    //Allow an editor class method to be initialized when Unity loads without action from the user.
    virtual protected void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    virtual protected void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }
    void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
            SaveOnEnterPlay();
        if (change == PlayModeStateChange.ExitingPlayMode)
            ResetOnExitPlay();
    }

    virtual protected void SaveOnEnterPlay()
    {
        defaultStateInJson = JsonUtility.ToJson(this);
    }

    virtual protected void ResetOnExitPlay()
    {
        JsonUtility.FromJsonOverwrite(defaultStateInJson, this);
    }
   
#endif
}

